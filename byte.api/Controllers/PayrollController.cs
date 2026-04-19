using Byte.Domain.Services;
using Byte.Domain.Services.Models;
using Byte.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Byte.Api.Controllers;

[ApiController]
[Route("api/payroll")]
[AllowAnonymous] // TODO: Replace with Stytch auth
public class PayrollController(
    ApprovalService approvalService,
    IPayrollCalculationRepository calcRepo,
    IPayrollRecordRepository recordRepo,
    IPayrollBatchRepository batchRepo) : ControllerBase
{
    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary([FromQuery] Guid? batchId, CancellationToken ct)
    {
        var calculations = batchId.HasValue
            ? (await calcRepo.GetByBatchIdAsync(batchId.Value, ct)).ToList()
            : (await calcRepo.GetAllAsync(ct)).ToList();

        var records = (await recordRepo.GetAllAsync(ct)).ToDictionary(r => r.Id);
        var batches = (await batchRepo.GetAllAsync(ct)).ToDictionary(b => b.Id);

        var summary = calculations
            .Where(c => records.ContainsKey(c.PayrollRecordId) && batches.ContainsKey(c.BatchId))
            .Select(c =>
            {
                var r = records[c.PayrollRecordId];
                var b = batches[c.BatchId];
                return new SummaryDto(r.WorkerId, r.WorkerName, r.Site, r.DaysPresent,
                    c.BasePay, c.SiteAllowance, c.GrossPay, c.NetPay, c.Status,
                    c.BatchId, b.BatchStatus);
            });

        return Ok(summary);
    }

    [HttpPost("batches/{batchId:guid}/approve")]
    public async Task<IActionResult> ApproveBatch(Guid batchId, CancellationToken ct)
    {
        var actor = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";
        try
        {
            var batch = await approvalService.ApproveBatchAsync(batchId, actor, ct);
            return Ok(batch);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
    }

    [HttpGet("batches")]
    public async Task<IActionResult> GetBatches(CancellationToken ct)
    {
        var batches = await batchRepo.GetAllAsync(ct);
        return Ok(batches);
    }
}
