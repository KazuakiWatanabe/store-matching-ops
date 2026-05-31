using MatchOps.Domain.Common;
using MatchOps.Domain.Matching;

namespace MatchOps.Domain.Tests.Matching;

public class ScoringPolicyTests
{
    [Fact]
    public void CreateV0Default_NormalizesToSumOne()
    {
        var policy = ScoringPolicy.CreateV0Default();

        Assert.Equal(1d, policy.Weights.Values.Sum(), precision: 9);
        Assert.Equal(0.40d, policy.Weights[ScoringFactors.Dormancy], precision: 9);
    }

    [Fact]
    public void Create_RelativeWeights_AreNormalized()
    {
        var policy = ScoringPolicy.Create(new Dictionary<string, double> { ["a"] = 2d, ["b"] = 2d });

        Assert.Equal(0.5d, policy.Weights["a"], precision: 9);
        Assert.Equal(0.5d, policy.Weights["b"], precision: 9);
    }

    [Fact]
    public void Create_Empty_Throws()
    {
        Assert.Throws<DomainException>(() => ScoringPolicy.Create(new Dictionary<string, double>()));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_NonPositiveWeight_Throws(double weight)
    {
        Assert.Throws<DomainException>(
            () => ScoringPolicy.Create(new Dictionary<string, double> { ["a"] = weight }));
    }

    [Fact]
    public void Create_BlankKey_Throws()
    {
        Assert.Throws<DomainException>(
            () => ScoringPolicy.Create(new Dictionary<string, double> { ["  "] = 0.5d }));
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void Create_NonFiniteWeight_Throws(double weight)
    {
        Assert.Throws<DomainException>(
            () => ScoringPolicy.Create(new Dictionary<string, double> { ["a"] = weight }));
    }

    [Fact]
    public void Score_AllFactorsPresentWithMaxValue_ReturnsOne()
    {
        var policy = ScoringPolicy.CreateV0Default();

        var breakdown = policy.Score(ScoreInputs.ForV0(1d, 1d, 1d));

        Assert.Equal(1d, breakdown.Total.Value, precision: 9);
        Assert.Equal(3, breakdown.Contributions.Count);
    }

    [Fact]
    public void Score_AllFactorsPresent_AppliesWeights()
    {
        var policy = ScoringPolicy.CreateV0Default();

        // 休眠のみ満点、他は 0 → 合計は休眠の重み 0.40 に一致。
        var breakdown = policy.Score(ScoreInputs.ForV0(1d, 0d, 0d));

        Assert.Equal(0.40d, breakdown.Total.Value, precision: 9);
        Assert.Equal(0.40d, breakdown.Contributions[ScoringFactors.Dormancy], precision: 9);
    }

    [Fact]
    public void Score_MissingFactor_RenormalizesRemainingWeights()
    {
        var policy = ScoringPolicy.CreateV0Default();

        // 曜日時間帯一致(0.25)を欠損 → 残り (0.40, 0.35) を合計 1 に再正規化。両者満点なら合計 1。
        var breakdown = policy.Score(ScoreInputs.ForV0(1d, 1d, null));

        Assert.Equal(1d, breakdown.Total.Value, precision: 9);
        Assert.Equal(2, breakdown.Contributions.Count);
        // 再正規化後の休眠寄与 = 0.40 / 0.75 ≈ 0.5333
        Assert.Equal(0.40d / 0.75d, breakdown.Contributions[ScoringFactors.Dormancy], precision: 9);
    }

    [Fact]
    public void Score_AllFactorsMissing_ReturnsZero()
    {
        var policy = ScoringPolicy.CreateV0Default();

        var breakdown = policy.Score(ScoreInputs.ForV0(null, null, null));

        Assert.Equal(MatchScore.Zero, breakdown.Total);
        Assert.Empty(breakdown.Contributions);
    }

    [Fact]
    public void Score_AlwaysWithinUnitInterval()
    {
        var policy = ScoringPolicy.CreateV0Default();

        var breakdown = policy.Score(ScoreInputs.ForV0(1d, 1d, 1d));

        Assert.InRange(breakdown.Total.Value, 0d, 1d);
    }

    [Fact]
    public void Score_BreakdownTotalMatchesSumOfContributions()
    {
        var policy = ScoringPolicy.CreateV0Default();

        var breakdown = policy.Score(ScoreInputs.ForV0(0.3d, 0.6d, 0.9d));

        Assert.Equal(breakdown.Contributions.Values.Sum(), breakdown.Total.Value, precision: 9);
    }
}
