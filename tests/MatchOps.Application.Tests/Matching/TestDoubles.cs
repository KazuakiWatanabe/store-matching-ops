using MatchOps.Application.Common;
using MatchOps.Application.Matching;
using MatchOps.Application.Notifications;
using MatchOps.Domain.Common;
using MatchOps.Domain.Matching;

namespace MatchOps.Application.Tests.Matching;

/// <summary>固定時刻を返す時刻源（テスト用）。</summary>
internal sealed class FakeClock(DateTimeOffset now) : IClock
{
    public DateTimeOffset Now { get; } = now;

    public DateOnly Today => DateOnly.FromDateTime(Now.UtcDateTime);
}

/// <summary>インメモリの施策リポジトリ（テスト用。同一インスタンスを返す）。</summary>
internal sealed class InMemoryCampaignRepository : IMatchingCampaignRepository
{
    private readonly Dictionary<CampaignId, MatchingCampaign> _store = [];

    public Task AddAsync(MatchingCampaign campaign, CancellationToken cancellationToken = default)
    {
        _store[campaign.Id] = campaign;
        return Task.CompletedTask;
    }

    public Task<MatchingCampaign?> GetAsync(CampaignId id, CancellationToken cancellationToken = default)
        => Task.FromResult(_store.TryGetValue(id, out MatchingCampaign? campaign) ? campaign : null);
}

/// <summary>事前設定した枠候補を返す候補ソース（テスト用）。</summary>
internal sealed class FakeCandidateSource(IReadOnlyList<SlotCandidates> slots) : ICampaignCandidateSource
{
    public Task<IReadOnlyList<SlotCandidates>> GetAsync(
        TenantId tenantId,
        StoreId storeId,
        IReadOnlyList<TimeSlotId> targetSlots,
        CancellationToken cancellationToken = default)
        => Task.FromResult(slots);
}

/// <summary>既定の v0 ポリシー・無制限頻度を返すポリシー提供（テスト用）。</summary>
internal sealed class FakePolicyProvider : IMatchingPolicyProvider
{
    public ScoringPolicy GetScoringPolicy(TenantId tenantId, StoreId storeId) => ScoringPolicy.CreateV0Default();

    public NotificationFrequencyPolicy GetFrequencyPolicy(TenantId tenantId, StoreId storeId)
        => NotificationFrequencyPolicy.Unlimited;
}

/// <summary>渡された入力を記録し、固定ドラフトを返す AI 提案サービス（テスト用スパイ）。</summary>
internal sealed class SpyAiProposalService(AiProposalDraft draft) : IAiProposalService
{
    public AiProposalRequest? LastRequest { get; private set; }

    public int CallCount { get; private set; }

    public Task<AiProposalDraft> GenerateProposalAsync(
        AiProposalRequest request, CancellationToken cancellationToken = default)
    {
        LastRequest = request;
        CallCount++;
        return Task.FromResult(draft);
    }
}

/// <summary>積まれたメッセージを収集する Outbox（テスト用スパイ）。</summary>
internal sealed class SpyOutboxWriter : IOutboxWriter
{
    private readonly List<OutboxMessage> _messages = [];

    public IReadOnlyList<OutboxMessage> Messages => _messages;

    public Task EnqueueAsync(OutboxMessage message, CancellationToken cancellationToken = default)
    {
        _messages.Add(message);
        return Task.CompletedTask;
    }
}

/// <summary>SaveChanges 呼び出し回数を数える Unit of Work（テスト用）。</summary>
internal sealed class CountingUnitOfWork : IUnitOfWork
{
    public int SaveCount { get; private set; }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SaveCount++;
        return Task.CompletedTask;
    }
}
