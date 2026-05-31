using MatchOps.Domain.Common;
using MatchOps.Domain.Scheduling;

namespace MatchOps.Domain.Tests.Scheduling;

public class SlotSchedulerTests
{
    private static DateTimeOffset At(int hour) => new(2026, 5, 31, hour, 0, 0, TimeSpan.FromHours(9));

    private static TimeRange Range(int startHour, int endHour) => TimeRange.Create(At(startHour), At(endHour));

    private readonly SlotScheduler _scheduler = new();

    [Fact]
    public void OpenSlot_NoExisting_ReturnsOpenSlot()
    {
        var tenant = TenantId.New();
        var store = StoreId.New();
        var resource = Resource.Create(tenant, store, ResourceKind.Seat, "席1");

        var slot = _scheduler.OpenSlot([], tenant, store, resource, Range(10, 11));

        Assert.Equal(SlotStatus.Open, slot.Status);
    }

    [Fact]
    public void OpenSlot_OverlappingActiveSlot_Throws()
    {
        var tenant = TenantId.New();
        var store = StoreId.New();
        var resource = Resource.Create(tenant, store, ResourceKind.Seat, "席1");
        var existing = TimeSlot.Open(tenant, store, resource, Range(10, 12));

        Assert.Throws<DomainException>(
            () => _scheduler.OpenSlot([existing], tenant, store, resource, Range(11, 13)));
    }

    [Fact]
    public void OpenSlot_OverlapsOnlyClosedSlot_Succeeds()
    {
        var tenant = TenantId.New();
        var store = StoreId.New();
        var resource = Resource.Create(tenant, store, ResourceKind.Seat, "席1");
        var closed = TimeSlot.Open(tenant, store, resource, Range(10, 12));
        closed.Close();

        var slot = _scheduler.OpenSlot([closed], tenant, store, resource, Range(11, 13));

        Assert.Equal(SlotStatus.Open, slot.Status);
    }

    [Fact]
    public void OpenSlot_AdjacentExistingSlot_Succeeds()
    {
        var tenant = TenantId.New();
        var store = StoreId.New();
        var resource = Resource.Create(tenant, store, ResourceKind.Seat, "席1");
        var existing = TimeSlot.Open(tenant, store, resource, Range(10, 11));

        var slot = _scheduler.OpenSlot([existing], tenant, store, resource, Range(11, 12));

        Assert.Equal(SlotStatus.Open, slot.Status);
    }

    [Fact]
    public void OpenSlot_ResourceFromDifferentTenant_Throws()
    {
        var store = StoreId.New();
        var resource = Resource.Create(TenantId.New(), store, ResourceKind.Seat, "席1");

        Assert.Throws<DomainException>(
            () => _scheduler.OpenSlot([], TenantId.New(), store, resource, Range(10, 11)));
    }
}
