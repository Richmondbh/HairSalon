namespace HairSalon.Api.Models;

/// <summary>
/// JOIN ENTITY : favourite stylist).
/// Creates a MANY-TO-MANY relationship: a customer can have many favourite stylists and a
/// stylist can be the favourite of many customers. The primary key is the combination
/// (UserId, StylistId), configured in SalonDbContext, so the same pair cannot be stored twice.
/// </summary>
public class FavoriteStylist
{
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;

    public int StylistId { get; set; }
    public Stylist Stylist { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
