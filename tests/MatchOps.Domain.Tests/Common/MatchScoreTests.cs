using MatchOps.Domain.Common;

namespace MatchOps.Domain.Tests.Common;

public class MatchScoreTests
{
    [Theory]
    [InlineData(0d)]
    [InlineData(0.5d)]
    [InlineData(1d)]
    public void From_WithinRange_SetsValue(double value)
    {
        var score = MatchScore.From(value);

        Assert.Equal(value, score.Value);
    }

    [Theory]
    [InlineData(-0.0001d)]
    [InlineData(1.0001d)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void From_OutOfRangeOrNonFinite_Throws(double value)
    {
        Assert.Throws<DomainException>(() => MatchScore.From(value));
    }

    [Fact]
    public void Zero_HasValueZero()
    {
        Assert.Equal(0d, MatchScore.Zero.Value);
    }

    [Fact]
    public void Equality_SameValue_AreEqual()
    {
        Assert.Equal(MatchScore.From(0.42d), MatchScore.From(0.42d));
    }

    [Fact]
    public void ToString_FormatsWithInvariantCulture()
    {
        Assert.Equal("0.1234", MatchScore.From(0.1234d).ToString());
    }
}
