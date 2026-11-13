using HairSalon.Shared.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace HairSalon.Api.Controllers;

/// <summary>
/// The simplest possible controller: GET api/health answers "Online".
/// The Blazor client calls it to check that the two applications can talk to each other.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class HealthController(IConfiguration configuration) : ControllerBase
{
    // "HealthController(IConfiguration configuration)" is a C# 12 PRIMARY CONSTRUCTOR.
    // ASP.NET Core's dependency injection passes in IConfiguration automatically, and the
    // parameter can be used anywhere in the class - no private field needed.

    [HttpGet]
    public ActionResult<HealthDto> Get() => Ok(new HealthDto
    {
        Status = "Online",
        SalonName = configuration["SalonName"] ?? "Hair Salon",
        ServerTime = DateTime.Now
    });
}
