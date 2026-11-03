using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PtwSystem.Api.Dtos;
using PtwSystem.Api.Services;

namespace PtwSystem.Api.Controllers;

[ApiController]
[Route("api/dashboard")]
[Authorize]
public class DashboardController(IDashboardService dashboard) : ControllerBase
{
    /// <summary>KPI counts and currently active permits for the dashboard.</summary>
    [HttpGet("summary")]
    public async Task<ActionResult<DashboardSummaryDto>> Summary() => Ok(await dashboard.GetSummaryAsync());
}
