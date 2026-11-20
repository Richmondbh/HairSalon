using HairSalon.Api.Infrastructure;
using HairSalon.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HairSalon.Api.Controllers;

/// <summary>
///   GET    api/favorites       ids of my favourite stylists
///   POST   api/favorites/5     add stylist 5
///   DELETE api/favorites/5     remove stylist 5
/// </summary>
[ApiController]
[Route("api/favorites")]
[Authorize]
public class FavoritesController(FavoriteService favoriteService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<int>>> GetMine() =>
        Ok(await favoriteService.GetFavoriteIdsAsync(User.GetUserId()));

    [HttpPost("{stylistId:int}")]
    public async Task<IActionResult> Add(int stylistId)
    {
        await favoriteService.AddAsync(User.GetUserId(), stylistId);
        return NoContent();
    }

    [HttpDelete("{stylistId:int}")]
    public async Task<IActionResult> Remove(int stylistId)
    {
        await favoriteService.RemoveAsync(User.GetUserId(), stylistId);
        return NoContent();
    }
}
