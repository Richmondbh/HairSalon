using HairSalon.Shared.Enums;

namespace HairSalon.Shared.Dtos;

/// <summary>An in-app notification (Grade A: notifications and reminders).</summary>
public class NotificationDto
{
    public int NotificationId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public NotificationType Type { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
    public int? AppointmentId { get; set; }
}
