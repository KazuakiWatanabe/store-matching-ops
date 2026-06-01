using MatchOps.Domain.Experiments;

namespace MatchOps.Domain.Tests.Experiments;

public class LiftResultTests
{
    [Fact]
    public void Calculate_KnownData_ReturnsExpectedLiftAndIncrements()
    {
        // 処置群 100 名中 20 CV（売上 200,000）、対照群 100 名中 10 CV。
        ArmOutcome treatment = ArmOutcome.Of(count: 100, conversions: 20, revenue: 200_000m);
        ArmOutcome control = ArmOutcome.Of(count: 100, conversions: 10, revenue: 0m);

        LiftResult result = LiftResult.Calculate(treatment, control);

        Assert.Equal(0.2d, result.TreatmentConversionRate, precision: 6);
        Assert.Equal(0.1d, result.ControlConversionRate, precision: 6);
        Assert.Equal(0.1d, result.Lift, precision: 6);
        Assert.Equal(10d, result.IncrementalConversions, precision: 6); // 0.1 × 100
        Assert.Equal(100_000m, result.IncrementalRevenue); // 10 件 × 客単価 10,000
    }

    [Fact]
    public void Calculate_EmptyArms_YieldsZero()
    {
        LiftResult result = LiftResult.Calculate(ArmOutcome.Of(0, 0, 0m), ArmOutcome.Of(0, 0, 0m));

        Assert.Equal(0d, result.Lift);
        Assert.Equal(0d, result.IncrementalConversions);
        Assert.Equal(0m, result.IncrementalRevenue);
    }

    [Fact]
    public void Calculate_NegativeLift_WhenControlOutperforms()
    {
        ArmOutcome treatment = ArmOutcome.Of(100, 5, 50_000m);
        ArmOutcome control = ArmOutcome.Of(100, 10, 0m);

        LiftResult result = LiftResult.Calculate(treatment, control);

        Assert.True(result.Lift < 0d);
        Assert.True(result.IncrementalConversions < 0d);
    }
}
