using Byte.Domain.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Byte.Api.Controllers;

[ApiController]
[Route("api/dashboard")]
[AllowAnonymous] // TODO: Replace with Stytch auth
public class DashboardController(DashboardService dashboardService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetSummary(CancellationToken ct)
    {
        var summary = await dashboardService.GetSummaryAsync(ct);
        return Ok(summary);
    }
}
