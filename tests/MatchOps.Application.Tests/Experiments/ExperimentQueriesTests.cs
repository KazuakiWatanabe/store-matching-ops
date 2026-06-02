using MatchOps.Application.Experiments;
using MatchOps.Domain.Common;
using MatchOps.Domain.Experiments;

namespace MatchOps.Application.Tests.Experiments;

public class ExperimentQueriesTests
{
    private static readonly DateTimeOffset Now = new(2026, 6, 2, 9, 0, 0, TimeSpan.Zero);

    private sealed class StubAssignmentRepository(IReadOnlyList<ExperimentAssignment> assignments)
        : IExperimentAssignmentRepository
    {
        public Task AddAsync(ExperimentAssignment assignment, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task<IReadOnlyList<ExperimentAssignment>> GetByExperimentAsync(
            ExperimentId experimentId, CancellationToken cancellationToken = default)
            => Task.FromResult(assignments);
    }

    private sealed class StubConversionStore(IReadOnlyList<ConversionRecord> conversions) : IConversionReadStore
    {
        public Task<IReadOnlyList<ConversionRecord>> GetByCampaignAsync(
            CampaignId campaignId, CancellationToken cancellationToken = default)
            => Task.FromResult(conversions);
    }

    [Fact]
    public async Task GetLiftAsync_NoAssignments_ReturnsNull()
    {
        var queries = new ExperimentQueries(new StubAssignmentRepository([]), new StubConversionStore([]));

        Assert.Null(await queries.GetLiftAsync(ExperimentId.New()));
    }

    [Fact]
    public async Task GetLiftAsync_TalliesArmsAndComputesLift()
    {
        var experimentId = ExperimentId.New();
        var campaignId = CampaignId.New();
        var tenantId = TenantId.New();

        // 処置群 3 名（うち 2 名 CV）、対照群 2 名（うち 1 名 CV）。
        var t1 = CustomerId.New();
        var t2 = CustomerId.New();
        var t3 = CustomerId.New();
        var c1 = CustomerId.New();
        var c2 = CustomerId.New();

        ExperimentAssignment Assign(CustomerId customer, ExperimentArm arm)
            => ExperimentAssignment.Create(experimentId, campaignId, customer, tenantId, arm, Now);

        var assignments = new List<ExperimentAssignment>
        {
            Assign(t1, ExperimentArm.Treatment),
            Assign(t2, ExperimentArm.Treatment),
            Assign(t3, ExperimentArm.Treatment),
            Assign(c1, ExperimentArm.Control),
            Assign(c2, ExperimentArm.Control),
        };
        var conversions = new List<ConversionRecord>
        {
            new(t1, 5000m),
            new(t2, 3000m),
            new(c1, 4000m),
        };

        var queries = new ExperimentQueries(
            new StubAssignmentRepository(assignments), new StubConversionStore(conversions));

        LiftResult result = (await queries.GetLiftAsync(experimentId))!;

        Assert.Equal(3, result.TreatmentCount);
        Assert.Equal(2, result.ControlCount);
        Assert.Equal(2d / 3d, result.TreatmentConversionRate, precision: 6);
        Assert.Equal(0.5d, result.ControlConversionRate, precision: 6);
        Assert.Equal((2d / 3d) - 0.5d, result.Lift, precision: 6);
    }
}
