using System.ComponentModel.DataAnnotations;
using HairSalon.Shared.Enums;

namespace HairSalon.Api.Models;

/// <summary>
/// ENTITY: a booking. This is the centre of the data model:
/// it belongs to ONE customer, includes ONE service and is assigned to ONE stylist.
/// </summary>
public class Appointment
{
    public int AppointmentId { get; set; }

    // DateOnly/TimeOnly are perfect here: a date without a time, and a time without a date.
    public DateOnly AppointmentDate { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }

    public AppointmentStatus Status { get; set; } = AppointmentStatus.Pending;

    [MaxLength(500)]
    public string? Notes { get; set; }

    /// <summary>
    /// The price when the booking was made. Stored here (a "snapshot") so that changing a
    /// service's price later does not change old bookings or the revenue history.
    /// </summary>
    public decimal Price { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    // ---- Foreign keys + navigation properties ----
    // The FK holds the id, the navigation property gives access to the whole object
    // when it is loaded with .Include(...). "= null!" tells the compiler EF Core fills it.

    public string CustomerId { get; set; } = string.Empty;
    public ApplicationUser Customer { get; set; } = null!;

    public int ServiceId { get; set; }
    public Service Service { get; set; } = null!;

    public int StylistId { get; set; }
    public Stylist Stylist { get; set; } = null!;
}
