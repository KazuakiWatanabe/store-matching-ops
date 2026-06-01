using MatchOps.Application.Common;
using MatchOps.Application.Notifications;
using MatchOps.Domain.Common;
using MatchOps.Domain.Matching;
using MatchOps.Infrastructure.Notifications;
using MatchOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MatchOps.Infrastructure.Tests.Persistence;

public sealed class MatchingCampaignPersistenceTests(PostgresFixture fixture) : IClassFixture<PostgresFixture>
{
    private static readonly DateTimeOffset Now = new(2026, 6, 1, 9, 0, 0, TimeSpan.Zero);

    private sealed class FixedClock : IClock
    {
        public DateTimeOffset Now => MatchingCampaignPersistenceTests.Now;

        public DateOnly Today => DateOnly.FromDateTime(MatchingCampaignPersistenceTests.Now.UtcDateTime);
    }

    private static MatchingCandidate ScoredCandidate(TimeSlotId slotId, string? reason = null)
    {
        MatchingCandidate candidate = MatchingCandidate.Create(
            CustomerId.New(), slotId, OfferId.New(),
            ScoreBreakdown.From(new Dictionary<string, double> { ["dormancy"] = 0.4d }));
        if (reason is not null)
        {
            candidate.AttachProposalReason(reason);
        }

        return candidate;
    }

    [Fact]
    public async Task MatchingCampaign_RoundTrips_StatusCandidatesAndApprovalMeta()
    {
        var tenant = TenantId.New();
        var store = StoreId.New();
        var slotId = TimeSlotId.New();
        var campaign = MatchingCampaign.Open(tenant, store, [slotId]);
        campaign.RecordScoring([ScoredCandidate(slotId, "45日以上未来店のお客様へ")]);
        campaign.Propose();
        campaign.Approve("admin@example.com", Now);

        await using (MatchOpsDbContext write = fixture.CreateContext(tenant))
        {
            write.MatchingCampaigns.Add(campaign);
            await write.SaveChangesAsync();
        }

        await using MatchOpsDbContext read = fixture.CreateContext(tenant);
        MatchingCampaign loaded = await read.MatchingCampaigns.SingleAsync(c => c.Id == campaign.Id);

        Assert.Equal(CampaignStatus.Approved, loaded.Status);
        Assert.Equal("admin@example.com", loaded.ApprovedBy);
        Assert.Equal(Now, loaded.ApprovedAt);
        Assert.Contains(slotId, loaded.TargetSlots);

        MatchingCandidate candidate = Assert.Single(loaded.Candidates);
        Assert.Equal("45日以上未来店のお客様へ", candidate.ProposalReason);
        Assert.Equal(0.4d, candidate.Score.Value, precision: 6);
        Assert.Equal(0.4d, candidate.Breakdown.Contributions["dormancy"], precision: 6);
    }

    [Fact]
    public async Task MatchingCampaign_QueryFilter_ExcludesOtherTenant()
    {
        var tenantA = TenantId.New();
        var tenantB = TenantId.New();
        var store = StoreId.New();
        var slotId = TimeSlotId.New();
        var campaign = MatchingCampaign.Open(tenantA, store, [slotId]);
        campaign.RecordScoring([ScoredCandidate(slotId)]);

        await using (MatchOpsDbContext write = fixture.CreateContext(tenantA))
        {
            write.MatchingCampaigns.Add(campaign);
            await write.SaveChangesAsync();
        }

        // 別テナント（B）のコンテキストからは取得できない。
        await using MatchOpsDbContext read = fixture.CreateContext(tenantB);
        MatchingCampaign? loaded = await read.MatchingCampaigns.SingleOrDefaultAsync(c => c.Id == campaign.Id);

        Assert.Null(loaded);
    }

    [Fact]
    public async Task EfOutboxWriter_Enqueues_MessagePersistedWithinTenant()
    {
        var tenant = TenantId.New();
        var message = new OutboxMessage(
            CampaignId.New(), tenant, CustomerId.New(), TimeSlotId.New(), OfferId.New(), "配信文面です。");

        await using (MatchOpsDbContext write = fixture.CreateContext(tenant))
        {
            var writer = new EfOutboxWriter(write, new FixedClock());
            await writer.EnqueueAsync(message);
            await write.SaveChangesAsync();
        }

        await using MatchOpsDbContext read = fixture.CreateContext(tenant);
        OutboxMessageEntity stored = await read.OutboxMessages.SingleAsync(m => m.CampaignId == message.CampaignId);

        Assert.Equal(tenant, stored.TenantId);
        Assert.Equal("配信文面です。", stored.Body);
        Assert.Equal("queued", stored.Status);
        Assert.Equal(Now, stored.CreatedAt);
    }
}
