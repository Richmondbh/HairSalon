using System.ComponentModel.DataAnnotations;

namespace HairSalon.Shared.Validation;

/// <summary>
/// A CUSTOM validation attribute: the date must be today or later.
///
/// Because it lives in the Shared project it runs in two places:
///  - in the browser, through &lt;DataAnnotationsValidator /&gt; in the Blazor EditForm
///  - on the server, because [ApiController] validates every incoming DTO automatically
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class NotInPastAttribute : ValidationAttribute
{
    public NotInPastAttribute() : base("The date cannot be in the past.") { }

    public override bool IsValid(object? value)
    {
        // Let [Required] deal with missing values.
        if (value is null) return true;

        var today = DateOnly.FromDateTime(DateTime.Today);
        return value switch
        {
            DateOnly date => date >= today,
            DateTime dateTime => DateOnly.FromDateTime(dateTime) >= today,
            _ => false
        };
    }
}
