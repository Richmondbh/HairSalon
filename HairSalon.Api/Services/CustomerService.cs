using HairSalon.Api.Data;
using HairSalon.Api.Infrastructure;
using HairSalon.Api.Mapping;
using HairSalon.Api.Models;
using HairSalon.Shared.Dtos;
using HairSalon.Shared.Enums;
using HairSalon.Shared.Rules;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace HairSalon.Api.Services;

/// <summary>
/// CUSTOMER MANAGEMENT MODULE (Grade A).
/// Combines Identity data (UserManager) with booking statistics (LINQ over appointments).
/// </summary>
public class CustomerService(SalonDbContext db, UserManager<ApplicationUser> userManager)
{
    public async Task<List<CustomerDto>> GetCustomersAsync(string? search)
    {
        IEnumerable<ApplicationUser> customers = await userManager.GetUsersInRoleAsync(Roles.Customer);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            customers = customers.Where(c =>
                c.FullName.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                (c.Email ?? "").Contains(term, StringComparison.OrdinalIgnoreCase) ||
                (c.PhoneNumber ?? "").Contains(term));
        }

        // One query for all appointments, then GroupBy in memory -> a dictionary per customer.
        var appointments = await db.Appointments
            .AsNoTracking()
            .Select(a => new AppointmentRow(a.CustomerId, a.Status, a.Price, a.AppointmentDate))
            .ToListAsync();
        var byCustomer = appointments.ToLookup(a => a.CustomerId);

        return customers
            .Select(c => ToCustomerDto(c, byCustomer[c.Id].ToList()))
            .OrderBy(c => c.FullName)
            .ToList();
    }

    public async Task<CustomerDetailsDto> GetCustomerAsync(string id)
    {
        var user = await GetCustomerUserAsync(id);

        var appointments = await db.Appointments
            .AsNoTracking()
            .Include(a => a.Service)
            .Include(a => a.Stylist)
            .Include(a => a.Customer)
            .Where(a => a.CustomerId == id)
            .OrderByDescending(a => a.AppointmentDate)
            .ThenByDescending(a => a.StartTime)
            .ToListAsync();

        var rows = appointments.Select(a => new AppointmentRow(a.CustomerId, a.Status, a.Price, a.AppointmentDate)).ToList();
        var summary = ToCustomerDto(user, rows);
        var notCancelled = appointments.Where(a => a.Status != AppointmentStatus.Cancelled).ToList();

        return new CustomerDetailsDto
        {
            Id = summary.Id,
            FullName = summary.FullName,
            Email = summary.Email,
            PhoneNumber = summary.PhoneNumber,
            RegistrationDate = summary.RegistrationDate,
            TotalAppointments = summary.TotalAppointments,
            CompletedAppointments = summary.CompletedAppointments,
            CancelledAppointments = summary.CancelledAppointments,
            TotalSpent = summary.TotalSpent,
            LastVisit = summary.LastVisit,
            IsLocked = summary.IsLocked,
            LoyaltyPoints = summary.LoyaltyPoints,
            LoyaltyTier = summary.LoyaltyTier,
            // The service/stylist this customer books most often.
            FavoriteService = notCancelled.GroupBy(a => a.Service.Name)
                .OrderByDescending(g => g.Count()).Select(g => g.Key).FirstOrDefault(),
            FavoriteStylist = notCancelled.GroupBy(a => a.Stylist.FullName)
                .OrderByDescending(g => g.Count()).Select(g => g.Key).FirstOrDefault(),
            Appointments = appointments.Select(a => a.ToDto()).ToList()
        };
    }

    /// <summary>A locked customer cannot log in (checked in AuthService.LoginAsync).</summary>
    public async Task<string> SetLockedAsync(string id, bool locked)
    {
        var user = await GetCustomerUserAsync(id);

        await userManager.SetLockoutEnabledAsync(user, true);
        await userManager.SetLockoutEndDateAsync(user, locked ? DateTimeOffset.MaxValue : null);

        return locked
            ? $"{user.FullName}'s account is now locked."
            : $"{user.FullName}'s account is now unlocked.";
    }

    private async Task<ApplicationUser> GetCustomerUserAsync(string id)
    {
        var user = await userManager.FindByIdAsync(id)
                   ?? throw new NotFoundException("The customer was not found.");

        if (!await userManager.IsInRoleAsync(user, Roles.Customer))
            throw new BusinessRuleException("This account is not a customer account.");

        return user;
    }

    private record AppointmentRow(string CustomerId, AppointmentStatus Status, decimal Price, DateOnly Date);

    private static CustomerDto ToCustomerDto(ApplicationUser user, List<AppointmentRow> rows)
    {
        var completed = rows.Where(r => r.Status == AppointmentStatus.Completed).ToList();
        var totalSpent = completed.Sum(r => r.Price);
        var points = LoyaltyRules.CalculatePoints(totalSpent);

        return new CustomerDto
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email ?? string.Empty,
            PhoneNumber = user.PhoneNumber,
            RegistrationDate = user.RegistrationDate,
            TotalAppointments = rows.Count,
            CompletedAppointments = completed.Count,
            CancelledAppointments = rows.Count(r => r.Status == AppointmentStatus.Cancelled),
            TotalSpent = totalSpent,
            LastVisit = completed.Count == 0 ? null : completed.Max(r => r.Date),
            IsLocked = user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTimeOffset.Now,
            LoyaltyPoints = points,
            LoyaltyTier = LoyaltyRules.GetTier(points),
        };
    }
}
