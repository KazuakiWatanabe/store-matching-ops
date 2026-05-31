using MatchOps.Domain.Catalog;
using MatchOps.Domain.Common;

namespace MatchOps.Domain.Tests.Catalog;

public class OfferTests
{
    private static Offer NewCoupon(DiscountCap? cap = null, OfferConditions? conditions = null)
        => Offer.CreateCoupon(
            TenantId.New(), StoreId.New(), "20%OFF",
            cap ?? DiscountCap.Rate(0.2m), conditions);

    [Fact]
    public void CreateCoupon_SetsFieldsAndIsActive()
    {
        var tenantId = TenantId.New();
        var storeId = StoreId.New();
        var cap = DiscountCap.Amount(Money.Jpy(1000m));

        var offer = Offer.CreateCoupon(tenantId, storeId, "  1000円引き  ", cap);

        Assert.NotEqual(default, offer.Id.Value);
        Assert.Equal(tenantId, offer.TenantId);
        Assert.Equal(storeId, offer.StoreId);
        Assert.Equal(OfferType.Coupon, offer.Type);
        Assert.Equal("1000円引き", offer.Name);
        Assert.Equal(cap, offer.DiscountCap);
        Assert.True(offer.IsActive);
    }

    [Theory]
    [InlineData(OfferType.Menu)]
    [InlineData(OfferType.Course)]
    public void CreateItem_NonCoupon_HasNoCap(OfferType type)
    {
        var offer = Offer.CreateItem(TenantId.New(), StoreId.New(), type, "カット");

        Assert.Equal(type, offer.Type);
        Assert.Null(offer.DiscountCap);
        Assert.True(offer.IsActive);
    }

    [Fact]
    public void CreateItem_CouponType_Throws()
    {
        Assert.Throws<DomainException>(
            () => Offer.CreateItem(TenantId.New(), StoreId.New(), OfferType.Coupon, "x"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void CreateCoupon_BlankName_Throws(string name)
    {
        Assert.Throws<DomainException>(
            () => Offer.CreateCoupon(TenantId.New(), StoreId.New(), name, DiscountCap.Rate(0.2m)));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void CreateItem_BlankName_Throws(string name)
    {
        Assert.Throws<DomainException>(
            () => Offer.CreateItem(TenantId.New(), StoreId.New(), OfferType.Menu, name));
    }

    [Fact]
    public void EnsureDiscountWithinCap_WithinCap_DoesNotThrow()
    {
        var offer = NewCoupon(DiscountCap.Rate(0.3m));

        offer.EnsureDiscountWithinCap(Discount.OfRate(0.2m));
    }

    [Fact]
    public void EnsureDiscountWithinCap_ExceedsCap_Throws()
    {
        var offer = NewCoupon(DiscountCap.Rate(0.3m));

        Assert.Throws<DomainException>(() => offer.EnsureDiscountWithinCap(Discount.OfRate(0.5m)));
    }

    [Fact]
    public void EnsureDiscountWithinCap_OfferWithoutCap_Throws()
    {
        var menu = Offer.CreateItem(TenantId.New(), StoreId.New(), OfferType.Menu, "カット");

        Assert.Throws<DomainException>(() => menu.EnsureDiscountWithinCap(Discount.OfRate(0.1m)));
    }

    [Fact]
    public void Deactivate_MakesUnavailable()
    {
        var offer = NewCoupon();
        var anyDate = new DateOnly(2026, 5, 31);

        Assert.True(offer.IsAvailableOn(anyDate));

        offer.Deactivate();
        Assert.False(offer.IsActive);
        Assert.False(offer.IsAvailableOn(anyDate));

        offer.Activate();
        Assert.True(offer.IsAvailableOn(anyDate));
    }

    [Fact]
    public void IsAvailableOn_RespectsDayConditions()
    {
        var conditions = OfferConditions.Create(applicableDays: new[] { DayOfWeek.Monday });
        var offer = NewCoupon(conditions: conditions);

        Assert.True(offer.IsAvailableOn(new DateOnly(2026, 6, 1)));   // 月曜
        Assert.False(offer.IsAvailableOn(new DateOnly(2026, 5, 31))); // 日曜
    }
}
