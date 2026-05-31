using MatchOps.Domain.Catalog;
using MatchOps.Domain.Common;
using MatchOps.Domain.Customers;
using MatchOps.Domain.Scheduling;
using MatchOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MatchOps.Infrastructure.Tests.Persistence;

public sealed class PersistenceIntegrationTests(PostgresFixture fixture) : IClassFixture<PostgresFixture>
{
    private const string Sha256Hex = "50d858e0985ecc7f60418aaf0cc5ab587f42c2570a884095a9e8ccacd0f6545c";

    [Fact]
    public async Task Migration_AppliesToEmptyDatabase_TablesAreQueryable()
    {
        await using MatchOpsDbContext context = fixture.CreateContext(TenantId.New());

        // マイグレーションが空 DB に適用済みで、テーブルが参照できる。
        int count = await context.Customers.CountAsync();

        Assert.Equal(0, count);
    }

    [Fact]
    public async Task Customer_RoundTrips_StrongTypedIdAndValueObjects()
    {
        var tenant = TenantId.New();
        var customer = Customer.Register(
            tenant, StoreId.New(), "山田 太", ContactHash.From(Sha256Hex), optInStatus: OptInStatus.OptedIn);

        await using (MatchOpsDbContext write = fixture.CreateContext(tenant))
        {
            write.Customers.Add(customer);
            await write.SaveChangesAsync();
        }

        await using MatchOpsDbContext read = fixture.CreateContext(tenant);
        Customer loaded = await read.Customers.SingleAsync(c => c.Id == customer.Id);

        Assert.Equal(customer.Id, loaded.Id);
        Assert.Equal(tenant, loaded.TenantId);
        Assert.Equal("山田 太", loaded.DisplayName);
        Assert.Equal(customer.PhoneHash, loaded.PhoneHash);
        Assert.Equal(OptInStatus.OptedIn, loaded.OptInStatus);
    }

    [Fact]
    public async Task TimeSlot_RoundTrips_JsonbTimeRangeAndCategories()
    {
        var tenant = TenantId.New();
        var store = StoreId.New();
        var resource = Resource.Create(tenant, store, ResourceKind.Seat, "席1");
        var range = TimeRange.Create(
            new DateTimeOffset(2026, 6, 1, 10, 0, 0, TimeSpan.FromHours(9)),
            new DateTimeOffset(2026, 6, 1, 11, 0, 0, TimeSpan.FromHours(9)));
        var slot = TimeSlot.Open(tenant, store, resource, range, new[] { "haircut", "color" });

        await using (MatchOpsDbContext write = fixture.CreateContext(tenant))
        {
            write.TimeSlots.Add(slot);
            await write.SaveChangesAsync();
        }

        await using MatchOpsDbContext read = fixture.CreateContext(tenant);
        TimeSlot loaded = await read.TimeSlots.SingleAsync(s => s.Id == slot.Id);

        Assert.Equal(range, loaded.Range);
        Assert.Equal(SlotStatus.Open, loaded.Status);
        Assert.Contains("haircut", loaded.SupportedOfferCategories);
        Assert.Contains("color", loaded.SupportedOfferCategories);
    }

    [Fact]
    public async Task Offer_Coupon_RoundTrips_JsonbDiscountCap()
    {
        var tenant = TenantId.New();
        var coupon = Offer.CreateCoupon(
            tenant, StoreId.New(), "1000円引き", DiscountCap.Amount(Money.Jpy(1000m)));

        await using (MatchOpsDbContext write = fixture.CreateContext(tenant))
        {
            write.Offers.Add(coupon);
            await write.SaveChangesAsync();
        }

        await using MatchOpsDbContext read = fixture.CreateContext(tenant);
        Offer loaded = await read.Offers.SingleAsync(o => o.Id == coupon.Id);

        Assert.Equal(OfferType.Coupon, loaded.Type);
        Assert.NotNull(loaded.DiscountCap);
        Assert.Equal(DiscountKind.Amount, loaded.DiscountCap!.Value.Kind);
        Assert.Equal(Money.Jpy(1000m), loaded.DiscountCap.Value.MaxAmount);
        Assert.True(loaded.IsActive);
    }

    [Fact]
    public async Task QueryFilter_ExcludesOtherTenantsData()
    {
        var tenantA = TenantId.New();
        var tenantB = TenantId.New();
        var store = StoreId.New();
        var customerA = Customer.Register(tenantA, store, "A");
        var customerB = Customer.Register(tenantB, store, "B");

        await using (MatchOpsDbContext write = fixture.CreateContext(tenantA))
        {
            write.Customers.Add(customerA);
            await write.SaveChangesAsync();
        }

        await using (MatchOpsDbContext write = fixture.CreateContext(tenantB))
        {
            write.Customers.Add(customerB);
            await write.SaveChangesAsync();
        }

        await using MatchOpsDbContext read = fixture.CreateContext(tenantA);
        List<Customer> visible = await read.Customers.ToListAsync();

        Assert.All(visible, c => Assert.Equal(tenantA, c.TenantId));
        Assert.Contains(visible, c => c.Id == customerA.Id);
        Assert.DoesNotContain(visible, c => c.Id == customerB.Id);
    }

    [Fact]
    public async Task QueryFilter_WithoutTenant_ReturnsNothing()
    {
        var tenant = TenantId.New();
        await using (MatchOpsDbContext write = fixture.CreateContext(tenant))
        {
            write.Customers.Add(Customer.Register(tenant, StoreId.New(), "X"));
            await write.SaveChangesAsync();
        }

        // テナント未解決のコンテキストは何も返さない（安全側）。
        await using MatchOpsDbContext read = fixture.CreateContext(tenant: null);
        Assert.Empty(await read.Customers.ToListAsync());
    }
}
