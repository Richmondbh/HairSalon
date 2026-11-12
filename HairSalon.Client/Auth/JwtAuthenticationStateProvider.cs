using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace HairSalon.Client.Auth;

/// <summary>
/// Tells Blazor WHO the current user is.
///
/// Blazor asks GetAuthenticationStateAsync() whenever it needs to know (for <AuthorizeView>,
/// [Authorize] pages, AuthorizeRouteView ...). We answer by reading the JWT from localStorage
/// and turning its claims into a ClaimsPrincipal.
///
/// After login/logout we call NotifyAuthenticationStateChanged, and every component that
/// depends on the user re-renders automatically (e.g. the menu shows "Log out").
/// </summary>
public class JwtAuthenticationStateProvider(TokenStorage tokenStorage) : AuthenticationStateProvider
{
    private static readonly AuthenticationState Anonymous = new(new ClaimsPrincipal(new ClaimsIdentity()));

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var token = await tokenStorage.GetTokenAsync();
        if (string.IsNullOrWhiteSpace(token))
            return Anonymous;

        try
        {
            var claims = JwtParser.ParseClaims(token);

            // "exp" = expiry time in seconds since 1970-01-01 (Unix time).
            var exp = claims.FirstOrDefault(c => c.Type == "exp")?.Value;
            if (long.TryParse(exp, out var seconds) && DateTimeOffset.FromUnixTimeSeconds(seconds) <= DateTimeOffset.UtcNow)
            {
                await tokenStorage.RemoveTokenAsync();
                return Anonymous;
            }

            // authenticationType must be set, otherwise IsAuthenticated is false.
            // nameType/roleType tell .NET which claims hold the name and the roles.
            var identity = new ClaimsIdentity(claims, authenticationType: "jwt", nameType: "name", roleType: "role");
            return new AuthenticationState(new ClaimsPrincipal(identity));
        }
        catch (Exception)
        {
            // A broken token -> remove it and treat the user as logged out.
            await tokenStorage.RemoveTokenAsync();
            return Anonymous;
        }
    }

    public async Task MarkUserAsLoggedInAsync(string token)
    {
        await tokenStorage.SaveTokenAsync(token);
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    public async Task MarkUserAsLoggedOutAsync()
    {
        await tokenStorage.RemoveTokenAsync();
        NotifyAuthenticationStateChanged(Task.FromResult(Anonymous));
    }
}
