using HairSalon.Api.Infrastructure;
using HairSalon.Api.Services;
using HairSalon.Shared.Dtos;
using HairSalon.Shared.Enums;
using HairSalon.Shared.Rules;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HairSalon.Api.Controllers;

/// <summary>
/// Booking endpoints. [Authorize] on the class = you must be logged in for every action
/// unless the action says [AllowAnonymous].
///
///   GET  api/appointments/available-slots   everyone
///   POST api/appointments                   logged in    (book)
///   GET  api/appointments/mine              logged in    (my bookings)
///   GET  api/appointments/5                 owner/Admin
///   PUT  api/appointments/5/cancel          owner/Admin
///   PUT  api/appointments/5/reschedule      owner/Admin
///   GET  api/appointments                   Admin        (all, with filters)
///   PUT  api/appointments/5/status          Admin        (confirm / complete / cancel)
/// </summary>
[ApiController]
[Route("api/appointments")]
[Authorize]
public class AppointmentsController(AppointmentService appointmentService) : ControllerBase
{
    /// <summary>GET api/appointments/available-slots?stylistId=1&amp;serviceId=2&amp;date=2026-11-20</summary>
    [AllowAnonymous]
    [HttpGet("available-slots")]
    public async Task<ActionResult<List<TimeSlotDto>>> GetAvailableSlots(
        [FromQuery] int stylistId, [FromQuery] int serviceId, [FromQuery] DateOnly date, [FromQuery] int? excludeAppointmentId = null) =>
        Ok(await appointmentService.GetAvailableSlotsAsync(stylistId, serviceId, date, excludeAppointmentId));

    [HttpPost]
    public async Task<ActionResult<AppointmentDto>> Create(CreateAppointmentDto dto)
    {
        // The customer id comes from the token - a user can never book in someone else's name.
        var created = await appointmentService.CreateAsync(User.GetUserId(), dto);
        return CreatedAtAction(nameof(GetById), new { id = created.AppointmentId }, created);
    }

    /// <summary>GET api/appointments/mine?filter=upcoming|history</summary>
    [HttpGet("mine")]
    public async Task<ActionResult<List<AppointmentDto>>> GetMine([FromQuery] string? filter) =>
        Ok(await appointmentService.GetForCustomerAsync(User.GetUserId(), filter));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<AppointmentDto>> GetById(int id) =>
        Ok(await appointmentService.GetByIdAsync(id, User.GetUserId(), User.IsAdmin()));

    [HttpPut("{id:int}/cancel")]
    public async Task<ActionResult<AppointmentDto>> Cancel(int id) =>
        Ok(await appointmentService.CancelAsync(id, User.GetUserId(), User.IsAdmin()));

    [HttpPut("{id:int}/reschedule")]
    public async Task<ActionResult<AppointmentDto>> Reschedule(int id, RescheduleDto dto) =>
        Ok(await appointmentService.RescheduleAsync(id, User.GetUserId(), User.IsAdmin(), dto));

    /// <summary>GET api/appointments?status=Pending&amp;stylistId=1&amp;from=2026-11-01&amp;to=2026-11-30&amp;search=anna</summary>
    [Authorize(Roles = Roles.Admin)]
    [HttpGet]
    public async Task<ActionResult<List<AppointmentDto>>> GetAll(
        [FromQuery] AppointmentStatus? status, [FromQuery] int? stylistId,
        [FromQuery] DateOnly? from, [FromQuery] DateOnly? to, [FromQuery] string? search) =>
        Ok(await appointmentService.GetAllAsync(status, stylistId, from, to, search));

    [Authorize(Roles = Roles.Admin)]
    [HttpPut("{id:int}/status")]
    public async Task<ActionResult<AppointmentDto>> UpdateStatus(int id, UpdateStatusDto dto) =>
        Ok(await appointmentService.UpdateStatusAsync(id, dto));
}
