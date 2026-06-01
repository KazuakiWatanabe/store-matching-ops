using System.Collections.Concurrent;
using MatchOps.Application.Common;
using MatchOps.Application.Matching;
using MatchOps.Application.Notifications;
using MatchOps.Application.Tenancy;
using MatchOps.Domain.Common;
using MatchOps.Domain.Matching;

namespace MatchOps.Api.Tests;

/// <summary>プロセス内に施策を保持する共有ストア（テスト用シングルトン）。</summary>
internal sealed class InMemoryCampaignStore
{
    public ConcurrentDictionary<CampaignId, MatchingCampaign> Campaigns { get; } = new();
}

/// <summary>現在テナントでスコープするインメモリ施策リポジトリ（テスト用）。</summary>
internal sealed class TenantScopedCampaignRepository(InMemoryCampaignStore store, ITenantContext tenantContext)
    : IMatchingCampaignRepository
{
    public Task AddAsync(MatchingCampaign campaign, CancellationToken cancellationToken = default)
    {
        store.Campaigns[campaign.Id] = campaign;
        return Task.CompletedTask;
    }

    public Task<MatchingCampaign?> GetAsync(CampaignId id, CancellationToken cancellationToken = default)
    {
        if (store.Campaigns.TryGetValue(id, out MatchingCampaign? campaign)
            && tenantContext.CurrentTenantId is { } tenant
            && campaign.TenantId == tenant)
        {
            return Task.FromResult<MatchingCampaign?>(campaign);
        }

        return Task.FromResult<MatchingCampaign?>(null);
    }
}

/// <summary>対象枠ごとに利用可能な候補を 1 件返す候補ソース（テスト用）。</summary>
internal sealed class TestCandidateSource : ICampaignCandidateSource
{
    public Task<IReadOnlyList<SlotCandidates>> GetAsync(
        TenantId tenantId,
        StoreId storeId,
        IReadOnlyList<TimeSlotId> targetSlots,
        CancellationToken cancellationToken = default)
    {
        var slots = targetSlots
            .Select(slotId => new SlotCandidates(
                new SlotCandidacy(slotId, tenantId, storeId),
                [
                    new CandidateInput(
                        new CustomerCandidacy(
                            CustomerId.New(), tenantId, storeId, CanReceiveNotifications: true, LastNotifiedOn: null),
                        ScoreInputs.ForV0(dormancy: 0.8d, visitCycleDeviation: 0.5d, slotDayTimeMatch: 0.3d),
                        [new OfferOption(OfferId.New(), IsActive: true, AppliesToSlot: true, DiscountWithinCap: true)]),
                ]))
            .ToList();

        return Task.FromResult<IReadOnlyList<SlotCandidates>>(slots);
    }
}

/// <summary>既定の v0 ポリシー・無制限頻度を返すポリシー提供（テスト用）。</summary>
internal sealed class TestPolicyProvider : IMatchingPolicyProvider
{
    public ScoringPolicy GetScoringPolicy(TenantId tenantId, StoreId storeId) => ScoringPolicy.CreateV0Default();

    public NotificationFrequencyPolicy GetFrequencyPolicy(TenantId tenantId, StoreId storeId)
        => NotificationFrequencyPolicy.Unlimited;
}

/// <summary>固定ドラフトを返す AI 提案サービス（テスト用）。</summary>
internal sealed class TestAiProposalService : IAiProposalService
{
    public Task<AiProposalDraft> GenerateProposalAsync(
        AiProposalRequest request, CancellationToken cancellationToken = default)
        => Task.FromResult(new AiProposalDraft("提案理由テンプレート。", "配信文面テンプレート。"));
}

/// <summary>積まれたメッセージを収集する Outbox（テスト用シングルトン）。</summary>
internal sealed class TestOutboxWriter : IOutboxWriter
{
    private readonly ConcurrentQueue<OutboxMessage> _messages = new();

    public int Count => _messages.Count;

    public Task EnqueueAsync(OutboxMessage message, CancellationToken cancellationToken = default)
    {
        _messages.Enqueue(message);
        return Task.CompletedTask;
    }
}

/// <summary>何もしない Unit of Work（テスト用）。</summary>
internal sealed class NoOpUnitOfWork : IUnitOfWork
{
    public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
}
