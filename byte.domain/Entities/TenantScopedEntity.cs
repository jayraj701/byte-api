namespace Byte.Domain.Entities;

public abstract class TenantScopedEntity : AuditableEntity
{
    public string TenantId { get; set; } = string.Empty;
}
