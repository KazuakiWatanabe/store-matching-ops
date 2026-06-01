using MatchOps.Domain.Common;
using MatchOps.Domain.Experiments;

namespace MatchOps.Domain.Tests.Experiments;

public class HoldoutAssignmentPolicyTests
{
    private readonly HoldoutAssignmentPolicy _policy = new();

    [Fact]
    public void Assign_SameInput_IsDeterministic()
    {
        var experimentId = ExperimentId.New();
        var customerId = CustomerId.New();

        ExperimentArm first = _policy.Assign(experimentId, customerId, 0.2d);
        ExperimentArm second = new HoldoutAssignmentPolicy().Assign(experimentId, customerId, 0.2d);

        Assert.Equal(first, second);
    }

    [Fact]
    public void Assign_RatioZero_AllTreatment()
    {
        var experimentId = ExperimentId.New();

        for (int i = 0; i < 200; i++)
        {
            Assert.Equal(ExperimentArm.Treatment, _policy.Assign(experimentId, CustomerId.New(), 0d));
        }
    }

    [Fact]
    public void Assign_RatioOne_AllControl()
    {
        var experimentId = ExperimentId.New();

        for (int i = 0; i < 200; i++)
        {
            Assert.Equal(ExperimentArm.Control, _policy.Assign(experimentId, CustomerId.New(), 1d));
        }
    }

    [Fact]
    public void Assign_Ratio20Percent_SplitsWithinTolerance()
    {
        var experimentId = ExperimentId.New();
        const int total = 10_000;
        int control = 0;

        for (int i = 0; i < total; i++)
        {
            if (_policy.Assign(experimentId, CustomerId.New(), 0.2d) == ExperimentArm.Control)
            {
                control++;
            }
        }

        double fraction = (double)control / total;
        Assert.InRange(fraction, 0.17d, 0.23d);
    }

    [Theory]
    [InlineData(-0.1d)]
    [InlineData(1.1d)]
    [InlineData(double.NaN)]
    public void Assign_InvalidRatio_Throws(double ratio)
        => Assert.Throws<DomainException>(() => _policy.Assign(ExperimentId.New(), CustomerId.New(), ratio));
}
