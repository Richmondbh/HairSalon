using HairSalon.Api.Data;
using HairSalon.Api.Infrastructure;
using HairSalon.Api.Mapping;
using HairSalon.Api.Models;
using HairSalon.Shared.Dtos;
using HairSalon.Shared.Enums;
using HairSalon.Shared.Rules;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace HairSalon.Api.Services;

/// <summary>
/// NOTIFICATIONS 
/// Other services call NotifyUserAsync / NotifyAdminsAsync when something happens
/// (booking created, confirmed, cancelled, moved, reminder ...). The Blazor client polls
/// for the unread count and shows it on the bell icon.
///
/// In a real product this is also where an email or SMS would be sent; here we write a log
/// line to show where that would plug in.
/// </summary>
public class NotificationService(
    SalonDbContext db,
    UserManager<ApplicationUser> userManager,
    ILogger<NotificationService> logger)
{
    /// <summary>
    /// Creates a notification. save = false lets a caller add several and save them together
    /// (used by the reminder background service).
    /// </summary>
    public async Task NotifyUserAsync(string userId, string title, string message,
        NotificationType type, int? appointmentId = null, bool save = true)
    {
        db.Notifications.Add(new Notification
        {
            UserId = userId,
            Title = title,
            Message = message.Length > 500 ? message[..500] : message,
            Type = type,
            AppointmentId = appointmentId,
            CreatedAt = DateTime.Now
        });

        logger.LogInformation("Notification to user {UserId}: {Title} (an email could be sent here)", userId, title);

        if (save)
            await db.SaveChangesAsync();
    }

    /// <summary>Sends the same notification to every administrator.</summary>
    public async Task NotifyAdminsAsync(string title, string message, NotificationType type, int? appointmentId = null)
    {
        var admins = await userManager.GetUsersInRoleAsync(Roles.Admin);
        foreach (var admin in admins)
            await NotifyUserAsync(admin.Id, title, message, type, appointmentId, save: false);

        await db.SaveChangesAsync();
    }

    public async Task<List<NotificationDto>> GetForUserAsync(string userId, bool unreadOnly, int take = 50)
    {
        var query = db.Notifications.AsNoTracking().Where(n => n.UserId == userId);
        if (unreadOnly)
            query = query.Where(n => !n.IsRead);

        var notifications = await query
            .OrderByDescending(n => n.CreatedAt)
            .Take(take)
            .ToListAsync();

        return notifications.Select(n => n.ToDto()).ToList();
    }

    public Task<int> GetUnreadCountAsync(string userId) =>
        db.Notifications.CountAsync(n => n.UserId == userId && !n.IsRead);

    public async Task MarkAsReadAsync(int id, string userId)
    {
        var notification = await GetOwnAsync(id, userId);
        notification.IsRead = true;
        await db.SaveChangesAsync();
    }

    public async Task MarkAllAsReadAsync(string userId)
    {
        // ExecuteUpdateAsync = one UPDATE statement in SQL, no need to load the rows first.
        await db.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ExecuteUpdateAsync(setters => setters.SetProperty(n => n.IsRead, true));
    }

    public async Task DeleteAsync(int id, string userId)
    {
        var notification = await GetOwnAsync(id, userId);
        db.Notifications.Remove(notification);
        await db.SaveChangesAsync();
    }

    private async Task<Notification> GetOwnAsync(int id, string userId) =>
        await db.Notifications.FirstOrDefaultAsync(n => n.NotificationId == id && n.UserId == userId)
        ?? throw new NotFoundException("The notification was not found.");
}
