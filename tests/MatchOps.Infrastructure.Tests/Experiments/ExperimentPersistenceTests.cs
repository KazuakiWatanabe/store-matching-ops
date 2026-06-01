using MatchOps.Application.Experiments;
using MatchOps.Domain.Common;
using MatchOps.Domain.Experiments;
using MatchOps.Infrastructure.Experiments;
using MatchOps.Infrastructure.Persistence;
using MatchOps.Infrastructure.Tests.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MatchOps.Infrastructure.Tests.Experiments;

public sealed class ExperimentPersistenceTests(PostgresFixture fixture) : IClassFixture<PostgresFixture>
{
    private static readonly DateTimeOffset Now = new(2026, 6, 2, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task ExperimentAssignment_RoundTrips_WithArm()
    {
        var tenant = TenantId.New();
        var experimentId = ExperimentId.New();
        var campaignId = CampaignId.New();
        var customerId = CustomerId.New();
        var assignment = ExperimentAssignment.Create(
            experimentId, campaignId, customerId, tenant, ExperimentArm.Treatment, Now);

        await using (MatchOpsDbContext write = fixture.CreateContext(tenant))
        {
            write.ExperimentAssignments.Add(assignment);
            await write.SaveChangesAsync();
        }

        await using MatchOpsDbContext read = fixture.CreateContext(tenant);
        ExperimentAssignment loaded = await read.ExperimentAssignments
            .SingleAsync(a => a.ExperimentId == experimentId && a.CustomerId == customerId);

        Assert.Equal(ExperimentArm.Treatment, loaded.Arm);
        Assert.Equal(campaignId, loaded.CampaignId);
        Assert.Equal(Now, loaded.AssignedAt);
    }

    [Fact]
    public async Task ExperimentAssignment_QueryFilter_ExcludesOtherTenant()
    {
        var tenantA = TenantId.New();
        var tenantB = TenantId.New();
        var experimentId = ExperimentId.New();
        var assignment = ExperimentAssignment.Create(
            experimentId, CampaignId.New(), CustomerId.New(), tenantA, ExperimentArm.Control, Now);

        await using (MatchOpsDbContext write = fixture.CreateContext(tenantA))
        {
            write.ExperimentAssignments.Add(assignment);
            await write.SaveChangesAsync();
        }

        await using MatchOpsDbContext read = fixture.CreateContext(tenantB);
        var repo = new EfExperimentAssignmentRepository(read);
        IReadOnlyList<ExperimentAssignment> visible = await repo.GetByExperimentAsync(experimentId);

        Assert.Empty(visible);
    }

    [Fact]
    public async Task LiftQuery_OverPersistedAssignmentsAndConversions_ComputesLift()
    {
        var tenant = TenantId.New();
        var experimentId = ExperimentId.New();
        var campaignId = CampaignId.New();
        var t1 = CustomerId.New();
        var t2 = CustomerId.New();
        var control = CustomerId.New();

        await using (MatchOpsDbContext write = fixture.CreateContext(tenant))
        {
            write.ExperimentAssignments.Add(ExperimentAssignment.Create(experimentId, campaignId, t1, tenant, ExperimentArm.Treatment, Now));
            write.ExperimentAssignments.Add(ExperimentAssignment.Create(experimentId, campaignId, t2, tenant, ExperimentArm.Treatment, Now));
            write.ExperimentAssignments.Add(ExperimentAssignment.Create(experimentId, campaignId, control, tenant, ExperimentArm.Control, Now));
            // 処置群 t1 のみ CV（売上 8000）。対照群は CV なし。
            write.ConversionEvents.Add(new ConversionEventEntity
            {
                TenantId = tenant,
                CampaignId = campaignId,
                CustomerId = t1,
                Kind = "visit",
                Revenue = 8000m,
                OccurredAt = Now,
            });
            await write.SaveChangesAsync();
        }

        await using MatchOpsDbContext read = fixture.CreateContext(tenant);
        var queries = new ExperimentQueries(
            new EfExperimentAssignmentRepository(read), new EfConversionReadStore(read));

        LiftResult result = (await queries.GetLiftAsync(experimentId))!;

        Assert.Equal(2, result.TreatmentCount);
        Assert.Equal(1, result.ControlCount);
        Assert.Equal(0.5d, result.TreatmentConversionRate, precision: 6);
        Assert.Equal(0d, result.ControlConversionRate, precision: 6);
        Assert.Equal(0.5d, result.Lift, precision: 6);
        Assert.Equal(1d, result.IncrementalConversions, precision: 6); // 0.5 × 2
        Assert.Equal(8000m, result.IncrementalRevenue); // 1 件 × 客単価 8000
    }
}
