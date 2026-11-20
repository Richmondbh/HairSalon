using System.ComponentModel.DataAnnotations;
using HairSalon.Shared.Enums;

namespace HairSalon.Api.Models;

/// <summary>
/// ENTITY : an in-app message to one user, e.g. "Your booking was confirmed".
/// Created automatically by the service layer when something happens to an appointment.
/// </summary>
public class Notification
{
    public int NotificationId { get; set; }

    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;

    [Required, MaxLength(100)]
    public string Title { get; set; } = string.Empty;

    [Required, MaxLength(500)]
    public string Message { get; set; } = string.Empty;

    public NotificationType Type { get; set; } = NotificationType.General;

    public bool IsRead { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    /// <summary>Optional link to the appointment the notification is about.</summary>
    public int? AppointmentId { get; set; }
    public Appointment? Appointment { get; set; }
}
