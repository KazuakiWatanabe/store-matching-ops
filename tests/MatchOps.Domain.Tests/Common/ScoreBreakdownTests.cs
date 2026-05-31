using MatchOps.Domain.Common;

namespace MatchOps.Domain.Tests.Common;

public class ScoreBreakdownTests
{
    [Fact]
    public void From_SumsContributionsIntoTotal()
    {
        var contributions = new Dictionary<string, double>
        {
            ["dormancy"] = 0.4d,
            ["cycle"] = 0.35d,
            ["slotMatch"] = 0.25d,
        };

        var breakdown = ScoreBreakdown.From(contributions);

        Assert.Equal(1.0d, breakdown.Total.Value, precision: 9);
        Assert.Equal(3, breakdown.Contributions.Count);
        Assert.Equal(0.4d, breakdown.Contributions["dormancy"]);
    }

    [Fact]
    public void From_Null_Throws()
    {
        Assert.Throws<DomainException>(() => ScoreBreakdown.From(null!));
    }

    [Fact]
    public void From_Empty_TotalIsZero()
    {
        var breakdown = ScoreBreakdown.From(new Dictionary<string, double>());

        Assert.Equal(MatchScore.Zero, breakdown.Total);
        Assert.Empty(breakdown.Contributions);
    }

    [Fact]
    public void From_SumWithFloatingPointError_ClampsToOne()
    {
        // 0.1 + 0.2 + 0.3 + 0.4 は IEEE754 で 1.0000000000000002 となり僅かに 1 を超える。
        var contributions = new Dictionary<string, double>
        {
            ["a"] = 0.1d,
            ["b"] = 0.2d,
            ["c"] = 0.3d,
            ["d"] = 0.4d,
        };

        var breakdown = ScoreBreakdown.From(contributions);

        Assert.Equal(1.0d, breakdown.Total.Value);
    }

    [Fact]
    public void From_SumExceedsOne_Throws()
    {
        var contributions = new Dictionary<string, double>
        {
            ["a"] = 0.6d,
            ["b"] = 0.6d,
        };

        Assert.Throws<DomainException>(() => ScoreBreakdown.From(contributions));
    }

    [Fact]
    public void From_NegativeContribution_Throws()
    {
        var contributions = new Dictionary<string, double> { ["a"] = -0.1d };

        Assert.Throws<DomainException>(() => ScoreBreakdown.From(contributions));
    }

    [Fact]
    public void From_ContributionAboveOne_Throws()
    {
        var contributions = new Dictionary<string, double> { ["a"] = 1.5d };

        Assert.Throws<DomainException>(() => ScoreBreakdown.From(contributions));
    }

    [Fact]
    public void From_NonFiniteContribution_Throws()
    {
        var contributions = new Dictionary<string, double> { ["a"] = double.NaN };

        Assert.Throws<DomainException>(() => ScoreBreakdown.From(contributions));
    }

    [Fact]
    public void From_BlankKey_Throws()
    {
        var contributions = new Dictionary<string, double> { ["  "] = 0.1d };

        Assert.Throws<DomainException>(() => ScoreBreakdown.From(contributions));
    }
}
