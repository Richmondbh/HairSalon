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
