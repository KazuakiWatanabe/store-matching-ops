using System.Text.Json;
using MatchOps.Application.Common;
using MatchOps.Application.Matching;
using MatchOps.Application.Notifications;
using MatchOps.Domain.Common;
using MatchOps.Domain.Matching;

namespace MatchOps.Application.Tests.Matching;

public class MatchingCampaignServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 6, 1, 9, 0, 0, TimeSpan.Zero);
    private static readonly TenantId Tenant = TenantId.New();
    private static readonly StoreId Store = StoreId.New();

    private static readonly AiProposalDraft DefaultDraft =
        new("45日以上ご来店のないお客様におすすめです。", "空き枠のご案内です。ぜひご来店ください。");

    /// <summary>テスト対象のサービスと観測用スパイを束ねる。</summary>
    private sealed class Fixture
    {
        public required MatchingCampaignService Service { get; init; }

        public required SpyAiProposalService Ai { get; init; }

        public required SpyOutboxWriter Outbox { get; init; }

        public required CountingUnitOfWork UnitOfWork { get; init; }
    }

    private static CandidateInput UsableInput(CustomerId customerId)
        => new(
            new CustomerCandidacy(customerId, Tenant, Store, CanReceiveNotifications: true, LastNotifiedOn: null),
            ScoreInputs.ForV0(dormancy: 0.8d, visitCycleDeviation: 0.5d, slotDayTimeMatch: 0.3d),
            [new OfferOption(OfferId.New(), IsActive: true, AppliesToSlot: true, DiscountWithinCap: true)]);

    private static Fixture BuildFixture(SlotCandidacy slot, params CustomerId[] customers)
    {
        var slots = new List<SlotCandidates>
        {
            new(slot, customers.Select(UsableInput).ToList()),
        };

        var ai = new SpyAiProposalService(DefaultDraft);
        var outbox = new SpyOutboxWriter();
        var unitOfWork = new CountingUnitOfWork();
        var service = new MatchingCampaignService(
            new InMemoryCampaignRepository(),
            new FakeCandidateSource(slots),
            new FakePolicyProvider(),
            ai,
            outbox,
            unitOfWork,
            new FakeClock(Now));

        // リポジトリは Fixture から参照しないが、サービス内で生成・取得が完結するよう同一インスタンスを保持する。
        return new Fixture { Service = service, Ai = ai, Outbox = outbox, UnitOfWork = unitOfWork };
    }

    private static SlotCandidacy NewSlot() => new(TimeSlotId.New(), Tenant, Store);

    private static RunCampaignCommand RunCommand(SlotCandidacy slot)
        => new(Tenant, Store, [slot.TimeSlotId]);

    [Fact]
    public async Task RunAsync_WithCandidates_CreatesScoredCampaignAndSaves()
    {
        SlotCandidacy slot = NewSlot();
        Fixture fixture = BuildFixture(slot, CustomerId.New(), CustomerId.New());

        Result<CampaignId> result = await fixture.Service.RunAsync(RunCommand(slot));

        Assert.True(result.IsSuccess);
        Assert.Equal(1, fixture.UnitOfWork.SaveCount);
    }

    [Fact]
    public async Task RunAsync_NoTargetSlots_ReturnsFailure()
    {
        SlotCandidacy slot = NewSlot();
        Fixture fixture = BuildFixture(slot, CustomerId.New());

        Result<CampaignId> result = await fixture.Service.RunAsync(
            new RunCampaignCommand(Tenant, Store, []));

        Assert.True(result.IsFailure);
        Assert.Equal("no_target_slots", result.ErrorCode);
        Assert.Equal(0, fixture.UnitOfWork.SaveCount);
    }

    [Fact]
    public async Task SendAsync_WithoutApproval_ReturnsFailureAndEnqueuesNothing()
    {
        SlotCandidacy slot = NewSlot();
        Fixture fixture = BuildFixture(slot, CustomerId.New());

        CampaignId campaignId = (await fixture.Service.RunAsync(RunCommand(slot))).Value;
        await fixture.Service.ProposeAsync(new ProposeCampaignCommand(campaignId));

        // 承認 (ApproveAsync) を行わず配信を試みる → 拒否される（ADR-0004）。
        Result result = await fixture.Service.SendAsync(new SendCampaignCommand(campaignId));

        Assert.True(result.IsFailure);
        Assert.Equal("not_approved", result.ErrorCode);
        Assert.Empty(fixture.Outbox.Messages);
    }

    [Fact]
    public async Task SendAsync_AfterApproval_SucceedsAndEnqueuesOnePerCandidate()
    {
        SlotCandidacy slot = NewSlot();
        Fixture fixture = BuildFixture(slot, CustomerId.New());

        CampaignId campaignId = (await fixture.Service.RunAsync(RunCommand(slot))).Value;
        await fixture.Service.ProposeAsync(new ProposeCampaignCommand(campaignId));
        Result approve = await fixture.Service.ApproveAsync(
            new ApproveCampaignCommand(campaignId, "admin@example.com"));
        Result send = await fixture.Service.SendAsync(new SendCampaignCommand(campaignId));

        Assert.True(approve.IsSuccess);
        Assert.True(send.IsSuccess);
        OutboxMessage message = Assert.Single(fixture.Outbox.Messages);
        Assert.Equal(campaignId, message.CampaignId);
        Assert.Equal(Tenant, message.TenantId);
        Assert.False(string.IsNullOrWhiteSpace(message.Body));
    }

    [Fact]
    public async Task ProposeAsync_PassesAggregatedDataWithoutPii()
    {
        SlotCandidacy slot = NewSlot();
        var customerA = CustomerId.New();
        var customerB = CustomerId.New();
        Fixture fixture = BuildFixture(slot, customerA, customerB);

        CampaignId campaignId = (await fixture.Service.RunAsync(RunCommand(slot))).Value;
        Result result = await fixture.Service.ProposeAsync(new ProposeCampaignCommand(campaignId));

        Assert.True(result.IsSuccess);

        // AI は施策単位で 1 回だけ呼ばれる（顧客ごとに呼ばない・CLAUDE.md §4.3）。
        Assert.Equal(1, fixture.Ai.CallCount);
        Assert.NotNull(fixture.Ai.LastRequest);
        AiProposalRequest request = fixture.Ai.LastRequest!;
        Assert.Equal(2, request.CandidateCount);

        // AI へ渡す入力に個別顧客の識別子（PII 相当）が含まれないことを検証する（ADR-0005）。
        string serialized = JsonSerializer.Serialize(request);
        Assert.DoesNotContain(customerA.Value.ToString(), serialized);
        Assert.DoesNotContain(customerB.Value.ToString(), serialized);
    }

    [Fact]
    public async Task ApproveAsync_BlankApprover_ReturnsFailure()
    {
        SlotCandidacy slot = NewSlot();
        Fixture fixture = BuildFixture(slot, CustomerId.New());

        CampaignId campaignId = (await fixture.Service.RunAsync(RunCommand(slot))).Value;
        await fixture.Service.ProposeAsync(new ProposeCampaignCommand(campaignId));

        Result result = await fixture.Service.ApproveAsync(new ApproveCampaignCommand(campaignId, "  "));

        Assert.True(result.IsFailure);
        Assert.Equal("approver_required", result.ErrorCode);
    }

    [Fact]
    public async Task ApproveAsync_NotProposed_ReturnsFailure()
    {
        SlotCandidacy slot = NewSlot();
        Fixture fixture = BuildFixture(slot, CustomerId.New());

        CampaignId campaignId = (await fixture.Service.RunAsync(RunCommand(slot))).Value;

        // proposed を経ずに承認しようとする（scored のまま）→ 拒否される。
        Result result = await fixture.Service.ApproveAsync(
            new ApproveCampaignCommand(campaignId, "admin@example.com"));

        Assert.True(result.IsFailure);
        Assert.Equal("invalid_state", result.ErrorCode);
    }

    [Fact]
    public async Task SendAsync_CampaignNotFound_ReturnsFailure()
    {
        SlotCandidacy slot = NewSlot();
        Fixture fixture = BuildFixture(slot, CustomerId.New());

        Result result = await fixture.Service.SendAsync(new SendCampaignCommand(CampaignId.New()));

        Assert.True(result.IsFailure);
        Assert.Equal("campaign_not_found", result.ErrorCode);
    }
}
