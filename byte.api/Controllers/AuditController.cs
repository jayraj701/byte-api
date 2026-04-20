using Byte.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Byte.Api.Controllers;

[ApiController]
[Route("api/audit")]
[AllowAnonymous] // TODO: Replace with Stytch auth
public class AuditController(IAuditLogRepository auditRepo) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetLogs([FromQuery] Guid? batchId, CancellationToken ct)
    {
        if (batchId.HasValue)
            return Ok(await auditRepo.GetByBatchIdAsync(batchId.Value, ct));

        var logs = await auditRepo.GetAllAsync(ct);
        return Ok(logs.OrderByDescending(a => a.OccurredAt));
    }
}
