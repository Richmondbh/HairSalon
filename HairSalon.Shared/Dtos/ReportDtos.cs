namespace HairSalon.Shared.Dtos;

/// <summary>Statistics for one month of the year.</summary>
public class MonthlyStatDto
{
    public int Month { get; set; }
    public string MonthName { get; set; } = string.Empty;
    public int Bookings { get; set; }
    public int Completed { get; set; }
    public int Cancelled { get; set; }
    public decimal Revenue { get; set; }
}

/// <summary>One of the best customers of the year.</summary>
public class TopCustomerDto
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int Bookings { get; set; }
    public decimal TotalSpent { get; set; }
}

/// <summary>How a stylist performed during the year.</summary>
public class StylistPerformanceDto
{
    public string Name { get; set; } = string.Empty;
    public int CompletedAppointments { get; set; }
    public decimal Revenue { get; set; }
    public double CancellationRate { get; set; }
    public double AverageRating { get; set; }
}

/// <summary>The full yearly report (Grade A: advanced reporting and revenue analysis).</summary>
public class ReportDto
{
    public int Year { get; set; }
    public List<int> AvailableYears { get; set; } = new();

    public decimal TotalRevenue { get; set; }
    public decimal AverageBookingValue { get; set; }
    public double CancellationRate { get; set; }
    public string BusiestWeekday { get; set; } = "-";
    public string BusiestHour { get; set; } = "-";

    public List<MonthlyStatDto> Months { get; set; } = new();
    public List<ChartPointDto> RevenueByService { get; set; } = new();
    public List<ChartPointDto> RevenueByCategory { get; set; } = new();
    public List<TopCustomerDto> TopCustomers { get; set; } = new();
    public List<StylistPerformanceDto> StylistPerformance { get; set; } = new();
}
