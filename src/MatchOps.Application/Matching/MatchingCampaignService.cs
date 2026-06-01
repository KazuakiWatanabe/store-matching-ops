// -----------------------------------------------------------------------------
// <copyright file="MatchingCampaignService.cs" company="MatchOps">
// Copyright (c) MatchOps. All rights reserved.
// </copyright>
// <summary>
// 施策ユースケースの調整役（Application Service）。run / propose / approve / send を実装する。
// - 承認境界: approved を経ない配信を拒否する（ADR-0004）。未承認 send は Result.Failure。
// - PII 境界: AI へは AiProposalRequest（集約・匿名化済み）のみ渡す（ADR-0005）。顧客ごとに LLM を呼ばない。
// - 配信は Outbox に積むのみ（実送信は Worker）。状態変更と積み込みは IUnitOfWork で同一トランザクション確定。
// - 時刻は IClock 経由（DateTime.UtcNow 直呼び禁止, CLAUDE.md §10.4）。想定済み失敗は Result で返す（§7.3）。
// </summary>
// -----------------------------------------------------------------------------

using MatchOps.Application.Common;
using MatchOps.Application.Notifications;
using MatchOps.Domain.Common;
using MatchOps.Domain.Matching;

namespace MatchOps.Application.Matching;

/// <summary>施策の実行・提案・承認・配信ユースケースの実装。</summary>
public sealed class MatchingCampaignService : IMatchingCampaignService
{
    private static readonly MatchingEngine Engine = new();

    private readonly IMatchingCampaignRepository _repository;
    private readonly ICampaignCandidateSource _candidateSource;
    private readonly IMatchingPolicyProvider _policyProvider;
    private readonly IAiProposalService _aiProposalService;
    private readonly IOutboxWriter _outboxWriter;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    /// <summary>依存を注入して施策サービスを構築する。</summary>
    /// <param name="repository">施策リポジトリ。</param>
    /// <param name="candidateSource">候補抽出入力ソース。</param>
    /// <param name="policyProvider">スコアリング・頻度ポリシー提供。</param>
    /// <param name="aiProposalService">AI 提案生成（PII 非送出）。</param>
    /// <param name="outboxWriter">Outbox 積み込み。</param>
    /// <param name="unitOfWork">トランザクション境界。</param>
    /// <param name="clock">時刻源。</param>
    /// <exception cref="ArgumentNullException">いずれかの依存が <c>null</c> の場合。</exception>
    public MatchingCampaignService(
        IMatchingCampaignRepository repository,
        ICampaignCandidateSource candidateSource,
        IMatchingPolicyProvider policyProvider,
        IAiProposalService aiProposalService,
        IOutboxWriter outboxWriter,
        IUnitOfWork unitOfWork,
        IClock clock)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _candidateSource = candidateSource ?? throw new ArgumentNullException(nameof(candidateSource));
        _policyProvider = policyProvider ?? throw new ArgumentNullException(nameof(policyProvider));
        _aiProposalService = aiProposalService ?? throw new ArgumentNullException(nameof(aiProposalService));
        _outboxWriter = outboxWriter ?? throw new ArgumentNullException(nameof(outboxWriter));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
    }

    /// <inheritdoc />
    public async Task<Result<CampaignId>> RunAsync(
        RunCampaignCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (command.TargetSlots is null || command.TargetSlots.Count == 0)
        {
            return Result<CampaignId>.Failure("no_target_slots", "対象の空き枠が指定されていません。");
        }

        ScoringPolicy scoringPolicy = _policyProvider.GetScoringPolicy(command.TenantId, command.StoreId);
        NotificationFrequencyPolicy frequencyPolicy =
            _policyProvider.GetFrequencyPolicy(command.TenantId, command.StoreId);
        DateOnly today = _clock.Today;

        IReadOnlyList<SlotCandidates> slots = await _candidateSource
            .GetAsync(command.TenantId, command.StoreId, command.TargetSlots, cancellationToken)
            .ConfigureAwait(false);

        var candidates = new List<MatchingCandidate>();
        foreach (SlotCandidates slot in slots)
        {
            candidates.AddRange(
                Engine.BuildCandidates(slot.Slot, slot.Inputs, scoringPolicy, frequencyPolicy, today));
        }

        var campaign = MatchingCampaign.Open(command.TenantId, command.StoreId, command.TargetSlots);
        campaign.RecordScoring(candidates);

        await _repository.AddAsync(campaign, cancellationToken).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result<CampaignId>.Success(campaign.Id);
    }

    /// <inheritdoc />
    public async Task<Result> ProposeAsync(
        ProposeCampaignCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        MatchingCampaign? campaign = await _repository.GetAsync(command.CampaignId, cancellationToken)
            .ConfigureAwait(false);
        if (campaign is null)
        {
            return Result.Failure("campaign_not_found", "施策が見つかりません。");
        }

        if (campaign.Status != CampaignStatus.Scored)
        {
            return Result.Failure("invalid_state", $"{campaign.Status} 状態の施策は提案できません。");
        }

        // AI へは集約・匿名化済みデータのみ渡す（個別顧客の識別子・連絡先は渡さない・ADR-0005）。
        AiProposalRequest request = BuildProposalRequest(campaign);
        AiProposalDraft draft = await _aiProposalService
            .GenerateProposalAsync(request, cancellationToken).ConfigureAwait(false);

        if (string.IsNullOrWhiteSpace(draft.MessageTemplate))
        {
            return Result.Failure("empty_proposal", "AI 提案の配信文面が空です。");
        }

        // 文面はテンプレート（施策単位）を全候補に適用する。顧客ごとに LLM を呼ばない。
        foreach (MatchingCandidate candidate in campaign.Candidates)
        {
            candidate.AttachProposalReason(draft.MessageTemplate);
        }

        campaign.Propose();
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success();
    }

    /// <inheritdoc />
    public async Task<Result> ApproveAsync(
        ApproveCampaignCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (string.IsNullOrWhiteSpace(command.ApprovedBy))
        {
            return Result.Failure("approver_required", "承認者は必須です。");
        }

        MatchingCampaign? campaign = await _repository.GetAsync(command.CampaignId, cancellationToken)
            .ConfigureAwait(false);
        if (campaign is null)
        {
            return Result.Failure("campaign_not_found", "施策が見つかりません。");
        }

        if (campaign.Status != CampaignStatus.Proposed)
        {
            return Result.Failure("invalid_state", $"{campaign.Status} 状態の施策は承認できません。");
        }

        campaign.Approve(command.ApprovedBy, _clock.Now);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success();
    }

    /// <inheritdoc />
    public async Task<Result> SendAsync(
        SendCampaignCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        MatchingCampaign? campaign = await _repository.GetAsync(command.CampaignId, cancellationToken)
            .ConfigureAwait(false);
        if (campaign is null)
        {
            return Result.Failure("campaign_not_found", "施策が見つかりません。");
        }

        // 承認境界の強制: approved を経ていない施策は配信できない（ADR-0004）。例外でなく Result で返す。
        if (campaign.Status != CampaignStatus.Approved)
        {
            return Result.Failure("not_approved", "承認されていない施策は配信できません。");
        }

        // 配信は Outbox に積むのみ（実送信は Worker）。状態変更と同一トランザクションで確定する。
        foreach (MatchingCandidate candidate in campaign.Candidates)
        {
            var message = new OutboxMessage(
                campaign.Id,
                campaign.TenantId,
                candidate.CustomerId,
                candidate.TimeSlotId,
                candidate.OfferId,
                candidate.ProposalReason ?? string.Empty);
            await _outboxWriter.EnqueueAsync(message, cancellationToken).ConfigureAwait(false);
        }

        campaign.Send(_clock.Now);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success();
    }

    private static AiProposalRequest BuildProposalRequest(MatchingCampaign campaign)
    {
        IReadOnlyList<MatchingCandidate> candidates = campaign.Candidates;
        int candidateCount = candidates.Count;
        int slotCount = candidates.Select(c => c.TimeSlotId).Distinct().Count();
        int offerVariety = candidates.Select(c => c.OfferId).Distinct().Count();
        double averageScore = candidateCount == 0 ? 0d : candidates.Average(c => c.Score.Value);
        string segmentSummary = $"対象顧客 {candidateCount} 名 / 空き枠 {slotCount} 件 / Offer {offerVariety} 種";

        return new AiProposalRequest(
            campaign.StoreId, candidateCount, slotCount, offerVariety, averageScore, segmentSummary);
    }
}
