using HairSalon.Api.Services;
using HairSalon.Shared.Dtos;
using HairSalon.Shared.Enums;
using HairSalon.Shared.Rules;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HairSalon.Api.Controllers;

/// <summary>
/// REST endpoints for salon services.
///   GET    api/services              everyone   (search + category filter)
///   GET    api/services/5            everyone
///   POST   api/services              Admin
///   PUT    api/services/5            Admin
///   DELETE api/services/5            Admin
/// </summary>
[ApiController]
[Route("api/services")]
public class ServicesController(ServiceCatalogService serviceCatalog) : ControllerBase
{
    /// <summary>GET api/services?search=cut&amp;category=Haircut&amp;includeInactive=true</summary>
    [HttpGet]
    public async Task<ActionResult<List<ServiceDto>>> GetAll(
        [FromQuery] string? search, [FromQuery] ServiceCategory? category, [FromQuery] bool includeInactive = false)
    {
        // Only administrators may see inactive services.
        var showInactive = includeInactive && User.IsInRole(Roles.Admin);
        return Ok(await serviceCatalog.GetAllAsync(search, category, showInactive));
    }

    // ":int" is a ROUTE CONSTRAINT - "api/services/popular" will not match this action.
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ServiceDto>> GetById(int id) =>
        Ok(await serviceCatalog.GetByIdAsync(id));

    [Authorize(Roles = Roles.Admin)]
    [HttpPost]
    public async Task<ActionResult<ServiceDto>> Create(ServiceEditDto dto)
    {
        var created = await serviceCatalog.CreateAsync(dto);
        // 201 Created + a Location header pointing to GET api/services/{id}
        return CreatedAtAction(nameof(GetById), new { id = created.ServiceId }, created);
    }

    [Authorize(Roles = Roles.Admin)]
    [HttpPut("{id:int}")]
    public async Task<ActionResult<ServiceDto>> Update(int id, ServiceEditDto dto) =>
        Ok(await serviceCatalog.UpdateAsync(id, dto));

    [Authorize(Roles = Roles.Admin)]
    [HttpDelete("{id:int}")]
    public async Task<ActionResult<ApiMessage>> Delete(int id) =>
        Ok(new ApiMessage(await serviceCatalog.DeleteAsync(id)));
}
