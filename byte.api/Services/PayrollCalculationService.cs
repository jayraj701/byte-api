using Byte.Api.Configuration;
using Byte.Domain.Entities;
using Byte.Domain.Interfaces;
using Microsoft.Extensions.Options;

namespace Byte.Api.Services;

public class PayrollCalculationService(
    IPayrollRecordRepository recordRepo,
    IPayrollCalculationRepository calcRepo,
    IAuditLogRepository auditRepo,
    IOptions<PayrollRulesOptions> options)
{
    public async Task<IEnumerable<PayrollCalculation>> CalculateBatchAsync(
        Guid batchId, string actor, CancellationToken ct = default)
    {
        var rules = options.Value;
        var records = (await recordRepo.GetByBatchIdAsync(batchId, ct)).ToList();
        var calculations = new List<PayrollCalculation>();

        foreach (var record in records)
        {
            var basePay = record.DaysPresent * record.DayRate;
            var allowanceRate = rules.GetSiteAllowance(record.Site);
            var siteAllowance = record.DaysPresent * allowanceRate;
            var grossPay = basePay + siteAllowance;
            var netPay = grossPay - record.AdvanceDeduction;
            var status = netPay < rules.DisputeThreshold ? "Disputed" : "Ready";

            var calc = new PayrollCalculation
            {
                PayrollRecordId = record.Id,
                BatchId = batchId,
                BasePay = basePay,
                SiteAllowance = siteAllowance,
                GrossPay = grossPay,
                NetPay = netPay,
                Status = status,
                CreatedBy = actor
            };

            await calcRepo.AddAsync(calc, ct);
            calculations.Add(calc);
        }

        await auditRepo.AddAsync(new AuditLog
        {
            EventType = "CalculationRun",
            BatchId = batchId,
            Actor = actor,
            Detail = $"Calculated {calculations.Count} records",
            OccurredAt = DateTime.UtcNow,
            CreatedBy = actor
        }, ct);

        return calculations;
    }
}
