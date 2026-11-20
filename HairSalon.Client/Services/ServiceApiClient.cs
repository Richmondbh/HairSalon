using HairSalon.Client.Auth;
using HairSalon.Shared.Dtos;
using HairSalon.Shared.Enums;

namespace HairSalon.Client.Services;

/// <summary>Calls the api/services endpoints.</summary>
public class ServiceApiClient(HttpClient http, JwtAuthenticationStateProvider authState)
    : ApiClientBase(http, authState)
{
    public Task<List<ServiceDto>> GetAllAsync(string? search = null, ServiceCategory? category = null, bool includeInactive = false) =>
        GetAsync<List<ServiceDto>>(WithQuery("api/services",
            ("search", search), ("category", category), ("includeInactive", includeInactive ? true : null)));

    public Task<ServiceDto> GetByIdAsync(int id) => GetAsync<ServiceDto>($"api/services/{id}");

    public Task<ServiceDto> CreateAsync(ServiceEditDto dto) => PostAsync<ServiceDto>("api/services", dto);

    public Task<ServiceDto> UpdateAsync(int id, ServiceEditDto dto) => PutAsync<ServiceDto>($"api/services/{id}", dto);

    public async Task<string> DeleteAsync(int id) => (await DeleteAsync<ApiMessage>($"api/services/{id}")).Message;

    public Task<List<ServiceDto>> GetRecommendedAsync(int count) =>
        GetAsync<List<ServiceDto>>($"api/services/recommended?count={count}");

    public Task<List<ServiceDto>> GetPopularAsync(int count) =>
        GetAsync<List<ServiceDto>>($"api/services/popular?count={count}");
}
