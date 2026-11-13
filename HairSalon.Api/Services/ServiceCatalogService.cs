using HairSalon.Api.Data;
using HairSalon.Api.Infrastructure;
using HairSalon.Api.Mapping;
using HairSalon.Api.Models;
using HairSalon.Shared.Dtos;
using HairSalon.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace HairSalon.Api.Services;

/// <summary>
/// BUSINESS LOGIC for the salon's services (haircuts, colouring ...).
/// Named "ServiceCatalogService" so it isn't confused with the word "service" in
/// "service layer": it manages the catalogue of treatments the salon sells.
/// </summary>
public class ServiceCatalogService(SalonDbContext db)
{
    /// <summary>Search and filter. Inactive services are only included for administrators.</summary>
    public async Task<List<ServiceDto>> GetAllAsync(string? search, ServiceCategory? category, bool includeInactive)
    {
        // Build the query step by step. Nothing is sent to the database until ToListAsync().
        IQueryable<Service> query = db.Services.AsNoTracking();

        if (!includeInactive)
            query = query.Where(s => s.IsActive);

        if (category.HasValue)
            query = query.Where(s => s.Category == category.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(s => s.Name.ToLower().Contains(term) || s.Description.ToLower().Contains(term));
        }

        var services = await query.OrderBy(s => s.Name).ToListAsync();
        return services.Select(s => s.ToDto()).ToList();
    }

    public async Task<ServiceDto> GetByIdAsync(int id)
    {
        var service = await db.Services.FindAsync(id)
                      ?? throw new NotFoundException($"Service {id} was not found.");
        return service.ToDto();
    }

    public async Task<ServiceDto> CreateAsync(ServiceEditDto dto)
    {
        await EnsureUniqueNameAsync(dto.Name, excludeId: null);

        var service = new Service();
        service.UpdateFrom(dto);
        db.Services.Add(service);
        await db.SaveChangesAsync();   // EF Core fills in service.ServiceId here

        return service.ToDto();
    }

    public async Task<ServiceDto> UpdateAsync(int id, ServiceEditDto dto)
    {
        var service = await db.Services.FindAsync(id)
                      ?? throw new NotFoundException($"Service {id} was not found.");

        await EnsureUniqueNameAsync(dto.Name, excludeId: id);

        service.UpdateFrom(dto);   // EF Core tracks the changes ...
        await db.SaveChangesAsync(); // ... and writes only the changed columns.
        return service.ToDto();
    }

    /// <summary>
    /// Deletes a service. If it has appointments it is DEACTIVATED instead, because deleting
    /// it would break the booking history and the revenue statistics.
    /// </summary>
    public async Task<string> DeleteAsync(int id)
    {
        var service = await db.Services.FindAsync(id)
                      ?? throw new NotFoundException($"Service {id} was not found.");

        var hasAppointments = await db.Appointments.AnyAsync(a => a.ServiceId == id);
        if (hasAppointments)
        {
            service.IsActive = false;
            await db.SaveChangesAsync();
            return $"'{service.Name}' has bookings, so it was deactivated instead of deleted.";
        }

        db.Services.Remove(service);
        await db.SaveChangesAsync();
        return $"'{service.Name}' was deleted.";
    }

    private async Task EnsureUniqueNameAsync(string name, int? excludeId)
    {
        var normalized = name.Trim().ToLower();
        var exists = await db.Services.AnyAsync(s => s.Name.ToLower() == normalized && s.ServiceId != excludeId);
        if (exists)
            throw new BusinessRuleException($"A service called '{name.Trim()}' already exists.");
    }
}
