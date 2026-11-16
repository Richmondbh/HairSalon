namespace HairSalon.Shared.Rules;

/// <summary>
/// The salon's opening hours. Used by the API to calculate free time slots and by the
/// client to show the opening hours. Change the values here and both sides follow.
/// </summary>
public static class SalonHours
{
    public static readonly TimeOnly OpeningTime = new(9, 0);
    public static readonly TimeOnly ClosingTime = new(18, 0);

    /// <summary>Appointments can start every 30 minutes (09:00, 09:30, 10:00 ...).</summary>
    public const int SlotIntervalMinutes = 30;

    /// <summary>How far ahead customers may book.</summary>
    public const int MaxDaysAhead = 60;

    /// <summary>The salon is closed on Sundays.</summary>
    public static bool IsOpen(DateOnly date) => date.DayOfWeek != DayOfWeek.Sunday;
}
