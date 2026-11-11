using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace HairSalon.Api.Models;

/// <summary>
/// A user account. IdentityUser already has Id, UserName, Email, PhoneNumber,
/// PasswordHash, LockoutEnd ... - I only add the extra profile fields the salon needs.
/// Customers and administrators are both ApplicationUsers; ROLES tell them apart.
/// </summary>
public class ApplicationUser : IdentityUser
{
    [MaxLength(50)]
    public string FirstName { get; set; } = string.Empty;

    [MaxLength(50)]
    public string LastName { get; set; } = string.Empty;

    public DateTime RegistrationDate { get; set; } = DateTime.Now;

    /// <summary>Not stored in the database ([NotMapped]) - calculated from the two names.</summary>
    [NotMapped]
    public string FullName => $"{FirstName} {LastName}".Trim();

    // Navigation property: one customer has many appointments.
    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
}
