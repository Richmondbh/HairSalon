namespace HairSalon.Shared.Dtos;

/// <summary>One bar in a chart: a label and a value (count or kronor).</summary>
public class ChartPointDto
{
    public string Label { get; set; } = string.Empty;
    public decimal Value { get; set; }
}

/// <summary>A service and how many times it has been booked.</summary>
public class PopularServiceDto
{
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public int BookingCount { get; set; }
}

/// <summary>Everything the admin dashboard shows, calculated with LINQ in DashboardService.</summary>
public class DashboardDto
{
    public int TotalAppointments { get; set; }
    public int AppointmentsThisWeek { get; set; }
    public int UpcomingAppointments { get; set; }
    public int PendingAppointments { get; set; }

    public int TotalCustomers { get; set; }
    public int ActiveCustomers { get; set; }

    public int AvailableStylists { get; set; }
    public int TotalStylists { get; set; }

    public decimal EstimatedRevenue { get; set; }
    public decimal RevenueThisMonth { get; set; }

    public double AverageRating { get; set; }
    public int TotalReviews { get; set; }

    public List<PopularServiceDto> PopularServices { get; set; } = new();
    public List<ChartPointDto> BookingsNext7Days { get; set; } = new();
    public List<ChartPointDto> StatusBreakdown { get; set; } = new();
    public List<AppointmentDto> TodaysAppointments { get; set; } = new();
}
