using System.Text.Json.Serialization;
using HairSalon.Api.Data;
using HairSalon.Api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
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

// ---------- 5. Controllers + JSON ----------
// Enums are sent as text ("Confirmed") instead of numbers (1) - easier to read and test.
builder.Services.AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// ---------- 7. Swagger (interactive API documentation at /swagger) ----------
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "Nordic Hair Studio API", Version = "v1" });
});

// ---------- 8. CORS ----------
// The Blazor client runs on another port (https://localhost:7100). Browsers block calls
// to another "origin" unless the server allows it. This policy allows our client only.
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options =>
    options.AddPolicy("BlazorClient", policy =>
        policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

// ===================== The middleware pipeline (order matters!) =====================

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("BlazorClient");
app.UseAuthorization();    // Are you allowed? ([Authorize] attributes)

app.MapControllers();

app.Run();
