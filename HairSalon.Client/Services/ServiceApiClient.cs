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
}
