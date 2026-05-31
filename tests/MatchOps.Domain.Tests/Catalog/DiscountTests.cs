using MatchOps.Domain.Catalog;
using MatchOps.Domain.Common;

namespace MatchOps.Domain.Tests.Catalog;

public class DiscountTests
{
    [Fact]
    public void OfAmount_Valid_SetsKindAndAmount()
    {
        var discount = Discount.OfAmount(Money.Jpy(500m));

        Assert.Equal(DiscountKind.Amount, discount.Kind);
        Assert.Equal(Money.Jpy(500m), discount.Amount);
    }

    [Fact]
    public void OfAmount_Negative_Throws()
    {
        Assert.Throws<DomainException>(() => Discount.OfAmount(Money.Jpy(-1m)));
    }

    [Fact]
    public void OfRate_Valid_SetsKindAndRate()
    {
        var discount = Discount.OfRate(0.2m);

        Assert.Equal(DiscountKind.Rate, discount.Kind);
        Assert.Equal(0.2m, discount.Rate);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-0.1)]
    [InlineData(1.1)]
    public void OfRate_OutOfRange_Throws(double rate)
    {
        Assert.Throws<DomainException>(() => Discount.OfRate((decimal)rate));
    }

    [Fact]
    public void OfRate_BoundaryOne_IsAllowed()
    {
        Assert.Equal(1m, Discount.OfRate(1m).Rate);
    }
}
