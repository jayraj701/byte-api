using Byte.Api.Services;
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
            return BadRequest("No file uploaded.");

        var actor = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";
        var batch = await attendanceService.IngestAsync(file, actor, ct);

        return CreatedAtAction(nameof(GetRecords), new { batchId = batch.Id }, new
        {
            batchId = batch.Id,
            fileName = batch.FileName,
            recordCount = (await recordRepo.GetByBatchIdAsync(batch.Id, ct)).Count()
        });
    }

    [HttpPost("{batchId:guid}/calculate")]
    public async Task<IActionResult> Calculate(Guid batchId, CancellationToken ct)
    {
        var actor = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";
        var calculations = await calculationService.CalculateBatchAsync(batchId, actor, ct);
        return Ok(calculations);
    }

    [HttpGet("{batchId:guid}/records")]
    public async Task<IActionResult> GetRecords(Guid batchId, CancellationToken ct)
    {
        var records = await recordRepo.GetByBatchIdAsync(batchId, ct);
        return Ok(records);
    }
}
