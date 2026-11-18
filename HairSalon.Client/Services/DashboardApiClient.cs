using HairSalon.Client.Auth;
using HairSalon.Shared.Dtos;

namespace HairSalon.Client.Services;

/// <summary>Calls the admin-only api/dashboard endpoints.</summary>
public class DashboardApiClient(HttpClient http, JwtAuthenticationStateProvider authState)
    : ApiClientBase(http, authState)
{
    public Task<DashboardDto> GetDashboardAsync() => GetAsync<DashboardDto>("api/dashboard");

    public Task<ReportDto> GetReportAsync(int year) => GetAsync<ReportDto>($"api/dashboard/report?year={year}");
}
