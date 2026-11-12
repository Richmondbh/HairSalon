using System.Globalization;
using HairSalon.Client;
using HairSalon.Client.Auth;
using HairSalon.Client.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

// =====================================================================================
//  Program.cs of the Blazor WebAssembly client - runs IN THE BROWSER.
//  Registers the services that components can @inject.
// =====================================================================================

// Show dates and numbers the same way for every visitor ("Tue 12 Nov", "1,250 kr").
var culture = new CultureInfo("en-GB");
CultureInfo.DefaultThreadCurrentCulture = culture;
CultureInfo.DefaultThreadCurrentUICulture = culture;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// The address of HairSalon.Api, read from wwwroot/appsettings.json.
var apiBaseUrl = builder.Configuration["ApiBaseUrl"]
                 ?? throw new InvalidOperationException("ApiBaseUrl is missing in wwwroot/appsettings.json.");

// ---------- Authentication (JWT) ----------
// TokenStorage keeps the JWT in the browser's localStorage.
// JwtAuthenticationStateProvider tells Blazor who is logged in (used by <AuthorizeView>,
// [Authorize] on pages and @context.User).
builder.Services.AddScoped<TokenStorage>();
builder.Services.AddScoped<JwtAuthenticationStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp => sp.GetRequiredService<JwtAuthenticationStateProvider>());
builder.Services.AddAuthorizationCore();
builder.Services.AddCascadingAuthenticationState();

// ---------- HttpClient ----------
// Every request goes through AuthHeaderHandler, which adds "Authorization: Bearer <token>".
builder.Services.AddScoped(sp =>
    new HttpClient(new AuthHeaderHandler(sp.GetRequiredService<TokenStorage>()) { InnerHandler = new HttpClientHandler() })
    {
        BaseAddress = new Uri(apiBaseUrl)
    });

// ---------- Application services (typed API clients) ----------
// Components never build URLs or parse JSON themselves - they call these classes.
builder.Services.AddScoped<AuthApiClient>();

await builder.Build().RunAsync();
