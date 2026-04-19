namespace Byte.Domain.Entities;

public class PayrollCalculation : AuditableEntity
{
    public Guid PayrollRecordId { get; set; }
    public Guid BatchId { get; set; }
    public decimal BasePay { get; set; }
    public decimal SiteAllowance { get; set; }
    public decimal GrossPay { get; set; }
    public decimal NetPay { get; set; }
    public string Status { get; set; } = "Pending";
}
