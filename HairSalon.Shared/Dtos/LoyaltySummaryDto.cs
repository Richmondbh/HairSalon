namespace HairSalon.Shared.Dtos;

/// <summary>A customer's loyalty status (Grade A: customer loyalty system).</summary>
public class LoyaltySummaryDto
{
    public int Points { get; set; }
    public string Tier { get; set; } = "Bronze";
    public int DiscountPercent { get; set; }
    public string? NextTier { get; set; }
    public int PointsToNextTier { get; set; }
    public int CompletedVisits { get; set; }
    public decimal TotalSpent { get; set; }
}
