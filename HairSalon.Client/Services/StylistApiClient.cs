using HairSalon.Client.Auth;
using HairSalon.Shared.Dtos;

namespace HairSalon.Client.Services;

/// <summary>Calls the api/stylists endpoints.</summary>
public class StylistApiClient(HttpClient http, JwtAuthenticationStateProvider authState)
    : ApiClientBase(http, authState)
{
    public Task<List<StylistDto>> GetAllAsync(string? search = null, bool onlyAvailable = false) =>
        GetAsync<List<StylistDto>>(WithQuery("api/stylists",
            ("search", search), ("onlyAvailable", onlyAvailable ? true : null)));

    public Task<StylistDto> GetByIdAsync(int id) => GetAsync<StylistDto>($"api/stylists/{id}");

    public Task<StylistDto> CreateAsync(StylistEditDto dto) => PostAsync<StylistDto>("api/stylists", dto);

    public Task<StylistDto> UpdateAsync(int id, StylistEditDto dto) => PutAsync<StylistDto>($"api/stylists/{id}", dto);

    public async Task<string> DeleteAsync(int id) => (await DeleteAsync<ApiMessage>($"api/stylists/{id}")).Message;

    public Task<List<StylistDto>> GetTopRatedAsync(int count) =>
        GetAsync<List<StylistDto>>($"api/stylists/top?count={count}");
}
