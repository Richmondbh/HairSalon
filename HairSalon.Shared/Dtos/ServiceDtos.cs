using System.ComponentModel.DataAnnotations;
using HairSalon.Shared.Enums;

namespace HairSalon.Shared.Dtos;

/// <summary>
/// A salon service as the client sees it (read model).
/// Why not send the entity itself? Entities have navigation properties
/// (Service -> Appointments -> Customer ...) that would leak data and can cause
/// endless JSON loops. A DTO contains exactly what the client needs.
/// </summary>
public class ServiceDto
{
    public int ServiceId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int DurationMinutes { get; set; }
    public ServiceCategory Category { get; set; }
    public bool IsActive { get; set; }
}

/// <summary>
/// Used when an administrator creates or edits a service (write model).
/// The data annotations below are checked in the browser (EditForm) and again by the API.
/// </summary>
public class ServiceEditDto
{
    [Required(ErrorMessage = "Please enter a name.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "The name must be 2-100 characters.")]
    public string Name { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "The description can be at most 500 characters.")]
    public string Description { get; set; } = string.Empty;

    [Range(1.0, 10000.0, ErrorMessage = "The price must be between 1 and 10 000 kr.")]
    public decimal Price { get; set; }

    [Range(10, 480, ErrorMessage = "The duration must be between 10 and 480 minutes.")]
    public int DurationMinutes { get; set; } = 30;

    [EnumDataType(typeof(ServiceCategory))]
    public ServiceCategory Category { get; set; }

    public bool IsActive { get; set; } = true;
}
