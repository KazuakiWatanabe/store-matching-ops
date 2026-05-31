using MatchOps.Domain.Catalog;
using MatchOps.Domain.Common;

namespace MatchOps.Domain.Tests.Catalog;

public class DiscountCapTests
{
    [Fact]
    public void Amount_Negative_Throws()
    {
        Assert.Throws<DomainException>(() => DiscountCap.Amount(Money.Jpy(-1m)));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1.1)]
    public void Rate_OutOfRange_Throws(double rate)
    {
        Assert.Throws<DomainException>(() => DiscountCap.Rate((decimal)rate));
    }

    [Fact]
    public void EnsureWithin_AmountWithinCap_DoesNotThrow()
    {
        var cap = DiscountCap.Amount(Money.Jpy(1000m));

        cap.EnsureWithin(Discount.OfAmount(Money.Jpy(800m)));
        cap.EnsureWithin(Discount.OfAmount(Money.Jpy(1000m))); // 境界
    }

    [Fact]
    public void EnsureWithin_AmountExceedsCap_Throws()
    {
        var cap = DiscountCap.Amount(Money.Jpy(1000m));

        Assert.Throws<DomainException>(() => cap.EnsureWithin(Discount.OfAmount(Money.Jpy(1001m))));
    }

    [Fact]
    public void EnsureWithin_RateWithinCap_DoesNotThrow()
    {
        var cap = DiscountCap.Rate(0.3m);

        cap.EnsureWithin(Discount.OfRate(0.2m));
        cap.EnsureWithin(Discount.OfRate(0.3m)); // 境界
    }

    [Fact]
    public void EnsureWithin_RateExceedsCap_Throws()
    {
        var cap = DiscountCap.Rate(0.3m);

        Assert.Throws<DomainException>(() => cap.EnsureWithin(Discount.OfRate(0.31m)));
    }

    [Fact]
    public void EnsureWithin_KindMismatch_Throws()
    {
        var cap = DiscountCap.Amount(Money.Jpy(1000m));

        Assert.Throws<DomainException>(() => cap.EnsureWithin(Discount.OfRate(0.1m)));
    }

    [Fact]
    public void EnsureWithin_CurrencyMismatch_Throws()
    {
        var cap = DiscountCap.Amount(Money.Of(10m, "USD"));

        Assert.Throws<DomainException>(() => cap.EnsureWithin(Discount.OfAmount(Money.Jpy(5m))));
    }
}
