namespace HairSalon.Shared.Enums;

/// <summary>
/// Groups the salon's services so customers can filter them.
/// Stored as text in the database (see SalonDbContext) so the table is easy to read.
/// </summary>
public enum ServiceCategory
{
    Haircut,
    Coloring,
    Styling,
    Treatment,
    Grooming
}
