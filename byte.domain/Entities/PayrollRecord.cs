namespace Byte.Domain.Entities;

public class PayrollRecord : AuditableEntity
{
    public string WorkerId { get; set; } = string.Empty;
    public string WorkerName { get; set; } = string.Empty;
    public string Site { get; set; } = string.Empty;
    public int DaysPresent { get; set; }
    public decimal DayRate { get; set; }
    public decimal AdvanceDeduction { get; set; }
    public Guid BatchId { get; set; }
}
