using MatchOps.Domain.Common;
using MatchOps.Domain.Matching;

namespace MatchOps.Domain.Tests.Matching;

public class MatchingEngineTests
{
    private static readonly DateOnly Today = new(2026, 5, 31);

    private readonly MatchingEngine _engine = new();
    private readonly ScoringPolicy _policy = ScoringPolicy.CreateV0Default();
    private readonly TenantId _tenant = TenantId.New();
    private readonly StoreId _store = StoreId.New();

    private SlotCandidacy Slot() => new(TimeSlotId.New(), _tenant, _store);

    private static OfferOption UsableOffer() => new(OfferId.New(), IsActive: true, AppliesToSlot: true, DiscountWithinCap: true);

    private CustomerCandidacy Customer(
        bool canReceive = true, DateOnly? lastNotifiedOn = null, TenantId? tenant = null, StoreId? store = null)
        => new(CustomerId.New(), tenant ?? _tenant, store ?? _store, canReceive, lastNotifiedOn);

    private static CandidateInput Input(CustomerCandidacy customer, params OfferOption[] offers)
        => new(customer, ScoreInputs.ForV0(1d, 1d, 1d), offers);

    [Fact]
    public void BuildCandidates_EligibleCustomer_ProducesCandidate()
    {
        var slot = Slot();
        var customer = Customer();
        var input = Input(customer, UsableOffer());

        var result = _engine.BuildCandidates(slot, [input], _policy, NotificationFrequencyPolicy.Unlimited, Today);

        Assert.Single(result);
        Assert.Equal(customer.CustomerId, result[0].CustomerId);
        Assert.Equal(slot.TimeSlotId, result[0].TimeSlotId);
    }

    [Fact]
    public void BuildCandidates_DifferentTenant_Excluded()
    {
        var input = Input(Customer(tenant: TenantId.New()), UsableOffer());

        var result = _engine.BuildCandidates(Slot(), [input], _policy, NotificationFrequencyPolicy.Unlimited, Today);

        Assert.Empty(result);
    }

    [Fact]
    public void BuildCandidates_DifferentStore_Excluded()
    {
        var input = Input(Customer(store: StoreId.New()), UsableOffer());

        var result = _engine.BuildCandidates(Slot(), [input], _policy, NotificationFrequencyPolicy.Unlimited, Today);

        Assert.Empty(result);
    }

    [Fact]
    public void BuildCandidates_OptedOutCustomer_Excluded()
    {
        var input = Input(Customer(canReceive: false), UsableOffer());

        var result = _engine.BuildCandidates(Slot(), [input], _policy, NotificationFrequencyPolicy.Unlimited, Today);

        Assert.Empty(result);
    }

    [Fact]
    public void BuildCandidates_FrequencyExceeded_Excluded()
    {
        var frequency = NotificationFrequencyPolicy.OfMinIntervalDays(7);
        var input = Input(Customer(lastNotifiedOn: Today.AddDays(-3)), UsableOffer());

        var result = _engine.BuildCandidates(Slot(), [input], _policy, frequency, Today);

        Assert.Empty(result);
    }

    [Fact]
    public void BuildCandidates_DiscountExceedsCap_OfferNotSelected_Excluded()
    {
        // 唯一の Offer が値引き上限超過 → 利用可能 Offer なしで候補から外れる。
        var overCap = new OfferOption(OfferId.New(), IsActive: true, AppliesToSlot: true, DiscountWithinCap: false);
        var input = Input(Customer(), overCap);

        var result = _engine.BuildCandidates(Slot(), [input], _policy, NotificationFrequencyPolicy.Unlimited, Today);

        Assert.Empty(result);
    }

    [Fact]
    public void BuildCandidates_InactiveOrNotApplicableOffers_Excluded()
    {
        var inactive = new OfferOption(OfferId.New(), IsActive: false, AppliesToSlot: true, DiscountWithinCap: true);
        var notApplicable = new OfferOption(OfferId.New(), IsActive: true, AppliesToSlot: false, DiscountWithinCap: true);
        var input = Input(Customer(), inactive, notApplicable);

        var result = _engine.BuildCandidates(Slot(), [input], _policy, NotificationFrequencyPolicy.Unlimited, Today);

        Assert.Empty(result);
    }

    [Fact]
    public void BuildCandidates_SelectsFirstUsableOffer()
    {
        var unusable = new OfferOption(OfferId.New(), IsActive: false, AppliesToSlot: true, DiscountWithinCap: true);
        var usable = UsableOffer();
        var input = Input(Customer(), unusable, usable);

        var result = _engine.BuildCandidates(Slot(), [input], _policy, NotificationFrequencyPolicy.Unlimited, Today);

        Assert.Single(result);
        Assert.Equal(usable.OfferId, result[0].OfferId);
    }

    [Fact]
    public void BuildCandidates_OrdersByScoreDescending()
    {
        var slot = Slot();
        var high = new CandidateInput(Customer(), ScoreInputs.ForV0(1d, 1d, 1d), [UsableOffer()]);
        var low = new CandidateInput(Customer(), ScoreInputs.ForV0(0.1d, 0.1d, 0.1d), [UsableOffer()]);

        var result = _engine.BuildCandidates(slot, [low, high], _policy, NotificationFrequencyPolicy.Unlimited, Today);

        Assert.Equal(2, result.Count);
        Assert.True(result[0].Score.Value >= result[1].Score.Value);
        Assert.Equal(high.Customer.CustomerId, result[0].CustomerId);
    }
}
