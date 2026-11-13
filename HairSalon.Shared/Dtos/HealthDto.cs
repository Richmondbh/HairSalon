namespace HairSalon.Shared.Dtos;

/// <summary>
/// Returned by GET api/health. The client shows it in the footer so you can
/// see at a glance that the Blazor app can talk to the Web API.
/// </summary>
public class HealthDto
{
    public string Status { get; set; } = string.Empty;
    public string SalonName { get; set; } = string.Empty;
    public DateTime ServerTime { get; set; }
}
