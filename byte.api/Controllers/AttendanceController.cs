using Byte.Domain.Services;
using Byte.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Byte.Api.Controllers;

[ApiController]
[Route("api/attendance")]
[AllowAnonymous] // TODO: Replace with Stytch auth
public class AttendanceController(
    AttendanceService attendanceService,
    PayrollCalculationService calculationService,
    IPayrollRecordRepository recordRepo) : ControllerBase
{
    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Upload(IFormFile file, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { error = "No file uploaded." });

        var actor = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";

        try
        {
            var batch = await attendanceService.IngestAsync(file, actor, ct);
            var records = await recordRepo.GetByBatchIdAsync(batch.Id, ct);
            return CreatedAtAction(nameof(GetRecords), new { batchId = batch.Id }, new
            {
                batchId     = batch.Id,
                fileName    = batch.FileName,
                recordCount = records.Count()
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("{batchId:guid}/calculate")]
    public async Task<IActionResult> Calculate(Guid batchId, CancellationToken ct)
    {
        var actor = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";

        try
        {
            var calculations = await calculationService.CalculateBatchAsync(batchId, actor, ct);
            return Ok(calculations);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { error = $"Batch '{batchId}' not found." });
        }
    }

    [HttpGet("{batchId:guid}/records")]
    public async Task<IActionResult> GetRecords(Guid batchId, CancellationToken ct)
    {
        try
        {
            var records = await recordRepo.GetByBatchIdAsync(batchId, ct);
            return Ok(records);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}
