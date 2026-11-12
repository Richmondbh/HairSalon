using Microsoft.JSInterop;

namespace HairSalon.Client.Auth;

/// <summary>
/// Saves the JWT in the browser's localStorage so the user stays logged in after a
/// page refresh. Blazor WebAssembly cannot touch browser storage directly from C#, so it
/// calls the JavaScript functions localStorage.getItem/setItem/removeItem through IJSRuntime.
/// </summary>
public class TokenStorage(IJSRuntime js)
{
    private const string Key = "nordic-salon-token";

    public ValueTask<string?> GetTokenAsync() => js.InvokeAsync<string?>("localStorage.getItem", Key);

    public ValueTask SaveTokenAsync(string token) => js.InvokeVoidAsync("localStorage.setItem", Key, token);

    public ValueTask RemoveTokenAsync() => js.InvokeVoidAsync("localStorage.removeItem", Key);
}
