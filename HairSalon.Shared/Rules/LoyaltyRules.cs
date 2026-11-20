namespace HairSalon.Shared.Rules;

/// <summary>
/// The loyalty programme (a Grade A feature).
///
/// - Customers earn 1 point for every 10 kr spent on COMPLETED appointments.
/// - Points decide the tier: Bronze (0+), Silver (300+), Gold (800+).
/// - Higher tiers get a discount on new bookings: Silver 5 %, Gold 10 %.
///
/// The rules live in the Shared project because the API uses them to calculate the
/// price of a booking, and the client uses them to show "X points to Gold".
/// Points are calculated from the appointment history, so they can never get out of sync.
/// </summary>
public static class LoyaltyRules
{
    public const int KronorPerPoint = 10;
    public const int SilverThreshold = 300;
    public const int GoldThreshold = 800;

    public static int CalculatePoints(decimal totalSpent) => (int)(totalSpent / KronorPerPoint);

    public static string GetTier(int points) =>
        points >= GoldThreshold ? "Gold"
        : points >= SilverThreshold ? "Silver"
        : "Bronze";

    public static int GetDiscountPercent(int points) =>
        points >= GoldThreshold ? 10
        : points >= SilverThreshold ? 5
        : 0;

    /// <summary>Returns the next tier and how many points are missing, or (null, 0) for Gold.</summary>
    public static (string? NextTier, int PointsNeeded) GetNextTier(int points) =>
        points >= GoldThreshold ? (null, 0)
        : points >= SilverThreshold ? ("Gold", GoldThreshold - points)
        : ("Silver", SilverThreshold - points);
}
