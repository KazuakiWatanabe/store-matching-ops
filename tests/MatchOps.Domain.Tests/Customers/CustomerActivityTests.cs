using MatchOps.Domain.Common;
using MatchOps.Domain.Customers;

namespace MatchOps.Domain.Tests.Customers;

public class CustomerActivityTests
{
    [Fact]
    public void Record_SetsAllFieldsAndGeneratesId()
    {
        var tenantId = TenantId.New();
        var customerId = CustomerId.New();
        var occurredOn = new DateOnly(2026, 5, 1);

        var activity = CustomerActivity.Record(
            tenantId, customerId, ActivityType.Order, occurredOn, Money.Jpy(3000m));

        Assert.NotEqual(default, activity.Id.Value);
        Assert.Equal(tenantId, activity.TenantId);
        Assert.Equal(customerId, activity.CustomerId);
        Assert.Equal(ActivityType.Order, activity.Type);
        Assert.Equal(occurredOn, activity.OccurredOn);
        Assert.Equal(Money.Jpy(3000m), activity.Amount);
    }

    [Fact]
    public void Record_WithoutAmount_AmountIsNull()
    {
        var activity = CustomerActivity.Record(
            TenantId.New(), CustomerId.New(), ActivityType.Visit, new DateOnly(2026, 5, 1));

        Assert.Null(activity.Amount);
    }

    [Fact]
    public void Record_ProducesDistinctIds()
    {
        var a = CustomerActivity.Record(TenantId.New(), CustomerId.New(), ActivityType.Visit, new DateOnly(2026, 5, 1));
        var b = CustomerActivity.Record(TenantId.New(), CustomerId.New(), ActivityType.Visit, new DateOnly(2026, 5, 1));

        Assert.NotEqual(a.Id, b.Id);
    }

    [Fact]
    public void ActivityId_ToString_ReturnsUnderlyingGuidString()
    {
        var guid = Guid.NewGuid();

        Assert.Equal(guid.ToString(), new CustomerActivityId(guid).ToString());
    }
}
