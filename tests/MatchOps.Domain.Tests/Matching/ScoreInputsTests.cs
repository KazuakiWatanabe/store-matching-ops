using MatchOps.Domain.Common;
using MatchOps.Domain.Matching;

namespace MatchOps.Domain.Tests.Matching;

public class ScoreInputsTests
{
    [Fact]
    public void ForV0_AllPresent_ContainsThreeFactors()
    {
        var inputs = ScoreInputs.ForV0(0.5d, 0.6d, 0.7d);

        Assert.Equal(3, inputs.Values.Count);
        Assert.Equal(0.5d, inputs.Values[ScoringFactors.Dormancy]);
        Assert.Equal(0.6d, inputs.Values[ScoringFactors.VisitCycleDeviation]);
        Assert.Equal(0.7d, inputs.Values[ScoringFactors.SlotDayTimeMatch]);
    }

    [Fact]
    public void ForV0_MissingFactor_OmitsIt()
    {
        var inputs = ScoreInputs.ForV0(0.5d, null, 0.7d);

        Assert.Equal(2, inputs.Values.Count);
        Assert.False(inputs.Values.ContainsKey(ScoringFactors.VisitCycleDeviation));
    }

    [Fact]
    public void ForV0_AllMissing_IsEmpty()
    {
        Assert.Empty(ScoreInputs.ForV0(null, null, null).Values);
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(1.1)]
    [InlineData(double.NaN)]
    public void FromValues_OutOfRange_Throws(double value)
    {
        var values = new Dictionary<string, double> { ["x"] = value };

        Assert.Throws<DomainException>(() => ScoreInputs.FromValues(values));
    }

    [Fact]
    public void FromValues_BlankKey_Throws()
    {
        var values = new Dictionary<string, double> { ["  "] = 0.5d };

        Assert.Throws<DomainException>(() => ScoreInputs.FromValues(values));
    }
}
