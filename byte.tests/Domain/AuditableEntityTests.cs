using Byte.Domain.Entities;

namespace Byte.Tests.Domain;

public class AuditableEntityTests
{
    private sealed class ConcreteEntity : AuditableEntity { }

    [Fact]
    public void Id_ShouldNotBeEmpty_WhenCreated()
    {
        var entity = new ConcreteEntity();

        Assert.NotEqual(Guid.Empty, entity.Id);
    }

    [Fact]
    public void TwoEntities_ShouldHaveDifferentIds()
    {
        var entity1 = new ConcreteEntity();
        var entity2 = new ConcreteEntity();

        Assert.NotEqual(entity1.Id, entity2.Id);
    }

    [Fact]
    public void AuditFields_ShouldBeNullOrDefault_BeforePersistence()
    {
        var entity = new ConcreteEntity();

        Assert.Equal(default, entity.CreatedAt);
        Assert.Equal(default, entity.UpdatedAt);
        Assert.Null(entity.CreatedBy);
        Assert.Null(entity.UpdatedBy);
    }
}
