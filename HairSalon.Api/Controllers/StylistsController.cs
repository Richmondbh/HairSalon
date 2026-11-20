using HairSalon.Api.Services;
using HairSalon.Shared.Dtos;
using HairSalon.Shared.Rules;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HairSalon.Api.Controllers;

/// <summary>
///   GET    api/stylists?search=&amp;onlyAvailable=   everyone
///   GET    api/stylists/5                        everyone
///   POST / PUT / DELETE                          Admin
/// </summary>
[ApiController]
[Route("api/stylists")]
public class StylistsController(StylistService stylistService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<StylistDto>>> GetAll([FromQuery] string? search, [FromQuery] bool onlyAvailable = false) =>
        Ok(await stylistService.GetAllAsync(search, onlyAvailable));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<StylistDto>> GetById(int id) =>
        Ok(await stylistService.GetByIdAsync(id));

    [Authorize(Roles = Roles.Admin)]
    [HttpPost]
    public async Task<ActionResult<StylistDto>> Create(StylistEditDto dto)
    {
        var created = await stylistService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = created.StylistId }, created);
    }

    [Authorize(Roles = Roles.Admin)]
    [HttpPut("{id:int}")]
    public async Task<ActionResult<StylistDto>> Update(int id, StylistEditDto dto) =>
        Ok(await stylistService.UpdateAsync(id, dto));

    [Authorize(Roles = Roles.Admin)]
    [HttpDelete("{id:int}")]
    public async Task<ActionResult<ApiMessage>> Delete(int id) =>
        Ok(new ApiMessage(await stylistService.DeleteAsync(id)));

    /// <summary>GET api/stylists/top - best rated stylists for the home page.</summary>
    [HttpGet("top")]
    public async Task<ActionResult<List<StylistDto>>> GetTop([FromQuery] int count = 3) =>
        Ok(await stylistService.GetTopRatedAsync(Math.Clamp(count, 1, 10)));
}
