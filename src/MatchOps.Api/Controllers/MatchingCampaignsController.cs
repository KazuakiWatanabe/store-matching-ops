// -----------------------------------------------------------------------------
// <copyright file="MatchingCampaignsController.cs" company="MatchOps">
// Copyright (c) MatchOps. All rights reserved.
// </copyright>
// <summary>
// 施策（MatchingCampaign）の管理 API。run / approve / send / results を公開する。
// ビジネスロジックは持たず Application Service を呼ぶだけ（CLAUDE.md §4.2）。DbContext を直接触らない。
// 変更系（run/approve/send）は Idempotency-Key 必須（IdempotencyFilter, §10.1）。
// テナントは認証コンテキストから解決し、テナント外リソースは 404（ADR-0006）。レスポンスに PII を含めない。
// </summary>
// -----------------------------------------------------------------------------

using MatchOps.Api.Contracts;
using MatchOps.Api.Idempotency;
using MatchOps.Api.Tenancy;
using MatchOps.Application.Common;
using MatchOps.Application.Matching;
using MatchOps.Domain.Common;
using Microsoft.AspNetCore.Mvc;

namespace MatchOps.Api.Controllers;

/// <summary>施策の実行・承認・配信・結果参照を提供するコントローラ。</summary>
[ApiController]
[Route("api/campaigns")]
[Produces("application/json")]
public sealed class MatchingCampaignsController : ControllerBase
{
    private readonly IMatchingCampaignService _campaignService;
    private readonly IMatchingCampaignQueries _campaignQueries;
    private readonly RequestContext _requestContext;

    /// <summary>依存を注入してコントローラを構築する。</summary>
    /// <param name="campaignService">施策ユースケースサービス。</param>
    /// <param name="campaignQueries">施策参照ユースケース。</param>
    /// <param name="requestContext">現在のテナント・操作者コンテキスト。</param>
    public MatchingCampaignsController(
        IMatchingCampaignService campaignService,
        IMatchingCampaignQueries campaignQueries,
        RequestContext requestContext)
    {
        _campaignService = campaignService;
        _campaignQueries = campaignQueries;
        _requestContext = requestContext;
    }

    /// <summary>施策を実行（候補抽出＋スコアリング）し、scored 状態の施策を作成する。</summary>
    /// <param name="request">実行リクエスト（店舗・対象枠）。</param>
    /// <param name="cancellationToken">キャンセルトークン。</param>
    /// <returns>作成された施策の識別子。</returns>
    [HttpPost("run")]
    [ServiceFilter(typeof(IdempotencyFilter))]
    [ProducesResponseType(typeof(RunCampaignResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> RunAsync(
        [FromBody] RunCampaignRequest request, CancellationToken cancellationToken)
    {
        if (_requestContext.CurrentTenantId is not { } tenantId)
        {
            return Unauthorized(new ApiError("tenant_required", "テナントが特定できません。"));
        }

        var command = new RunCampaignCommand(
            tenantId,
            new StoreId(request.StoreId),
            request.TargetSlotIds.Select(id => new TimeSlotId(id)).ToList());

        Result<CampaignId> result = await _campaignService.RunAsync(command, cancellationToken);
        return result.IsSuccess
            ? Ok(new RunCampaignResponse(result.Value.Value))
            : MapFailure(result);
    }

    /// <summary>施策を人手で承認する（proposed → approved, Human-in-the-loop）。</summary>
    /// <param name="id">施策の識別子。</param>
    /// <param name="request">承認リクエスト（承認者は省略可）。</param>
    /// <param name="cancellationToken">キャンセルトークン。</param>
    /// <returns>処理結果。</returns>
    [HttpPost("{id:guid}/approve")]
    [ServiceFilter(typeof(IdempotencyFilter))]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> ApproveAsync(
        Guid id, [FromBody] ApproveCampaignRequest? request, CancellationToken cancellationToken)
    {
        string? approvedBy = request?.ApprovedBy ?? _requestContext.UserId;
        var command = new ApproveCampaignCommand(new CampaignId(id), approvedBy ?? string.Empty);

        Result result = await _campaignService.ApproveAsync(command, cancellationToken);
        return result.IsSuccess ? NoContent() : MapFailure(result);
    }

    /// <summary>承認済み施策を配信する（approved → sent）。配信は Outbox に積むのみ。</summary>
    /// <param name="id">施策の識別子。</param>
    /// <param name="cancellationToken">キャンセルトークン。</param>
    /// <returns>処理結果。</returns>
    [HttpPost("{id:guid}/send")]
    [ServiceFilter(typeof(IdempotencyFilter))]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> SendAsync(Guid id, CancellationToken cancellationToken)
    {
        Result result = await _campaignService.SendAsync(new SendCampaignCommand(new CampaignId(id)), cancellationToken);
        return result.IsSuccess ? NoContent() : MapFailure(result);
    }

    /// <summary>施策の結果概況を取得する。テナント外・存在しない場合は 404。</summary>
    /// <param name="id">施策の識別子。</param>
    /// <param name="cancellationToken">キャンセルトークン。</param>
    /// <returns>施策の結果概況（PII を含まない）。</returns>
    [HttpGet("{id:guid}/results")]
    [ProducesResponseType(typeof(CampaignResultsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetResultsAsync(Guid id, CancellationToken cancellationToken)
    {
        CampaignResultsView? view = await _campaignQueries.GetResultsAsync(new CampaignId(id), cancellationToken);
        if (view is null)
        {
            return NotFound(new ApiError("campaign_not_found", "施策が見つかりません。"));
        }

        var response = new CampaignResultsResponse(
            view.CampaignId.Value,
            view.Status,
            view.CandidateCount,
            view.Candidates.Select(c => new CandidateScoreResponse(c.Score, c.OfferId.Value, c.TimeSlotId.Value)).ToList());

        return Ok(response);
    }

    private IActionResult MapFailure(Result result)
    {
        var error = new ApiError(result.ErrorCode ?? "error", result.ErrorMessage ?? "処理に失敗しました。");
        int statusCode = result.ErrorCode switch
        {
            "campaign_not_found" => StatusCodes.Status404NotFound,
            "not_approved" => StatusCodes.Status409Conflict,
            "invalid_state" => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status422UnprocessableEntity,
        };

        return StatusCode(statusCode, error);
    }
}
