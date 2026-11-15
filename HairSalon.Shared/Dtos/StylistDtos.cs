using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace HairSalon.Shared.Dtos;

/// <summary>A stylist as shown on the Stylists page (read model).</summary>
public class StylistDto
{
    public int StylistId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Specialization { get; set; } = string.Empty;
    public int YearsOfExperience { get; set; }
    public bool IsAvailable { get; set; }
    public string Bio { get; set; } = string.Empty;

    /// <summary>Calculated on the fly; [JsonIgnore] keeps it out of the JSON.</summary>
    [JsonIgnore]
    public string FullName => $"{FirstName} {LastName}";

    [JsonIgnore]
    public string Initials => $"{FirstName.FirstOrDefault()}{LastName.FirstOrDefault()}";
}

/// <summary>Used by administrators to create or edit a stylist (write model).</summary>
public class StylistEditDto
{
    [Required(ErrorMessage = "Please enter a first name.")]
    [StringLength(50)]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter a last name.")]
    [StringLength(50)]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter an email.")]
    [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter a phone number.")]
    [RegularExpression(@"^[0-9 +()\-]{6,20}$", ErrorMessage = "Please enter a valid phone number.")]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter a specialization.")]
    [StringLength(100)]
    public string Specialization { get; set; } = string.Empty;

    [Range(0, 60, ErrorMessage = "Experience must be between 0 and 60 years.")]
    public int YearsOfExperience { get; set; }

    public bool IsAvailable { get; set; } = true;

    [StringLength(1000, ErrorMessage = "The bio can be at most 1000 characters.")]
    public string Bio { get; set; } = string.Empty;
}
