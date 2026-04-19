namespace Byte.Api.Services.Models;

public record SummaryDto(
    string WorkerId,
    string WorkerName,
    string Site,
    int DaysPresent,
    decimal BasePay,
    decimal SiteAllowance,
    decimal GrossPay,
    decimal NetPay,
    string Status,
    Guid BatchId,
    string BatchStatus);
