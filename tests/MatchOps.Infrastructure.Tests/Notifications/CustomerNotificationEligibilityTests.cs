using MatchOps.Domain.Common;
using MatchOps.Domain.Customers;
using MatchOps.Infrastructure.Notifications;
using MatchOps.Infrastructure.Persistence;
using MatchOps.Infrastructure.Tests.Persistence;

namespace MatchOps.Infrastructure.Tests.Notifications;

public sealed class CustomerNotificationEligibilityTests(PostgresFixture fixture) : IClassFixture<PostgresFixture>
{
    private static readonly DateOnly Today = new(2026, 6, 2);

    private async Task<CustomerId> SeedCustomerAsync(TenantId tenant, OptInStatus optIn)
    {
        var customer = Customer.Register(tenant, StoreId.New(), "テスト顧客", optInStatus: optIn);
        await using MatchOpsDbContext seed = fixture.CreateContext(tenant);
        seed.Customers.Add(customer);
        await seed.SaveChangesAsync();
        return customer.Id;
    }

    [Fact]
    public async Task OptedInCustomer_IsAllowed()
    {
        var tenant = TenantId.New();
        CustomerId customerId = await SeedCustomerAsync(tenant, OptInStatus.OptedIn);

        await using MatchOpsDbContext context = fixture.CreateContext(tenant: null);
        var eligibility = new CustomerNotificationEligibility(context);

        Assert.True(await eligibility.IsAllowedAsync(tenant, customerId, Today));
    }

    [Fact]
    public async Task OptedOutCustomer_IsNotAllowed()
    {
        var tenant = TenantId.New();
        CustomerId customerId = await SeedCustomerAsync(tenant, OptInStatus.OptedOut);

        await using MatchOpsDbContext context = fixture.CreateContext(tenant: null);
        var eligibility = new CustomerNotificationEligibility(context);

        Assert.False(await eligibility.IsAllowedAsync(tenant, customerId, Today));
    }

    [Fact]
    public async Task UnknownCustomer_IsNotAllowed()
    {
        await using MatchOpsDbContext context = fixture.CreateContext(tenant: null);
        var eligibility = new CustomerNotificationEligibility(context);

        Assert.False(await eligibility.IsAllowedAsync(TenantId.New(), CustomerId.New(), Today));
    }
}
