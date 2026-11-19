using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using HairSalon.Shared.Enums;
using HairSalon.Shared.Validation;

namespace HairSalon.Shared.Dtos;

/// <summary>
/// An appointment as the client sees it. Instead of nested objects it contains the
/// names the UI needs (ServiceName, StylistName ...) - a "flattened" DTO.
/// </summary>
public class AppointmentDto
{
    public int AppointmentId { get; set; }
    public DateOnly AppointmentDate { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public AppointmentStatus Status { get; set; }
    public string? Notes { get; set; }
    public decimal Price { get; set; }
    public DateTime CreatedAt { get; set; }

    public int ServiceId { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public int StylistId { get; set; }
    public string StylistName { get; set; } = string.Empty;
    public string CustomerId { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public bool HasReview { get; set; }

    // ---- Helper properties: shared business logic, not sent as JSON ----

    [JsonIgnore]
    public DateTime StartDateTime => AppointmentDate.ToDateTime(StartTime);

    /// <summary>Pending or confirmed and still in the future.</summary>
    [JsonIgnore]
    public bool IsUpcoming =>
        (Status == AppointmentStatus.Pending || Status == AppointmentStatus.Confirmed)
        && StartDateTime > DateTime.Now;

    /// <summary>Customers may cancel or move upcoming appointments.</summary>
    [JsonIgnore]
    public bool CanBeChanged => IsUpcoming;

    /// <summary>Only completed appointments without a review can be reviewed.</summary>
    [JsonIgnore]
    public bool CanBeReviewed => Status == AppointmentStatus.Completed && !HasReview;
}

/// <summary>Sent by the booking form to POST api/appointments.</summary>
public class CreateAppointmentDto
{
    [Range(1, int.MaxValue, ErrorMessage = "Please choose a service.")]
    public int ServiceId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Please choose a stylist.")]
    public int StylistId { get; set; }

    [NotInPast(ErrorMessage = "Please choose today or a later date.")]
    public DateOnly AppointmentDate { get; set; } = DateOnly.FromDateTime(DateTime.Today.AddDays(1));

    [Required(ErrorMessage = "Please choose a time.")]
    public TimeOnly? StartTime { get; set; }

    [StringLength(500, ErrorMessage = "Notes can be at most 500 characters.")]
    public string? Notes { get; set; }
}

/// <summary>A free time slot returned by GET api/appointments/available-slots.</summary>
public class TimeSlotDto
{
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
}

/// <summary>Moves an appointment to a new date and time (same service and stylist).</summary>
public class RescheduleDto
{
    [NotInPast(ErrorMessage = "Please choose today or a later date.")]
    public DateOnly NewDate { get; set; }

    [Required(ErrorMessage = "Please choose a time.")]
    public TimeOnly? NewStartTime { get; set; }
}

/// <summary>Used by administrators to approve, complete or cancel an appointment.</summary>
public class UpdateStatusDto
{
    [Required]
    public AppointmentStatus Status { get; set; }

    /// <summary>Optional message that is included in the customer's notification.</summary>
    [StringLength(300)]
    public string? Message { get; set; }
}
