using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using HairSalon.Client.Auth;
using HairSalon.Shared.Rules;

namespace HairSalon.Client.Services;

/// <summary>
/// Base class for all API clients (the client-side "application services" layer).
/// It contains the plumbing that every client needs:
///   - sending GET/POST/PUT/DELETE requests with the shared JSON settings
///   - turning error responses into an ApiException with a readable message
///   - logging the user out when the API answers 401 (token expired)
/// The derived classes (ServiceApiClient, AppointmentApiClient ...) only contain URLs.
/// </summary>
public abstract class ApiClientBase(HttpClient http, JwtAuthenticationStateProvider authState)
{
    protected HttpClient Http { get; } = http;
    protected JwtAuthenticationStateProvider AuthState { get; } = authState;

    protected async Task<T> GetAsync<T>(string url)
    {
        var response = await SendAsync(() => Http.GetAsync(url));
        return await ReadAsync<T>(response);
    }

    protected async Task<T> PostAsync<T>(string url, object? body)
    {
        var response = await SendAsync(() => Http.PostAsJsonAsync(url, body, SalonJson.Options));
        return await ReadAsync<T>(response);
    }

    protected async Task PostAsync(string url, object? body = null)
    {
        await SendAsync(() => Http.PostAsJsonAsync(url, body, SalonJson.Options));
    }

    protected async Task<T> PutAsync<T>(string url, object? body)
    {
        var response = await SendAsync(() => Http.PutAsJsonAsync(url, body, SalonJson.Options));
        return await ReadAsync<T>(response);
    }

    protected async Task PutAsync(string url, object? body = null)
    {
        await SendAsync(() => body == null ? Http.PutAsync(url, null) : Http.PutAsJsonAsync(url, body, SalonJson.Options));
    }

    protected async Task<T> DeleteAsync<T>(string url)
    {
        var response = await SendAsync(() => Http.DeleteAsync(url));
        return await ReadAsync<T>(response);
    }

    protected async Task DeleteAsync(string url)
    {
        await SendAsync(() => Http.DeleteAsync(url));
    }

    /// <summary>
    /// Builds "api/services?search=cut&amp;category=Haircut". Empty values are skipped and
    /// dates are always written as yyyy-MM-dd, whatever the browser's language.
    /// </summary>
    protected static string WithQuery(string path, params (string Name, object? Value)[] parameters)
    {
        var parts = parameters
            .Where(p => p.Value != null && p.Value.ToString() != string.Empty)
            .Select(p => $"{p.Name}={Uri.EscapeDataString(FormatValue(p.Value!))}");

        var query = string.Join("&", parts);
        return query.Length == 0 ? path : $"{path}?{query}";
    }

    private static string FormatValue(object value) => value switch
    {
        DateOnly date => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
        bool flag => flag ? "true" : "false",
        IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
        _ => value.ToString() ?? string.Empty
    };

    // ------------------------------------------------------------------------------

    private async Task<HttpResponseMessage> SendAsync(Func<Task<HttpResponseMessage>> request)
    {
        HttpResponseMessage response;
        try
        {
            response = await request();
        }
        catch (HttpRequestException)
        {
            // The API is not running, or CORS blocked the call.
            throw new ApiException("Cannot reach the server. Make sure HairSalon.Api is running.");
        }

        if (response.IsSuccessStatusCode)
            return response;

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            await AuthState.MarkUserAsLoggedOutAsync();
            throw new ApiException("Please log in again - your session has expired.", 401);
        }

        throw new ApiException(await ReadErrorMessageAsync(response), (int)response.StatusCode);
    }

    private static async Task<T> ReadAsync<T>(HttpResponseMessage response)
    {
        var result = await response.Content.ReadFromJsonAsync<T>(SalonJson.Options);
        return result ?? throw new ApiException("The server returned an empty answer.");
    }

    /// <summary>
    /// The API sends errors in two shapes:
    ///   { "message": "..." }                         from our ApiExceptionHandler
    ///   { "title": "...", "errors": { "Price": [...] } } automatic model validation (400)
    /// </summary>
    private static async Task<string> ReadErrorMessageAsync(HttpResponseMessage response)
    {
        try
        {
            var json = await response.Content.ReadFromJsonAsync<JsonElement>();
            if (json.ValueKind == JsonValueKind.Object)
            {
                if (json.TryGetProperty("message", out var message) && message.ValueKind == JsonValueKind.String)
                    return message.GetString()!;

                if (json.TryGetProperty("errors", out var errors) && errors.ValueKind == JsonValueKind.Object)
                {
                    var texts = errors.EnumerateObject()
                        .SelectMany(field => field.Value.EnumerateArray())
                        .Select(text => text.GetString())
                        .Where(text => !string.IsNullOrWhiteSpace(text));
                    return string.Join(" ", texts);
                }

                if (json.TryGetProperty("title", out var title) && title.ValueKind == JsonValueKind.String)
                    return title.GetString()!;
            }
        }
        catch (Exception)
        {
            // The body was not JSON - fall through to the generic messages.
        }

        return response.StatusCode switch
        {
            HttpStatusCode.Forbidden => "You do not have permission to do that.",
            HttpStatusCode.NotFound => "The item could not be found.",
            _ => $"Something went wrong (HTTP {(int)response.StatusCode}). Please try again."
        };
    }
}
