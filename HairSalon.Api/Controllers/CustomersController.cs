using HairSalon.Api.Services;
using HairSalon.Shared.Dtos;
using HairSalon.Shared.Rules;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HairSalon.Api.Controllers;

/// <summary>
/// Customer management for administrators.
///   GET api/customers?search=     list with statistics
///   GET api/customers/{id}        one customer + full history
///   PUT api/customers/{id}/lock   lock the account
///   PUT api/customers/{id}/unlock unlock the account
/// </summary>
[ApiController]
[Route("api/customers")]
[Authorize(Roles = Roles.Admin)]
public class CustomersController(CustomerService customerService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<CustomerDto>>> GetAll([FromQuery] string? search) =>
        Ok(await customerService.GetCustomersAsync(search));

    [HttpGet("{id}")]
    public async Task<ActionResult<CustomerDetailsDto>> GetById(string id) =>
        Ok(await customerService.GetCustomerAsync(id));

    [HttpPut("{id}/lock")]
    public async Task<ActionResult<ApiMessage>> Lock(string id) =>
        Ok(new ApiMessage(await customerService.SetLockedAsync(id, locked: true)));

    [HttpPut("{id}/unlock")]
    public async Task<ActionResult<ApiMessage>> Unlock(string id) =>
        Ok(new ApiMessage(await customerService.SetLockedAsync(id, locked: false)));
}
