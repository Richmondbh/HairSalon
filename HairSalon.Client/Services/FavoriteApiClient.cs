using HairSalon.Client.Auth;

namespace HairSalon.Client.Services;

/// <summary>Calls the api/favorites endpoints.</summary>
public class FavoriteApiClient(HttpClient http, JwtAuthenticationStateProvider authState)
    : ApiClientBase(http, authState)
{
    public Task<List<int>> GetFavoriteIdsAsync() => GetAsync<List<int>>("api/favorites");

    public Task AddAsync(int stylistId) => PostAsync($"api/favorites/{stylistId}");

    public Task RemoveAsync(int stylistId) => DeleteAsync($"api/favorites/{stylistId}");
}
