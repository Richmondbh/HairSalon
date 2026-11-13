using System.Text;
using System.Text.Json.Serialization;
using HairSalon.Api.Data;
using HairSalon.Api.Models;
using HairSalon.Api.Infrastructure;
using HairSalon.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

// =====================================================================================
//  Program.cs - the start-up file of the Web API.
//  Part 1 (before builder.Build()) registers SERVICES in the dependency injection (DI)
//  container. Part 2 (after Build()) sets up the MIDDLEWARE pipeline that every HTTP
//  request passes through.
// =====================================================================================

var builder = WebApplication.CreateBuilder(args);

// ---------- 1. Database: Entity Framework Core with SQLite ----------
// The connection string ("Data Source=salon.db") lives in appsettings.json.
builder.Services.AddDbContext<SalonDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("SalonDb")));

// ---------- 2. ASP.NET Core Identity (users, passwords and roles) ----------
// AddIdentityCore registers UserManager/RoleManager WITHOUT cookie login.
// We use JWT tokens instead of cookies because the Blazor WebAssembly client is a
// separate application that calls the API over HTTP.
builder.Services
    .AddIdentityCore<ApplicationUser>(options =>
    {
        options.User.RequireUniqueEmail = true;
        options.Password.RequiredLength = 6;
        options.Password.RequireDigit = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireNonAlphanumeric = false;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<SalonDbContext>();

// ---------- 3. Authentication with JWT bearer tokens ----------
// Every request may carry "Authorization: Bearer <token>". The middleware checks the
// signature, issuer, audience and expiry and turns the claims into HttpContext.User.
var jwt = builder.Configuration.GetSection("Jwt");
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // Keep the short claim names ("sub", "name", "role") exactly as they are in the token.
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt["Issuer"],
            ValidateAudience = true,
            ValidAudience = jwt["Audience"],
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["Key"]!)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1),
            NameClaimType = "name",   // User.Identity.Name
            RoleClaimType = "role"    // [Authorize(Roles = "Admin")]
        };
    });
builder.Services.AddAuthorization();

// ---------- 4. Business services (the "service layer") ----------
// Scoped = one instance per HTTP request, which matches the lifetime of the DbContext.
builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<ServiceCatalogService>();

// ---------- 5. Controllers + JSON ----------
// Enums are sent as text ("Confirmed") instead of numbers (1) - easier to read and test.
builder.Services.AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// ---------- 6. Central error handling ----------
// Business exceptions thrown anywhere in the service layer become clean JSON answers.
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddProblemDetails();

// ---------- 7. Swagger (interactive API documentation at /swagger) ----------
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "Nordic Hair Studio API", Version = "v1" });

    // Adds the "Authorize" button so you can paste a JWT and test protected endpoints.
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Log in with POST /api/auth/login and paste the token here."
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

// ---------- 8. CORS ----------
// The Blazor client runs on another port (https://localhost:7100). Browsers block calls
// to another "origin" unless the server allows it. This policy allows our client only.
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options =>
    options.AddPolicy("BlazorClient", policy =>
        policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

// Create/upgrade the database and insert sample data (see Data/DbSeeder.cs).
await DbSeeder.InitializeAsync(app.Services);

// ===================== The middleware pipeline (order matters!) =====================
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("BlazorClient");
app.UseAuthentication();   // Who are you? (reads the JWT)
app.UseAuthorization();    // Are you allowed? ([Authorize] attributes)

app.MapControllers();

app.Run();
