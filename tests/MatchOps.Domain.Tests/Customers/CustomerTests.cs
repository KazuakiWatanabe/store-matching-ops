using MatchOps.Domain.Common;
using MatchOps.Domain.Customers;

namespace MatchOps.Domain.Tests.Customers;

public class CustomerTests
{
    private const string Sha256Hex = "50d858e0985ecc7f60418aaf0cc5ab587f42c2570a884095a9e8ccacd0f6545c";

    private static Customer NewCustomer(OptInStatus status = OptInStatus.Unknown)
        => Customer.Register(TenantId.New(), StoreId.New(), "山田", optInStatus: status);

    [Fact]
    public void Register_SetsDefaultsAndFields()
    {
        var tenantId = TenantId.New();
        var storeId = StoreId.New();
        var phone = ContactHash.From(Sha256Hex);

        var customer = Customer.Register(tenantId, storeId, "山田 太", phone);

        Assert.NotEqual(default, customer.Id.Value);
        Assert.Equal(tenantId, customer.TenantId);
        Assert.Equal(storeId, customer.StoreId);
        Assert.Equal("山田 太", customer.DisplayName);
        Assert.Equal(phone, customer.PhoneHash);
        Assert.Null(customer.EmailHash);
        Assert.Equal(0, customer.VisitCount);
        Assert.Null(customer.LastVisitOn);
        Assert.Equal(OptInStatus.Unknown, customer.OptInStatus);
        Assert.Null(customer.LastNotifiedOn);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Register_BlankDisplayName_Throws(string displayName)
    {
        Assert.Throws<DomainException>(
            () => Customer.Register(TenantId.New(), StoreId.New(), displayName));
    }

    [Fact]
    public void Register_DisplayNameIsTrimmed()
    {
        var customer = Customer.Register(TenantId.New(), StoreId.New(), "  山田  ");

        Assert.Equal("山田", customer.DisplayName);
    }

    [Fact]
    public void IsOptedOut_TrueOnlyWhenOptedOut()
    {
        Assert.True(NewCustomer(OptInStatus.OptedOut).IsOptedOut);
        Assert.False(NewCustomer(OptInStatus.OptedIn).IsOptedOut);
        Assert.False(NewCustomer(OptInStatus.Unknown).IsOptedOut);
    }

    [Fact]
    public void CanReceiveNotifications_TrueOnlyWhenOptedIn()
    {
        Assert.True(NewCustomer(OptInStatus.OptedIn).CanReceiveNotifications());
        Assert.False(NewCustomer(OptInStatus.Unknown).CanReceiveNotifications());
        Assert.False(NewCustomer(OptInStatus.OptedOut).CanReceiveNotifications());
    }

    [Fact]
    public void OptIn_And_OptOut_ChangeStatus()
    {
        var customer = NewCustomer();

        customer.OptIn();
        Assert.Equal(OptInStatus.OptedIn, customer.OptInStatus);

        customer.OptOut();
        Assert.Equal(OptInStatus.OptedOut, customer.OptInStatus);
    }

    [Fact]
    public void RecordActivity_Visit_IncrementsCountAndTracksLatestDate()
    {
        var customer = NewCustomer();

        customer.RecordActivity(Visit(customer, new DateOnly(2026, 4, 1)));
        customer.RecordActivity(Visit(customer, new DateOnly(2026, 5, 10)));
        customer.RecordActivity(Visit(customer, new DateOnly(2026, 4, 20))); // 過去日でも最新は維持

        Assert.Equal(3, customer.VisitCount);
        Assert.Equal(new DateOnly(2026, 5, 10), customer.LastVisitOn);
    }

    [Fact]
    public void RecordActivity_NonVisit_DoesNotChangeVisitStats()
    {
        var customer = NewCustomer();

        customer.RecordActivity(CustomerActivity.Record(
            customer.TenantId, customer.Id, ActivityType.Cancellation, new DateOnly(2026, 5, 1)));

        Assert.Equal(0, customer.VisitCount);
        Assert.Null(customer.LastVisitOn);
    }

    [Fact]
    public void RecordActivity_DifferentTenant_Throws()
    {
        var customer = NewCustomer();
        var foreign = CustomerActivity.Record(
            TenantId.New(), customer.Id, ActivityType.Visit, new DateOnly(2026, 5, 1));

        Assert.Throws<DomainException>(() => customer.RecordActivity(foreign));
    }

    [Fact]
    public void RecordActivity_DifferentCustomer_Throws()
    {
        var customer = NewCustomer();
        var foreign = CustomerActivity.Record(
            customer.TenantId, CustomerId.New(), ActivityType.Visit, new DateOnly(2026, 5, 1));

        Assert.Throws<DomainException>(() => customer.RecordActivity(foreign));
    }

    [Fact]
    public void RecordActivity_Null_Throws()
    {
        var customer = NewCustomer();

        Assert.Throws<ArgumentNullException>(() => customer.RecordActivity(null!));
    }

    [Fact]
    public void DaysSinceLastVisit_NoVisit_ReturnsNull()
    {
        Assert.Null(NewCustomer().DaysSinceLastVisit(new DateOnly(2026, 5, 31)));
    }

    [Fact]
    public void DaysSinceLastVisit_AfterVisit_ReturnsElapsedDays()
    {
        var customer = NewCustomer();
        customer.RecordActivity(Visit(customer, new DateOnly(2026, 5, 1)));

        Assert.Equal(30, customer.DaysSinceLastVisit(new DateOnly(2026, 5, 31)));
    }

    [Fact]
    public void MarkNotified_SetsLastNotifiedOn()
    {
        var customer = NewCustomer();

        customer.MarkNotified(new DateOnly(2026, 5, 31));

        Assert.Equal(new DateOnly(2026, 5, 31), customer.LastNotifiedOn);
    }

    private static CustomerActivity Visit(Customer customer, DateOnly on)
        => CustomerActivity.Record(customer.TenantId, customer.Id, ActivityType.Visit, on);
}
