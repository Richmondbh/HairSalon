using HairSalon.Api.Infrastructure;
using HairSalon.Api.Services;
using HairSalon.Shared.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HairSalon.Api.Controllers;

/// <summary>
/// The logged-in user's notifications. Every action uses the user id from the token,
/// so nobody can read or change another user's notifications.
/// </summary>
[ApiController]
[Route("api/notifications")]
[Authorize]
public class NotificationsController(NotificationService notificationService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<NotificationDto>>> GetMine([FromQuery] bool unreadOnly = false) =>
        Ok(await notificationService.GetForUserAsync(User.GetUserId(), unreadOnly));

    [HttpGet("unread-count")]
    public async Task<ActionResult<int>> GetUnreadCount() =>
        Ok(await notificationService.GetUnreadCountAsync(User.GetUserId()));

    [HttpPut("{id:int}/read")]
    public async Task<IActionResult> MarkAsRead(int id)
    {
        await notificationService.MarkAsReadAsync(id, User.GetUserId());
        return NoContent();   // 204: success, nothing to return
    }

    [HttpPut("read-all")]
    public async Task<IActionResult> MarkAllAsRead()
    {
        await notificationService.MarkAllAsReadAsync(User.GetUserId());
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await notificationService.DeleteAsync(id, User.GetUserId());
        return NoContent();
    }
}
