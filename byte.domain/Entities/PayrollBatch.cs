namespace Byte.Domain.Entities;

public class PayrollBatch : AuditableEntity
{
    public string FileName { get; set; } = string.Empty;
    public DateTime UploadedAt { get; set; }
    public string BatchStatus { get; set; } = "Pending";
    public string? ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }
}
