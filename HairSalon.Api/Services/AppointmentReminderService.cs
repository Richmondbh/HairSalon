using System.Globalization;
using HairSalon.Api.Data;
using HairSalon.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace HairSalon.Api.Services;

/// <summary>
/// APPOINTMENT REMINDERS (Grade A) - a BACKGROUND SERVICE.
///
/// A BackgroundService starts together with the API and keeps running in the background.
/// Every few minutes (appsettings.json: Reminders:CheckIntervalMinutes) it looks for
/// CONFIRMED appointments that start within the next 24 hours (Reminders:HoursBefore)
/// and have not had a reminder yet, and creates a reminder notification for each.
///
/// Why IServiceScopeFactory? A background service is a SINGLETON (one instance for the whole
/// life of the app), but the DbContext is SCOPED. So for every run we create a new scope and
/// get a fresh DbContext from it.
/// </summary>
public class AppointmentReminderService(
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    ILogger<AppointmentReminderService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var minutes = Math.Max(1, configuration.GetValue("Reminders:CheckIntervalMinutes", 5));
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(minutes));

        logger.LogInformation("Reminder service started - checking every {Minutes} minute(s).", minutes);

        // Run once straight away, then every time the timer ticks, until the API stops.
        do
        {
            try
            {
                await SendDueRemindersAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // Never let one failed run stop the service.
                logger.LogError(ex, "Sending reminders failed.");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task SendDueRemindersAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SalonDbContext>();
        var notifications = scope.ServiceProvider.GetRequiredService<NotificationService>();

        var hoursBefore = configuration.GetValue("Reminders:HoursBefore", 24);
        var now = DateTime.Now;
        var limit = now.AddHours(hoursBefore);
        var firstDate = DateOnly.FromDateTime(now);
        var lastDate = DateOnly.FromDateTime(limit);

        // Step 1 (SQL): confirmed, no reminder yet, on one of the relevant dates.
        var candidates = await db.Appointments
            .Include(a => a.Service)
            .Include(a => a.Stylist)
            .Where(a => a.Status == AppointmentStatus.Confirmed && !a.ReminderSent &&
                        a.AppointmentDate >= firstDate && a.AppointmentDate <= lastDate)
            .ToListAsync(cancellationToken);

        // Step 2 (memory): combine date + time and keep the ones starting within the window.
        var due = candidates
            .Where(a => a.AppointmentDate.ToDateTime(a.StartTime) > now &&
                        a.AppointmentDate.ToDateTime(a.StartTime) <= limit)
            .ToList();

        foreach (var appointment in due)
        {
            var when = appointment.AppointmentDate == DateOnly.FromDateTime(now) ? "today" : "tomorrow";
            var date = appointment.AppointmentDate.ToString("ddd d MMM", CultureInfo.InvariantCulture);
            var time = appointment.StartTime.ToString("HH:mm", CultureInfo.InvariantCulture);
            var message = $"Reminder: your {appointment.Service.Name} with {appointment.Stylist.FullName} is {when} " +
                          $"({date}) at {time}. Need to change it? Go to 'My appointments'.";

            await notifications.NotifyUserAsync(appointment.CustomerId, "Appointment reminder", message,
                NotificationType.Reminder, appointment.AppointmentId, save: false);

            appointment.ReminderSent = true;
        }

        if (due.Count > 0)
        {
            await db.SaveChangesAsync(cancellationToken);   // saves notifications + ReminderSent together
            logger.LogInformation("Sent {Count} appointment reminder(s).", due.Count);
        }
    }
}
