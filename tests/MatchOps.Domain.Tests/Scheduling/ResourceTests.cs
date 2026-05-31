using MatchOps.Domain.Common;
using MatchOps.Domain.Scheduling;

namespace MatchOps.Domain.Tests.Scheduling;

public class ResourceTests
{
    [Fact]
    public void Create_SetsFieldsAndGeneratesId()
    {
        var tenantId = TenantId.New();
        var storeId = StoreId.New();

        var resource = Resource.Create(tenantId, storeId, ResourceKind.Staff, "  スタイリストA  ");

        Assert.NotEqual(default, resource.Id.Value);
        Assert.Equal(tenantId, resource.TenantId);
        Assert.Equal(storeId, resource.StoreId);
        Assert.Equal(ResourceKind.Staff, resource.Kind);
        Assert.Equal("スタイリストA", resource.Name);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_BlankName_Throws(string name)
    {
        Assert.Throws<DomainException>(
            () => Resource.Create(TenantId.New(), StoreId.New(), ResourceKind.Seat, name));
    }

    [Fact]
    public void ResourceId_ToString_ReturnsUnderlyingGuidString()
    {
        var guid = Guid.NewGuid();

        Assert.Equal(guid.ToString(), new ResourceId(guid).ToString());
    }
}
