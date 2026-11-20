namespace HairSalon.Shared.Dtos;

/// <summary>A row in the admin "Customers" list (Grade A: customer management module).</summary>
public class CustomerDto
{
    public string Id { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public DateTime RegistrationDate { get; set; }

    public int TotalAppointments { get; set; }
    public int CompletedAppointments { get; set; }
    public int CancelledAppointments { get; set; }
    public decimal TotalSpent { get; set; }
    public DateOnly? LastVisit { get; set; }
    public bool IsLocked { get; set; }

    public int LoyaltyPoints { get; set; }
    public string LoyaltyTier { get; set; } = string.Empty;
}

/// <summary>A customer with their full appointment history.</summary>
public class CustomerDetailsDto : CustomerDto
{
    public string? FavoriteService { get; set; }
    public string? FavoriteStylist { get; set; }
    public List<AppointmentDto> Appointments { get; set; } = new();
}
