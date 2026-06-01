// -----------------------------------------------------------------------------
// <copyright file="IMatchingCampaignService.cs" company="MatchOps">
// Copyright (c) MatchOps. All rights reserved.
// </copyright>
// <summary>
// 施策ユースケースの調整役（Application Service）。run → propose → approve → send の各ユースケースを提供する。
// 承認境界（approved を経ない配信不可・ADR-0004）と PII 境界（AI へは集約データのみ・ADR-0005）を強制する。
// 想定済みの失敗は例外でなく Result で返す（CLAUDE.md §7.3）。
// </summary>
// -----------------------------------------------------------------------------

using MatchOps.Application.Common;
using MatchOps.Domain.Common;

namespace MatchOps.Application.Matching;

/// <summary>施策の実行・提案・承認・配信ユースケースを提供する抽象。</summary>
public interface IMatchingCampaignService
{
    /// <summary>
    /// 候補抽出とスコアリングを行い、scored 状態の施策を作成・永続化する（draft → scored）。
    /// </summary>
    /// <param name="command">実行コマンド（テナント・店舗・対象枠）。</param>
    /// <param name="cancellationToken">キャンセルトークン。</param>
    /// <returns>成功時は作成された施策 ID、失敗時はエラー。</returns>
    Task<Result<CampaignId>> RunAsync(RunCampaignCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// AI に提案ドラフトを生成依頼し、施策を提案状態に進める（scored → proposed）。
    /// AI へは匿名化・集約済みデータのみを渡す（PII 非送出）。
    /// </summary>
    /// <param name="command">提案コマンド。</param>
    /// <param name="cancellationToken">キャンセルトークン。</param>
    /// <returns>成功/失敗。</returns>
    Task<Result> ProposeAsync(ProposeCampaignCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 人手で施策を承認する（proposed → approved, Human-in-the-loop）。
    /// </summary>
    /// <param name="command">承認コマンド（承認者を含む）。</param>
    /// <param name="cancellationToken">キャンセルトークン。</param>
    /// <returns>成功/失敗。</returns>
    Task<Result> ApproveAsync(ApproveCampaignCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 承認済み施策を配信する（approved → sent）。配信は Outbox に積むのみで実送信しない。
    /// 未承認の施策は配信できない（Result.Failure）。
    /// </summary>
    /// <param name="command">配信コマンド。</param>
    /// <param name="cancellationToken">キャンセルトークン。</param>
    /// <returns>成功/失敗。</returns>
    Task<Result> SendAsync(SendCampaignCommand command, CancellationToken cancellationToken = default);
}
