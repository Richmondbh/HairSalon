using HairSalon.Api.Data;
using HairSalon.Api.Infrastructure;
using HairSalon.Api.Mapping;
using HairSalon.Api.Models;
using HairSalon.Shared.Dtos;
using Microsoft.EntityFrameworkCore;

namespace HairSalon.Api.Services;

/// <summary>BUSINESS LOGIC for stylists (the salon's employees).</summary>
public class StylistService(SalonDbContext db)
{
    public async Task<List<StylistDto>> GetAllAsync(string? search, bool onlyAvailable)
    {
        IQueryable<Stylist> query = db.Stylists.AsNoTracking();

        if (onlyAvailable)
            query = query.Where(s => s.IsAvailable);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(s =>
                s.FirstName.ToLower().Contains(term) ||
                s.LastName.ToLower().Contains(term) ||
                s.Specialization.ToLower().Contains(term));
        }

        var stylists = await query.OrderBy(s => s.FirstName).ThenBy(s => s.LastName).ToListAsync();
        var dtos = stylists.Select(s => s.ToDto()).ToList();
        await AddRatingsAsync(dtos);
        return dtos;
    }

    public async Task<StylistDto> GetByIdAsync(int id)
    {
        var stylist = await db.Stylists.FindAsync(id)
                      ?? throw new NotFoundException($"Stylist {id} was not found.");
        var dto = stylist.ToDto();
        await AddRatingsAsync([dto]);
        return dto;
    }

    public async Task<StylistDto> CreateAsync(StylistEditDto dto)
    {
        await EnsureUniqueEmailAsync(dto.Email, excludeId: null);

        var stylist = new Stylist();
        stylist.UpdateFrom(dto);
        db.Stylists.Add(stylist);
        await db.SaveChangesAsync();
        return stylist.ToDto();
    }

    public async Task<StylistDto> UpdateAsync(int id, StylistEditDto dto)
    {
        var stylist = await db.Stylists.FindAsync(id)
                      ?? throw new NotFoundException($"Stylist {id} was not found.");

        await EnsureUniqueEmailAsync(dto.Email, excludeId: id);

        stylist.UpdateFrom(dto);
        await db.SaveChangesAsync();
        return stylist.ToDto();
    }

    /// <summary>Stylists with appointments are marked unavailable instead of deleted.</summary>
    public async Task<string> DeleteAsync(int id)
    {
        var stylist = await db.Stylists.FindAsync(id)
                      ?? throw new NotFoundException($"Stylist {id} was not found.");

        if (await db.Appointments.AnyAsync(a => a.StylistId == id))
        {
            stylist.IsAvailable = false;
            await db.SaveChangesAsync();
            return $"{stylist.FullName} has appointments, so they were marked as unavailable instead of deleted.";
        }

        db.Stylists.Remove(stylist);
        await db.SaveChangesAsync();
        return $"{stylist.FullName} was deleted.";
    }

    /// <summary>The best rated available stylists (home page).</summary>
    public async Task<List<StylistDto>> GetTopRatedAsync(int count)
    {
        var all = await GetAllAsync(search: null, onlyAvailable: true);
        return all
            .OrderByDescending(s => s.AverageRating)
            .ThenByDescending(s => s.ReviewCount)
            .Take(count)
            .ToList();
    }

    /// <summary>
    /// Calculates the average rating per stylist with ONE query:
    /// Reviews -> grouped by the appointment's stylist -> average + count.
    /// </summary>
    private async Task AddRatingsAsync(List<StylistDto> stylists)
    {
        var ratings = await db.Reviews
            .GroupBy(r => r.Appointment.StylistId)
            .Select(g => new { StylistId = g.Key, Average = g.Average(r => r.Rating), Count = g.Count() })
            .ToDictionaryAsync(x => x.StylistId);

        foreach (var stylist in stylists)
        {
            if (ratings.TryGetValue(stylist.StylistId, out var rating))
            {
                stylist.AverageRating = Math.Round(rating.Average, 1);
                stylist.ReviewCount = rating.Count;
            }
        }
    }

    private async Task EnsureUniqueEmailAsync(string email, int? excludeId)
    {
        var normalized = email.Trim().ToLower();
        if (await db.Stylists.AnyAsync(s => s.Email.ToLower() == normalized && s.StylistId != excludeId))
            throw new BusinessRuleException($"Another stylist already uses {email.Trim()}.");
    }
}
