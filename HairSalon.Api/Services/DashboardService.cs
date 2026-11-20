using System.Globalization;
using HairSalon.Api.Data;
using HairSalon.Api.Mapping;
using HairSalon.Api.Models;
using HairSalon.Shared.Dtos;
using HairSalon.Shared.Enums;
using HairSalon.Shared.Rules;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace HairSalon.Api.Services;

/// <summary>
/// LINQ for the admin dashboard and the yearly report.
///
/// Note about SQLite: it stores decimal values as text and cannot SUM/AVERAGE/ORDER BY them
/// in SQL. We therefore load a small PROJECTION (only the columns we need) with one query and
/// do the calculations with LINQ-to-Objects in memory. For a salon (thousands of rows, not
/// millions) this is fast and keeps the queries readable. With SQL Server we could let the
/// database do the sums.
/// </summary>
public class DashboardService(SalonDbContext db, UserManager<ApplicationUser> userManager)
{
    private static readonly CultureInfo English = CultureInfo.InvariantCulture;

    /// <summary>A light-weight row used for the statistics (an anonymous type would also work).</summary>
    private record StatRow(
        int AppointmentId, DateOnly Date, TimeOnly StartTime, AppointmentStatus Status, decimal Price,
        string CustomerId, string CustomerName, string CustomerEmail,
        int ServiceId, string ServiceName, ServiceCategory Category,
        int StylistId, string StylistName);

    /// <summary>
    /// Builds the projection query. The date filter is applied BEFORE Select, because EF Core
    /// can translate a.AppointmentDate to SQL but not a property of the StatRow record.
    /// </summary>
    private IQueryable<StatRow> StatRows(DateOnly? from = null, DateOnly? to = null)
    {
        var query = db.Appointments.AsNoTracking();
        if (from.HasValue) query = query.Where(a => a.AppointmentDate >= from.Value);
        if (to.HasValue) query = query.Where(a => a.AppointmentDate <= to.Value);

        return query.Select(a => new StatRow(
            a.AppointmentId, a.AppointmentDate, a.StartTime, a.Status, a.Price,
            a.CustomerId, a.Customer.FirstName + " " + a.Customer.LastName, a.Customer.Email ?? "",
            a.ServiceId, a.Service.Name, a.Service.Category,
            a.StylistId, a.Stylist.FirstName + " " + a.Stylist.LastName));
    }

    public async Task<DashboardDto> GetDashboardAsync()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var now = DateTime.Now;

        // Monday of this week: DayOfWeek is Sunday=0, Monday=1 ... so shift by 6 and wrap.
        var weekStart = today.AddDays(-(((int)today.DayOfWeek + 6) % 7));
        var weekEnd = weekStart.AddDays(6);
        var monthStart = new DateOnly(today.Year, today.Month, 1);

        var rows = await StatRows().ToListAsync();
        var active = rows.Where(r => r.Status != AppointmentStatus.Cancelled).ToList();
        var completed = rows.Where(r => r.Status == AppointmentStatus.Completed).ToList();

        var customers = await userManager.GetUsersInRoleAsync(Roles.Customer);

        var dashboard = new DashboardDto
        {
            TotalAppointments = rows.Count,
            AppointmentsThisWeek = active.Count(r => r.Date >= weekStart && r.Date <= weekEnd),
            UpcomingAppointments = active.Count(r =>
                (r.Status == AppointmentStatus.Pending || r.Status == AppointmentStatus.Confirmed) &&
                r.Date.ToDateTime(r.StartTime) > now),
            PendingAppointments = rows.Count(r => r.Status == AppointmentStatus.Pending),

            TotalCustomers = customers.Count,
            // "Active" = has booked something during the last 90 days.
            ActiveCustomers = active.Where(r => r.Date >= today.AddDays(-90))
                                    .Select(r => r.CustomerId).Distinct().Count(),

            AvailableStylists = await db.Stylists.CountAsync(s => s.IsAvailable),
            TotalStylists = await db.Stylists.CountAsync(),

            // Revenue estimate = the sum of all completed appointments.
            EstimatedRevenue = completed.Sum(r => r.Price),
            RevenueThisMonth = completed.Where(r => r.Date >= monthStart).Sum(r => r.Price),

            // GroupBy -> count -> sort -> top 5
            PopularServices = active
                .GroupBy(r => new { r.ServiceName, r.Category })
                .Select(g => new PopularServiceDto
                {
                    Name = g.Key.ServiceName,
                    Category = g.Key.Category.ToString(),
                    BookingCount = g.Count()
                })
                .OrderByDescending(p => p.BookingCount)
                .Take(5)
                .ToList(),

            // One bar per day for the next 7 days.
            BookingsNext7Days = Enumerable.Range(0, 7)
                .Select(offset => today.AddDays(offset))
                .Select(day => new ChartPointDto
                {
                    Label = day.ToString("ddd d/M", English),
                    Value = active.Count(r => r.Date == day)
                })
                .ToList(),

            StatusBreakdown = rows
                .GroupBy(r => r.Status)
                .OrderBy(g => g.Key)
                .Select(g => new ChartPointDto { Label = g.Key.ToString(), Value = g.Count() })
                .ToList()
        };

        dashboard.TotalReviews = await db.Reviews.CountAsync();
        dashboard.AverageRating = dashboard.TotalReviews == 0
            ? 0
            : Math.Round(await db.Reviews.AverageAsync(r => r.Rating), 1);

        // Today's appointments need the full details, so load them with Include.
        var todays = await db.Appointments
            .AsNoTracking()
            .Include(a => a.Service)
            .Include(a => a.Stylist)
            .Include(a => a.Customer)
            .Where(a => a.AppointmentDate == today && a.Status != AppointmentStatus.Cancelled)
            .OrderBy(a => a.StartTime)
            .ToListAsync();
        dashboard.TodaysAppointments = todays.Select(a => a.ToDto()).ToList();

        return dashboard;
    }

    /// <summary>The yearly report: monthly statistics, revenue analysis, top customers and stylists.</summary>
    public async Task<ReportDto> GetReportAsync(int year)
    {
        var allDates = await db.Appointments.Select(a => a.AppointmentDate).ToListAsync();
        var availableYears = allDates.Select(d => d.Year)
            .Append(DateTime.Today.Year)
            .Distinct()
            .OrderByDescending(y => y)
            .ToList();

        // DateOnly is stored as 'yyyy-MM-dd' text, so a range comparison works in SQL.
        var first = new DateOnly(year, 1, 1);
        var last = new DateOnly(year, 12, 31);
        var rows = await StatRows(first, last).ToListAsync();

        var completed = rows.Where(r => r.Status == AppointmentStatus.Completed).ToList();
        var notCancelled = rows.Where(r => r.Status != AppointmentStatus.Cancelled).ToList();

        // ToLookup groups the rows by month once; looking up a month with no rows gives an empty list.
        var byMonth = rows.ToLookup(r => r.Date.Month);

        var ratingsByStylist = await db.Reviews
            .Where(r => r.Appointment.AppointmentDate >= first && r.Appointment.AppointmentDate <= last)
            .GroupBy(r => r.Appointment.StylistId)
            .Select(g => new { StylistId = g.Key, Average = g.Average(r => r.Rating) })
            .ToDictionaryAsync(x => x.StylistId, x => x.Average);

        return new ReportDto
        {
            Year = year,
            AvailableYears = availableYears,

            TotalRevenue = completed.Sum(r => r.Price),
            AverageBookingValue = completed.Count == 0 ? 0 : Math.Round(completed.Average(r => r.Price), 0),
            CancellationRate = rows.Count == 0 ? 0
                : Math.Round(100.0 * rows.Count(r => r.Status == AppointmentStatus.Cancelled) / rows.Count, 1),

            BusiestWeekday = notCancelled
                .GroupBy(r => r.Date.DayOfWeek)
                .OrderByDescending(g => g.Count())
                .Select(g => g.Key.ToString())
                .FirstOrDefault() ?? "-",

            BusiestHour = notCancelled
                .GroupBy(r => r.StartTime.Hour)
                .OrderByDescending(g => g.Count())
                .Select(g => $"{g.Key:00}:00-{g.Key + 1:00}:00")
                .FirstOrDefault() ?? "-",

            Months = Enumerable.Range(1, 12).Select(month => new MonthlyStatDto
            {
                Month = month,
                MonthName = English.DateTimeFormat.GetAbbreviatedMonthName(month),
                Bookings = byMonth[month].Count(r => r.Status != AppointmentStatus.Cancelled),
                Completed = byMonth[month].Count(r => r.Status == AppointmentStatus.Completed),
                Cancelled = byMonth[month].Count(r => r.Status == AppointmentStatus.Cancelled),
                Revenue = byMonth[month].Where(r => r.Status == AppointmentStatus.Completed).Sum(r => r.Price)
            }).ToList(),

            RevenueByService = completed
                .GroupBy(r => r.ServiceName)
                .Select(g => new ChartPointDto { Label = g.Key, Value = g.Sum(r => r.Price) })
                .OrderByDescending(p => p.Value)
                .ToList(),

            RevenueByCategory = completed
                .GroupBy(r => r.Category)
                .Select(g => new ChartPointDto { Label = g.Key.ToString(), Value = g.Sum(r => r.Price) })
                .OrderByDescending(p => p.Value)
                .ToList(),

            // Customers with the most money spent (and bookings as a tie-breaker).
            TopCustomers = notCancelled
                .GroupBy(r => new { r.CustomerId, r.CustomerName, r.CustomerEmail })
                .Select(g => new TopCustomerDto
                {
                    Name = g.Key.CustomerName,
                    Email = g.Key.CustomerEmail,
                    Bookings = g.Count(),
                    TotalSpent = g.Where(r => r.Status == AppointmentStatus.Completed).Sum(r => r.Price)
                })
                .OrderByDescending(c => c.TotalSpent)
                .ThenByDescending(c => c.Bookings)
                .Take(5)
                .ToList(),

            StylistPerformance = rows
                .GroupBy(r => new { r.StylistId, r.StylistName })
                .Select(g => new StylistPerformanceDto
                {
                    Name = g.Key.StylistName,
                    CompletedAppointments = g.Count(r => r.Status == AppointmentStatus.Completed),
                    Revenue = g.Where(r => r.Status == AppointmentStatus.Completed).Sum(r => r.Price),
                    CancellationRate = Math.Round(100.0 * g.Count(r => r.Status == AppointmentStatus.Cancelled) / g.Count(), 1),
                    AverageRating = ratingsByStylist.TryGetValue(g.Key.StylistId, out var avg) ? Math.Round(avg, 1) : 0
                })
                .OrderByDescending(s => s.Revenue)
                .ToList()
        };
    }
}
