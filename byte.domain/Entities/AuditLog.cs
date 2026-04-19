namespace Byte.Domain.Entities;

public class AuditLog : AuditableEntity
{
    public string EventType { get; set; } = string.Empty;
    public Guid? BatchId { get; set; }
    public string Actor { get; set; } = string.Empty;
    public string Detail { get; set; } = string.Empty;
    public DateTime OccurredAt { get; set; }
}
