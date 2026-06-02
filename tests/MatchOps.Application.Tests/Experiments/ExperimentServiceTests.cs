using MatchOps.Application.Common;
using MatchOps.Application.Experiments;
using MatchOps.Domain.Common;
using MatchOps.Domain.Experiments;

namespace MatchOps.Application.Tests.Experiments;

public class ExperimentServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 6, 2, 9, 0, 0, TimeSpan.Zero);

    private sealed class FakeClock : IClock
    {
        public DateTimeOffset Now => ExperimentServiceTests.Now;

        public DateOnly Today => DateOnly.FromDateTime(ExperimentServiceTests.Now.UtcDateTime);
    }

    private sealed class InMemoryAssignmentRepository : IExperimentAssignmentRepository
    {
        public List<ExperimentAssignment> Saved { get; } = [];

        public Task AddAsync(ExperimentAssignment assignment, CancellationToken cancellationToken = default)
        {
            Saved.Add(assignment);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<ExperimentAssignment>> GetByExperimentAsync(
            ExperimentId experimentId, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<ExperimentAssignment>>(
                Saved.Where(a => a.ExperimentId == experimentId).ToList());
    }

    private sealed class CountingUnitOfWork : IUnitOfWork
    {
        public int SaveCount { get; private set; }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveCount++;
            return Task.CompletedTask;
        }
    }

    private static AssignHoldoutCommand Command(
        ExperimentId experimentId, IReadOnlyList<CustomerId> customers, double ratio)
        => new(experimentId, CampaignId.New(), TenantId.New(), customers, ratio);

    [Fact]
    public async Task AssignAsync_PersistsAllAndReturnsTreatmentOnlyDeliverySet()
    {
        var repo = new InMemoryAssignmentRepository();
        var uow = new CountingUnitOfWork();
        var service = new ExperimentService(repo, uow, new FakeClock());
        var experimentId = ExperimentId.New();
        var customers = Enumerable.Range(0, 200).Select(_ => CustomerId.New()).ToList();

        Result<ExperimentAssignmentResult> result = await service.AssignAsync(Command(experimentId, customers, 0.2d));

        Assert.True(result.IsSuccess);
        Assert.Equal(1, uow.SaveCount);
        Assert.Equal(200, repo.Saved.Count);

        ExperimentAssignmentResult assignment = result.Value;
        Assert.Equal(customers.Count, assignment.TreatmentCount + assignment.ControlCount);

        // 配信対象は treatment のみ（control は含まれない）。
        var controlCustomers = repo.Saved.Where(a => a.Arm == ExperimentArm.Control).Select(a => a.CustomerId).ToHashSet();
        Assert.NotEmpty(controlCustomers);
        Assert.DoesNotContain(assignment.TreatmentCustomers, c => controlCustomers.Contains(c));
        Assert.Equal(assignment.TreatmentCount, assignment.TreatmentCustomers.Count);
    }

    [Fact]
    public async Task AssignAsync_IsReproducible_SameExperimentAndCustomers()
    {
        var experimentId = ExperimentId.New();
        var customers = Enumerable.Range(0, 100).Select(_ => CustomerId.New()).ToList();

        var repoA = new InMemoryAssignmentRepository();
        await new ExperimentService(repoA, new CountingUnitOfWork(), new FakeClock())
            .AssignAsync(Command(experimentId, customers, 0.3d));
        var repoB = new InMemoryAssignmentRepository();
        await new ExperimentService(repoB, new CountingUnitOfWork(), new FakeClock())
            .AssignAsync(Command(experimentId, customers, 0.3d));

        IEnumerable<(CustomerId, ExperimentArm)> armsA = repoA.Saved.Select(a => (a.CustomerId, a.Arm)).OrderBy(x => x.Item1.Value);
        IEnumerable<(CustomerId, ExperimentArm)> armsB = repoB.Saved.Select(a => (a.CustomerId, a.Arm)).OrderBy(x => x.Item1.Value);
        Assert.Equal(armsA, armsB);
    }

    [Fact]
    public async Task AssignAsync_NoCustomers_ReturnsFailure()
    {
        var service = new ExperimentService(new InMemoryAssignmentRepository(), new CountingUnitOfWork(), new FakeClock());

        Result<ExperimentAssignmentResult> result = await service.AssignAsync(Command(ExperimentId.New(), [], 0.2d));

        Assert.True(result.IsFailure);
        Assert.Equal("no_customers", result.ErrorCode);
    }

    [Fact]
    public async Task AssignAsync_InvalidRatio_ReturnsFailure()
    {
        var service = new ExperimentService(new InMemoryAssignmentRepository(), new CountingUnitOfWork(), new FakeClock());

        Result<ExperimentAssignmentResult> result = await service.AssignAsync(
            Command(ExperimentId.New(), [CustomerId.New()], 1.5d));

        Assert.True(result.IsFailure);
        Assert.Equal("invalid_control_ratio", result.ErrorCode);
    }
}
