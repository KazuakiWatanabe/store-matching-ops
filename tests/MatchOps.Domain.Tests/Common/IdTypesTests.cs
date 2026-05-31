using MatchOps.Domain.Common;

namespace MatchOps.Domain.Tests.Common;

public class IdTypesTests
{
    [Fact]
    public void New_ProducesDistinctValues_ForEachIdType()
    {
        Assert.NotEqual(TenantId.New(), TenantId.New());
        Assert.NotEqual(StoreId.New(), StoreId.New());
        Assert.NotEqual(CustomerId.New(), CustomerId.New());
        Assert.NotEqual(TimeSlotId.New(), TimeSlotId.New());
        Assert.NotEqual(OfferId.New(), OfferId.New());
        Assert.NotEqual(CampaignId.New(), CampaignId.New());
    }

    [Fact]
    public void SameUnderlyingGuid_AreEqualWithSameHashCode()
    {
        var guid = Guid.NewGuid();

        var a = new CustomerId(guid);
        var b = new CustomerId(guid);

        Assert.Equal(a, b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
        Assert.Equal(guid, a.Value);
    }

    [Fact]
    public void ToString_ReturnsUnderlyingGuidString_ForEachIdType()
    {
        var guid = Guid.NewGuid();
        var expected = guid.ToString();

        Assert.Equal(expected, new TenantId(guid).ToString());
        Assert.Equal(expected, new StoreId(guid).ToString());
        Assert.Equal(expected, new CustomerId(guid).ToString());
        Assert.Equal(expected, new TimeSlotId(guid).ToString());
        Assert.Equal(expected, new OfferId(guid).ToString());
        Assert.Equal(expected, new CampaignId(guid).ToString());
    }
}
