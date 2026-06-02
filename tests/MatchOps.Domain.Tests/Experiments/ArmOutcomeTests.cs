using MatchOps.Domain.Common;
using MatchOps.Domain.Experiments;

namespace MatchOps.Domain.Tests.Experiments;

public class ArmOutcomeTests
{
    [Fact]
    public void Of_Valid_ComputesConversionRate()
    {
        ArmOutcome outcome = ArmOutcome.Of(count: 50, conversions: 10, revenue: 1000m);

        Assert.Equal(0.2d, outcome.ConversionRate, precision: 6);
    }

    [Fact]
    public void Of_ZeroCount_ConversionRateIsZero()
        => Assert.Equal(0d, ArmOutcome.Of(0, 0, 0m).ConversionRate);

    [Theory]
    [InlineData(-1, 0, 0)]
    [InlineData(10, 11, 0)]
    [InlineData(10, -1, 0)]
    [InlineData(10, 5, -1)]
    public void Of_Invalid_Throws(int count, int conversions, int revenue)
        => Assert.Throws<DomainException>(() => ArmOutcome.Of(count, conversions, revenue));
}
