using Byte.Domain.Entities;

namespace Byte.Tests.Domain;

public class TenantScopedEntityTests
{
    private sealed class ConcreteEntity : TenantScopedEntity { }

    [Fact]
    public void TenantId_ShouldBeEmpty_WhenCreated()
    {
        var entity = new ConcreteEntity();

        Assert.Equal(string.Empty, entity.TenantId);
    }

    [Fact]
    public void TenantId_ShouldBeSettable()
    {
        var entity = new ConcreteEntity { TenantId = "tenant-abc" };

        Assert.Equal("tenant-abc", entity.TenantId);
    }

    [Fact]
    public void InheritsAuditableEntity_ShouldHaveGeneratedId()
    {
        var entity = new ConcreteEntity();

        Assert.NotEqual(Guid.Empty, entity.Id);
    }
}
