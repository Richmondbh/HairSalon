using System.ComponentModel.DataAnnotations;

namespace HairSalon.Api.Models;

/// <summary>ENTITY: an employee who performs services.</summary>
public class Stylist
{
    public int StylistId { get; set; }

    [Required, MaxLength(50)]
    public string FirstName { get; set; } = string.Empty;

    [Required, MaxLength(50)]
    public string LastName { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string Email { get; set; } = string.Empty;

    [MaxLength(20)]
    public string PhoneNumber { get; set; } = string.Empty;

    /// e.g. "Hair Cutting", "Coloring", "Bridal Styling".
    [MaxLength(100)]
    public string Specialization { get; set; } = string.Empty;

    public int YearsOfExperience { get; set; }

    /// Unavailable stylists cannot be booked (holiday, sick leave, left the salon...).
    public bool IsAvailable { get; set; } = true;

    [MaxLength(1000)]
    public string Bio { get; set; } = string.Empty;

    public string FullName => $"{FirstName} {LastName}";

    // One stylist -> many appointments.
    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
}
