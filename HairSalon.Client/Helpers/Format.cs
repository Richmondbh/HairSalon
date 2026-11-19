using HairSalon.Shared.Enums;

namespace HairSalon.Client.Helpers;

/// <summary>
/// Small helpers for showing values the same way on every page.
/// Usage in Razor:  @Format.Price(service.Price)
/// </summary>
public static class Format
{
    public static string Price(decimal price) => $"{price:N0} kr";

    public static string Duration(int minutes) =>
        minutes < 60 ? $"{minutes} min"
        : minutes % 60 == 0 ? $"{minutes / 60} h"
        : $"{minutes / 60} h {minutes % 60} min";

    public static string Date(DateOnly date) => date.ToString("ddd d MMM yyyy");

    public static string ShortDate(DateOnly date) => date.ToString("d MMM");

    public static string Time(TimeOnly time) => time.ToString("HH:mm");

    /// <summary>A Bootstrap Icons class for each category.</summary>
    public static string CategoryIcon(ServiceCategory category) => category switch
    {
        ServiceCategory.Haircut => "bi-scissors",
        ServiceCategory.Coloring => "bi-palette",
        ServiceCategory.Styling => "bi-stars",
        ServiceCategory.Treatment => "bi-droplet-half",
        ServiceCategory.Grooming => "bi-person-check",
        _ => "bi-tag"
    };

    /// <summary>CSS class for the coloured status badge (see app.css).</summary>
    public static string StatusClass(AppointmentStatus status) => status switch
    {
        AppointmentStatus.Pending => "status-pending",
        AppointmentStatus.Confirmed => "status-confirmed",
        AppointmentStatus.Completed => "status-completed",
        AppointmentStatus.Cancelled => "status-cancelled",
        _ => string.Empty
    };

    public static string StatusIcon(AppointmentStatus status) => status switch
    {
        AppointmentStatus.Pending => "bi-hourglass-split",
        AppointmentStatus.Confirmed => "bi-check-circle",
        AppointmentStatus.Completed => "bi-check2-all",
        AppointmentStatus.Cancelled => "bi-x-circle",
        _ => "bi-circle"
    };

    public static string NotificationIcon(NotificationType type) => type switch
    {
        NotificationType.Booking => "bi-calendar-plus",
        NotificationType.StatusChange => "bi-arrow-repeat",
        NotificationType.Reminder => "bi-alarm",
        NotificationType.Review => "bi-star",
        _ => "bi-info-circle"
    };

    /// <summary>"just now", "5 min ago", "3 h ago", "2 days ago" or a date.</summary>
    public static string TimeAgo(DateTime time)
    {
        var span = DateTime.Now - time;
        if (span.TotalMinutes < 1) return "just now";
        if (span.TotalMinutes < 60) return $"{(int)span.TotalMinutes} min ago";
        if (span.TotalHours < 24) return $"{(int)span.TotalHours} h ago";
        if (span.TotalDays < 7) return $"{(int)span.TotalDays} day{((int)span.TotalDays == 1 ? "" : "s")} ago";
        return time.ToString("d MMM yyyy");
    }
}
