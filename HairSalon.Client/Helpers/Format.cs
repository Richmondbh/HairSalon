using HairSalon.Shared.Enums;

namespace HairSalon.Client.Helpers;

/// <summary>
/// Small helpers for showing values the same way on every page.
/// Usage in Razor:  @Format.Price(service.Price)
/// </summary>
public static class Format
{
    public static string Price(decimal price) => $"{price:N0} kr";

    public static string Duration(int minutes) =>
        minutes < 60 ? $"{minutes} min"
        : minutes % 60 == 0 ? $"{minutes / 60} h"
        : $"{minutes / 60} h {minutes % 60} min";

    public static string Date(DateOnly date) => date.ToString("ddd d MMM yyyy");

    public static string ShortDate(DateOnly date) => date.ToString("d MMM");

    public static string Time(TimeOnly time) => time.ToString("HH:mm");

    /// <summary>A Bootstrap Icons class for each category.</summary>
    public static string CategoryIcon(ServiceCategory category) => category switch
    {
        ServiceCategory.Haircut => "bi-scissors",
        ServiceCategory.Coloring => "bi-palette",
        ServiceCategory.Styling => "bi-stars",
        ServiceCategory.Treatment => "bi-droplet-half",
        ServiceCategory.Grooming => "bi-person-check",
        _ => "bi-tag"
    };
}
