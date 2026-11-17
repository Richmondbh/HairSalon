using HairSalon.Api.Services;
using HairSalon.Shared.Dtos;
using HairSalon.Shared.Rules;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HairSalon.Api.Controllers;

/// <summary>
/// Statistics for administrators only - the attribute on the class protects every action.
///   GET api/dashboard                 the dashboard numbers
///   GET api/dashboard/report?year=    the yearly report
/// </summary>
[ApiController]
[Route("api/dashboard")]
[Authorize(Roles = Roles.Admin)]
public class DashboardController(DashboardService dashboardService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<DashboardDto>> Get() =>
        Ok(await dashboardService.GetDashboardAsync());
}
