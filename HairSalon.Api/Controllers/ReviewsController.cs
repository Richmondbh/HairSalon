using HairSalon.Api.Infrastructure;
using HairSalon.Api.Services;
using HairSalon.Shared.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HairSalon.Api.Controllers;

/// <summary>
///   POST api/reviews                  logged in (review a completed appointment)
///   GET  api/reviews/stylist/5        everyone
///   GET  api/reviews/latest?count=3   everyone (home page)
/// </summary>
[ApiController]
[Route("api/reviews")]
public class ReviewsController(ReviewService reviewService) : ControllerBase
{
    [Authorize]
    [HttpPost]
    public async Task<ActionResult<ReviewDto>> Create(CreateReviewDto dto) =>
        Ok(await reviewService.CreateAsync(User.GetUserId(), dto));

    [HttpGet("stylist/{stylistId:int}")]
    public async Task<ActionResult<List<ReviewDto>>> GetForStylist(int stylistId) =>
        Ok(await reviewService.GetForStylistAsync(stylistId));

    [HttpGet("latest")]
    public async Task<ActionResult<List<ReviewDto>>> GetLatest([FromQuery] int count = 3) =>
        Ok(await reviewService.GetLatestAsync(Math.Clamp(count, 1, 20)));
}
