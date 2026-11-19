using HairSalon.Client.Auth;
using HairSalon.Shared.Dtos;

namespace HairSalon.Client.Services;

/// <summary>Calls the api/reviews endpoints.</summary>
public class ReviewApiClient(HttpClient http, JwtAuthenticationStateProvider authState)
    : ApiClientBase(http, authState)
{
    public Task<ReviewDto> CreateAsync(CreateReviewDto dto) => PostAsync<ReviewDto>("api/reviews", dto);

    public Task<List<ReviewDto>> GetForStylistAsync(int stylistId) =>
        GetAsync<List<ReviewDto>>($"api/reviews/stylist/{stylistId}");

    public Task<List<ReviewDto>> GetLatestAsync(int count) =>
        GetAsync<List<ReviewDto>>($"api/reviews/latest?count={count}");
}
