using MatchOps.Domain.Common;
using MatchOps.Domain.Matching;

namespace MatchOps.Domain.Tests.Matching;

public class MatchingCampaignTests
{
    private static readonly DateTimeOffset Now = new(2026, 5, 31, 10, 0, 0, TimeSpan.FromHours(9));

    private static MatchingCampaign DraftCampaign(out TimeSlotId slotId)
    {
        slotId = TimeSlotId.New();
        return MatchingCampaign.Open(TenantId.New(), StoreId.New(), [slotId]);
    }

    private static MatchingCandidate Candidate(TimeSlotId slotId)
        => MatchingCandidate.Create(
            CustomerId.New(), slotId, OfferId.New(),
            ScoreBreakdown.From(new Dictionary<string, double> { ["x"] = 0.5d }));

    private static MatchingCampaign Proposed(out TimeSlotId slotId)
    {
        var campaign = DraftCampaign(out slotId);
        campaign.RecordScoring([Candidate(slotId)]);
        campaign.Propose();
        return campaign;
    }

    [Fact]
    public void Open_SetsDraftStatus()
    {
        var campaign = DraftCampaign(out _);

        Assert.Equal(CampaignStatus.Draft, campaign.Status);
    }

    [Fact]
    public void Open_NoTargetSlots_Throws()
    {
        Assert.Throws<DomainException>(
            () => MatchingCampaign.Open(TenantId.New(), StoreId.New(), Array.Empty<TimeSlotId>()));
    }

    [Fact]
    public void FullHappyPath_DraftToMeasured()
    {
        var campaign = DraftCampaign(out var slotId);

        campaign.RecordScoring([Candidate(slotId)]);
        Assert.Equal(CampaignStatus.Scored, campaign.Status);

        campaign.Propose();
        Assert.Equal(CampaignStatus.Proposed, campaign.Status);

        campaign.Approve("admin@example.com", Now);
        Assert.Equal(CampaignStatus.Approved, campaign.Status);
        Assert.Equal("admin@example.com", campaign.ApprovedBy);
        Assert.Equal(Now, campaign.ApprovedAt);

        campaign.Send(Now);
        Assert.Equal(CampaignStatus.Sent, campaign.Status);
        Assert.Equal(Now, campaign.SentAt);

        campaign.Measure();
        Assert.Equal(CampaignStatus.Measured, campaign.Status);
    }

    [Fact]
    public void Send_BeforeApprove_Throws()
    {
        var campaign = Proposed(out _);

        // proposed のまま（承認なし）の Send は拒否される（ADR-0004）。
        Assert.Throws<DomainException>(() => campaign.Send(Now));
        Assert.Equal(CampaignStatus.Proposed, campaign.Status);
    }

    [Fact]
    public void Approve_FromSent_Throws()
    {
        var campaign = Proposed(out _);
        campaign.Approve("admin", Now);
        campaign.Send(Now);

        Assert.Throws<DomainException>(() => campaign.Approve("admin", Now));
    }

    [Fact]
    public void Propose_FromDraft_Throws()
    {
        var campaign = DraftCampaign(out _);

        Assert.Throws<DomainException>(() => campaign.Propose());
    }

    [Fact]
    public void RecordScoring_FromProposed_Throws()
    {
        var campaign = Proposed(out var slotId);

        Assert.Throws<DomainException>(() => campaign.RecordScoring([Candidate(slotId)]));
    }

    [Fact]
    public void Measure_BeforeSent_Throws()
    {
        var campaign = Proposed(out _);
        campaign.Approve("admin", Now);

        Assert.Throws<DomainException>(() => campaign.Measure());
    }

    [Fact]
    public void Approve_BlankApprover_Throws()
    {
        var campaign = Proposed(out _);

        Assert.Throws<DomainException>(() => campaign.Approve("  ", Now));
    }

    [Fact]
    public void RecordScoring_CandidateForUntargetedSlot_Throws()
    {
        var campaign = DraftCampaign(out _);
        var foreignCandidate = Candidate(TimeSlotId.New()); // 対象外の枠

        Assert.Throws<DomainException>(() => campaign.RecordScoring([foreignCandidate]));
    }

    [Fact]
    public void RecordScoring_StoresCandidatesAndRaisesEvent()
    {
        var campaign = DraftCampaign(out var slotId);

        campaign.RecordScoring([Candidate(slotId), Candidate(slotId)]);

        Assert.Equal(2, campaign.Candidates.Count);
        var scored = Assert.IsType<MatchScored>(Assert.Single(campaign.DomainEvents));
        Assert.Equal(2, scored.CandidateCount);
        Assert.Equal(campaign.Id, scored.CampaignId);
    }

    [Fact]
    public void Approve_And_Send_RaiseEvents()
    {
        var campaign = Proposed(out _);

        campaign.Approve("admin", Now);
        campaign.Send(Now);

        Assert.Contains(campaign.DomainEvents, e => e is CampaignApproved);
        Assert.Contains(campaign.DomainEvents, e => e is CampaignSent);
    }

    [Fact]
    public void ClearDomainEvents_RemovesAll()
    {
        var campaign = DraftCampaign(out var slotId);
        campaign.RecordScoring([Candidate(slotId)]);

        campaign.ClearDomainEvents();

        Assert.Empty(campaign.DomainEvents);
    }

    [Fact]
    public void Open_DeduplicatesTargetSlots()
    {
        var slotId = TimeSlotId.New();

        var campaign = MatchingCampaign.Open(TenantId.New(), StoreId.New(), [slotId, slotId]);

        Assert.Single(campaign.TargetSlots);
    }
}
