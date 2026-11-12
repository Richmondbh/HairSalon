using HairSalon.Api.Services;
using HairSalon.Shared.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace HairSalon.Api.Controllers;

/// <summary>
/// The WEB API LAYER for accounts. Controllers are deliberately THIN: they receive the
/// request, call the service layer and return the result. No business logic here.
///
/// [ApiController] gives us automatic model validation: if the DTO breaks a data
/// annotation (e.g. [Required]) the action is never called and a 400 is returned.
/// </summary>
[ApiController]
[Route("api/auth")]
public class AuthController(AuthService authService) : ControllerBase
{
    /// <summary>POST api/auth/register - create a customer account and log in.</summary>
    [HttpPost("register")]
    public async Task<ActionResult<AuthResponseDto>> Register(RegisterDto dto) =>
        Ok(await authService.RegisterAsync(dto));

    /// <summary>POST api/auth/login - returns a JWT if the email and password are correct.</summary>
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponseDto>> Login(LoginDto dto) =>
        Ok(await authService.LoginAsync(dto));
}
