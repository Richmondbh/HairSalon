using HairSalon.Client.Auth;
using HairSalon.Shared.Dtos;

namespace HairSalon.Client.Services;

/// <summary>Calls the admin-only api/customers endpoints.</summary>
public class CustomerApiClient(HttpClient http, JwtAuthenticationStateProvider authState)
    : ApiClientBase(http, authState)
{
    public Task<List<CustomerDto>> GetAllAsync(string? search) =>
        GetAsync<List<CustomerDto>>(WithQuery("api/customers", ("search", search)));

    public Task<CustomerDetailsDto> GetByIdAsync(string id) =>
        GetAsync<CustomerDetailsDto>($"api/customers/{Uri.EscapeDataString(id)}");

    public async Task<string> SetLockedAsync(string id, bool locked)
    {
        var action = locked ? "lock" : "unlock";
        var result = await PutAsync<ApiMessage>($"api/customers/{Uri.EscapeDataString(id)}/{action}", null);
        return result.Message;
    }
}
