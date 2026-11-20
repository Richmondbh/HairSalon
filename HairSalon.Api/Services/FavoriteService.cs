using HairSalon.Api.Data;
using HairSalon.Api.Infrastructure;
using HairSalon.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace HairSalon.Api.Services;

/// <summary>
/// FAVOURITE STYLISTS (Grade A) - works with the many-to-many join table FavoriteStylists.
/// </summary>
public class FavoriteService(SalonDbContext db)
{
    public Task<List<int>> GetFavoriteIdsAsync(string userId) =>
        db.FavoriteStylists
            .Where(f => f.UserId == userId)
            .Select(f => f.StylistId)
            .ToListAsync();

    public async Task AddAsync(string userId, int stylistId)
    {
        if (!await db.Stylists.AnyAsync(s => s.StylistId == stylistId))
            throw new NotFoundException("The stylist was not found.");

        // Adding the same favourite twice is simply ignored ("idempotent").
        if (await db.FavoriteStylists.AnyAsync(f => f.UserId == userId && f.StylistId == stylistId))
            return;

        db.FavoriteStylists.Add(new FavoriteStylist { UserId = userId, StylistId = stylistId });
        await db.SaveChangesAsync();
    }

    public async Task RemoveAsync(string userId, int stylistId)
    {
        // FindAsync with a composite key: the values in the same order as in HasKey(...).
        var favorite = await db.FavoriteStylists.FindAsync(userId, stylistId);
        if (favorite == null) return;

        db.FavoriteStylists.Remove(favorite);
        await db.SaveChangesAsync();
    }
}
