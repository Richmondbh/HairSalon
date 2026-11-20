using HairSalon.Api.Infrastructure;
using HairSalon.Api.Services;
using HairSalon.Shared.Dtos;
using Microsoft.AspNetCore.Authorization;
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

    /// <summary>GET api/auth/profile - the logged-in user's profile and loyalty status.</summary>
    [Authorize]
    [HttpGet("profile")]
    public async Task<ActionResult<UserProfileDto>> GetProfile() =>
        Ok(await authService.GetProfileAsync(User.GetUserId()));

    /// <summary>PUT api/auth/profile - update name and phone number.</summary>
    [Authorize]
    [HttpPut("profile")]
    public async Task<ActionResult<UserProfileDto>> UpdateProfile(UpdateProfileDto dto) =>
        Ok(await authService.UpdateProfileAsync(User.GetUserId(), dto));
}
