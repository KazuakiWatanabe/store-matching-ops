using MatchOps.Domain.Common;
using MatchOps.Domain.Scheduling;

namespace MatchOps.Domain.Tests.Scheduling;

public class TimeSlotTests
{
    private static DateTimeOffset At(int hour) => new(2026, 5, 31, hour, 0, 0, TimeSpan.FromHours(9));

    private static TimeRange Range(int startHour, int endHour) => TimeRange.Create(At(startHour), At(endHour));

    private static (TenantId Tenant, StoreId Store, Resource Resource) NewContext()
    {
        var tenant = TenantId.New();
        var store = StoreId.New();
        var resource = Resource.Create(tenant, store, ResourceKind.Seat, "カウンター1");
        return (tenant, store, resource);
    }

    private static TimeSlot OpenSlot(IEnumerable<string>? categories = null)
    {
        var (tenant, store, resource) = NewContext();
        return TimeSlot.Open(tenant, store, resource, Range(10, 11), categories);
    }

    [Fact]
    public void Open_SetsOpenStatusAndFields()
    {
        var (tenant, store, resource) = NewContext();

        var slot = TimeSlot.Open(tenant, store, resource, Range(10, 11), new[] { "haircut", "  color  " });

        Assert.Equal(SlotStatus.Open, slot.Status);
        Assert.Equal(tenant, slot.TenantId);
        Assert.Equal(store, slot.StoreId);
        Assert.Equal(resource.Id, slot.ResourceId);
        Assert.Equal(Range(10, 11), slot.Range);
        Assert.Contains("haircut", slot.SupportedOfferCategories);
        Assert.Contains("color", slot.SupportedOfferCategories); // トリム済み
    }

    [Fact]
    public void Open_NoCategories_HasEmptySet()
    {
        Assert.Empty(OpenSlot().SupportedOfferCategories);
    }

    [Fact]
    public void Open_BlankCategory_Throws()
    {
        var (tenant, store, resource) = NewContext();

        Assert.Throws<DomainException>(
            () => TimeSlot.Open(tenant, store, resource, Range(10, 11), new[] { "ok", "  " }));
    }

    [Fact]
    public void Open_ResourceFromDifferentTenant_Throws()
    {
        var store = StoreId.New();
        var resource = Resource.Create(TenantId.New(), store, ResourceKind.Seat, "席");

        Assert.Throws<DomainException>(
            () => TimeSlot.Open(TenantId.New(), store, resource, Range(10, 11)));
    }

    [Fact]
    public void Open_ResourceFromDifferentStore_Throws()
    {
        var tenant = TenantId.New();
        var resource = Resource.Create(tenant, StoreId.New(), ResourceKind.Seat, "席");

        Assert.Throws<DomainException>(
            () => TimeSlot.Open(tenant, StoreId.New(), resource, Range(10, 11)));
    }

    [Fact]
    public void Open_NullResource_Throws()
    {
        Assert.Throws<ArgumentNullException>(
            () => TimeSlot.Open(TenantId.New(), StoreId.New(), null!, Range(10, 11)));
    }

    [Fact]
    public void Hold_Then_Book_FollowsHappyPath()
    {
        var slot = OpenSlot();

        slot.Hold();
        Assert.Equal(SlotStatus.Held, slot.Status);

        slot.Book();
        Assert.Equal(SlotStatus.Booked, slot.Status);
    }

    [Fact]
    public void Release_FromHeld_ReturnsToOpen()
    {
        var slot = OpenSlot();
        slot.Hold();

        slot.Release();

        Assert.Equal(SlotStatus.Open, slot.Status);
    }

    [Fact]
    public void Book_FromOpen_Throws()
    {
        var slot = OpenSlot();

        Assert.Throws<DomainException>(() => slot.Book());
    }

    [Fact]
    public void Hold_FromBooked_Throws()
    {
        var slot = OpenSlot();
        slot.Hold();
        slot.Book();

        Assert.Throws<DomainException>(() => slot.Hold());
    }

    [Fact]
    public void Release_FromOpen_Throws()
    {
        var slot = OpenSlot();

        Assert.Throws<DomainException>(() => slot.Release());
    }

    [Fact]
    public void Close_FromOpenHeldBooked_Succeeds()
    {
        var open = OpenSlot();
        open.Close();
        Assert.Equal(SlotStatus.Closed, open.Status);

        var held = OpenSlot();
        held.Hold();
        held.Close();
        Assert.Equal(SlotStatus.Closed, held.Status);

        var booked = OpenSlot();
        booked.Hold();
        booked.Book();
        booked.Close();
        Assert.Equal(SlotStatus.Closed, booked.Status);
    }

    [Fact]
    public void Close_WhenAlreadyClosed_Throws()
    {
        var slot = OpenSlot();
        slot.Close();

        Assert.Throws<DomainException>(() => slot.Close());
    }

    [Fact]
    public void Hold_FromClosed_Throws()
    {
        var slot = OpenSlot();
        slot.Close();

        Assert.Throws<DomainException>(() => slot.Hold());
    }

    [Fact]
    public void OverlapsWith_SameResourceOverlappingTime_ReturnsTrue()
    {
        var (tenant, store, resource) = NewContext();
        var a = TimeSlot.Open(tenant, store, resource, Range(10, 12));
        var b = TimeSlot.Open(tenant, store, resource, Range(11, 13));

        Assert.True(a.OverlapsWith(b));
    }

    [Fact]
    public void OverlapsWith_DifferentResource_ReturnsFalse()
    {
        var (tenant, store, _) = NewContext();
        var resourceA = Resource.Create(tenant, store, ResourceKind.Seat, "席1");
        var resourceB = Resource.Create(tenant, store, ResourceKind.Seat, "席2");
        var a = TimeSlot.Open(tenant, store, resourceA, Range(10, 12));
        var b = TimeSlot.Open(tenant, store, resourceB, Range(10, 12));

        Assert.False(a.OverlapsWith(b));
    }

    [Fact]
    public void OverlapsWith_SameResourceAdjacentTime_ReturnsFalse()
    {
        var (tenant, store, resource) = NewContext();
        var a = TimeSlot.Open(tenant, store, resource, Range(10, 11));
        var b = TimeSlot.Open(tenant, store, resource, Range(11, 12));

        Assert.False(a.OverlapsWith(b));
    }

    [Fact]
    public void OverlapsWith_Null_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => OpenSlot().OverlapsWith(null!));
    }
}
