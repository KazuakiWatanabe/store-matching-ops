using System.Globalization;
using MatchOps.Domain.Common;

namespace MatchOps.Domain.Tests.Common;

public class MoneyTests
{
    [Fact]
    public void Jpy_WithInteger_CreatesMoney()
    {
        var money = Money.Jpy(5000m);

        Assert.Equal(5000m, money.Amount);
        Assert.Equal("JPY", money.Currency);
    }

    [Fact]
    public void Jpy_WithFraction_Throws()
    {
        Assert.Throws<DomainException>(() => Money.Jpy(100.5m));
    }

    [Fact]
    public void Of_NullCurrency_Throws()
    {
        Assert.Throws<DomainException>(() => Money.Of(100m, null!));
    }

    [Fact]
    public void Of_NonJpyWithFraction_IsAllowed()
    {
        var money = Money.Of(10.5m, "USD");

        Assert.Equal(10.5m, money.Amount);
        Assert.Equal("USD", money.Currency);
    }

    [Theory]
    [InlineData("jpy")]   // 小文字
    [InlineData("JP")]    // 2文字
    [InlineData("JPYX")]  // 4文字
    [InlineData("US1")]   // 数字混在
    [InlineData("")]      // 空
    public void Of_WithInvalidCurrencyCode_Throws(string currency)
    {
        Assert.Throws<DomainException>(() => Money.Of(100m, currency));
    }

    [Fact]
    public void Add_SameCurrency_ReturnsSum()
    {
        var result = Money.Jpy(1000m) + Money.Jpy(500m);

        Assert.Equal(Money.Jpy(1500m), result);
    }

    [Fact]
    public void Add_DifferentCurrency_Throws()
    {
        var jpy = Money.Jpy(1000m);
        var usd = Money.Of(10m, "USD");

        Assert.Throws<DomainException>(() => jpy + usd);
    }

    [Fact]
    public void Subtract_SameCurrency_ReturnsDifference()
    {
        var result = Money.Jpy(1000m) - Money.Jpy(300m);

        Assert.Equal(Money.Jpy(700m), result);
    }

    [Fact]
    public void Subtract_DifferentCurrency_Throws()
    {
        var jpy = Money.Jpy(1000m);
        var usd = Money.Of(10m, "USD");

        Assert.Throws<DomainException>(() => jpy - usd);
    }

    [Fact]
    public void Equality_SameAmountAndCurrency_AreEqual()
    {
        Assert.Equal(Money.Jpy(1000m), Money.Jpy(1000m));
    }

    [Fact]
    public void Equality_DifferentCurrency_AreNotEqual()
    {
        Assert.NotEqual(Money.Of(1000m, "USD"), Money.Of(1000m, "EUR"));
    }

    [Fact]
    public void ToString_ReturnsAmountAndCurrency()
    {
        Assert.Equal("5000 JPY", Money.Jpy(5000m).ToString());
    }

    [Fact]
    public void ToString_WithFraction_UsesInvariantCulture_RegardlessOfCurrentCulture()
    {
        CultureInfo original = CultureInfo.CurrentCulture;
        try
        {
            // 小数点にカンマを使うカルチャでも、表現は不変カルチャ（ピリオド）に固定される。
            CultureInfo.CurrentCulture = new CultureInfo("de-DE");

            Assert.Equal("1234.5 USD", Money.Of(1234.5m, "USD").ToString());
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }
}
