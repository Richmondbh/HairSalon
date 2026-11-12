using System.Net.Http.Headers;

namespace HairSalon.Client.Auth;

/// <summary>
/// A DelegatingHandler sits in the HttpClient "pipeline" and can change every request
/// before it is sent. This one adds the JWT:  Authorization: Bearer eyJhbGciOi...
/// Thanks to this, no API client or page ever has to think about the token.
/// </summary>
public class AuthHeaderHandler(TokenStorage tokenStorage) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = await tokenStorage.GetTokenAsync();
        if (!string.IsNullOrWhiteSpace(token))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        return await base.SendAsync(request, cancellationToken);
    }
}
