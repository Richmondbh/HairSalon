using HairSalon.Client.Auth;
using HairSalon.Shared.Dtos;

namespace HairSalon.Client.Services;

/// <summary>Login, registration and logout (+ the user's profile).</summary>
public class AuthApiClient(HttpClient http, JwtAuthenticationStateProvider authState)
    : ApiClientBase(http, authState)
{
    public async Task<AuthResponseDto> LoginAsync(LoginDto dto)
    {
        var response = await PostAsync<AuthResponseDto>("api/auth/login", dto);
        await AuthState.MarkUserAsLoggedInAsync(response.Token);
        return response;
    }

    public async Task<AuthResponseDto> RegisterAsync(RegisterDto dto)
    {
        var response = await PostAsync<AuthResponseDto>("api/auth/register", dto);
        await AuthState.MarkUserAsLoggedInAsync(response.Token);
        return response;
    }

    /// <summary>A JWT cannot be "cancelled" on the server - logging out = forgetting the token.</summary>
    public Task LogoutAsync() => AuthState.MarkUserAsLoggedOutAsync();
}
