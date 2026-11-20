using HairSalon.Api.Data;
using HairSalon.Shared.Dtos;
using HairSalon.Shared.Enums;
using HairSalon.Shared.Rules;
using Microsoft.EntityFrameworkCore;

namespace HairSalon.Api.Services;

/// <summary>
/// LOYALTY PROGRAMME  The points are CALCULATED from the completed appointments
/// instead of being stored, so they can never get out of sync with the booking history.
/// The rules themselves are in Shared/Rules/LoyaltyRules.cs.
/// </summary>
public class LoyaltyService(SalonDbContext db)
{
    public async Task<LoyaltySummaryDto> GetSummaryAsync(string customerId)
    {
        // SQLite cannot SUM decimals in SQL, so load the prices and sum them in memory.
        var prices = await db.Appointments
            .Where(a => a.CustomerId == customerId && a.Status == AppointmentStatus.Completed)
            .Select(a => a.Price)
            .ToListAsync();

        var totalSpent = prices.Sum();
        var points = LoyaltyRules.CalculatePoints(totalSpent);
        var (nextTier, pointsNeeded) = LoyaltyRules.GetNextTier(points);

        return new LoyaltySummaryDto
        {
            Points = points,
            Tier = LoyaltyRules.GetTier(points),
            DiscountPercent = LoyaltyRules.GetDiscountPercent(points),
            NextTier = nextTier,
            PointsToNextTier = pointsNeeded,
            CompletedVisits = prices.Count,
            TotalSpent = totalSpent
        };
    }

    public async Task<int> GetDiscountPercentAsync(string customerId) =>
        (await GetSummaryAsync(customerId)).DiscountPercent;
}
