using System.ComponentModel.DataAnnotations;
using HairSalon.Shared.Enums;

namespace HairSalon.Api.Models;

/// <summary>
/// ENTITY: a treatment the salon offers (e.g. "Women's Haircut").
/// Entities are the C# classes that EF Core maps to database tables.
/// </summary>
public class Service
{
    // EF Core sees "<ClassName>Id" and makes it the primary key automatically.
    public int ServiceId { get; set; }

    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;

    public decimal Price { get; set; }

    public int DurationMinutes { get; set; }

    public ServiceCategory Category { get; set; }

    /// <summary>Inactive services are hidden from customers but kept for history.</summary>
    public bool IsActive { get; set; } = true;

    // One service -> many appointments.
    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
}
