using System.ComponentModel.DataAnnotations;

namespace HairSalon.Shared.Dtos;

/// <summary>A review as shown on a stylist's page and on the home page.</summary>
public class ReviewDto
{
    public int ReviewId { get; set; }
    public int AppointmentId { get; set; }
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public DateTime CreatedAt { get; set; }

    /// <summary>"Anna S." - only first name and initial, to protect privacy.</summary>
    public string CustomerName { get; set; } = string.Empty;
    public int StylistId { get; set; }
    public string StylistName { get; set; } = string.Empty;
    public string ServiceName { get; set; } = string.Empty;
}

/// <summary>Sent when a customer reviews a completed appointment.</summary>
public class CreateReviewDto
{
    [Range(1, int.MaxValue)]
    public int AppointmentId { get; set; }

    [Range(1, 5, ErrorMessage = "Please choose between 1 and 5 stars.")]
    public int Rating { get; set; }

    [StringLength(500, ErrorMessage = "The comment can be at most 500 characters.")]
    public string? Comment { get; set; }
}
