using System.ComponentModel.DataAnnotations;

namespace HairSalon.Api.Models;

/// <summary>
/// ENTITY (Grade A): a customer's rating of a completed appointment.
/// It only stores the AppointmentId - the customer, stylist and service are reached
/// THROUGH the appointment (Review.Appointment.Stylist). That avoids storing the same
/// information twice (normalisation).
/// </summary>
public class Review
{
    public int ReviewId { get; set; }

    public int AppointmentId { get; set; }
    public Appointment Appointment { get; set; } = null!;

    /// <summary>1-5 stars.</summary>
    [Range(1, 5)]
    public int Rating { get; set; }

    [MaxLength(500)]
    public string? Comment { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
