using HairSalon.Client.Auth;
using HairSalon.Shared.Dtos;
using HairSalon.Shared.Enums;

namespace HairSalon.Client.Services;

/// <summary>Calls the api/appointments endpoints.</summary>
public class AppointmentApiClient(HttpClient http, JwtAuthenticationStateProvider authState)
    : ApiClientBase(http, authState)
{
    public Task<List<TimeSlotDto>> GetAvailableSlotsAsync(int stylistId, int serviceId, DateOnly date, int? excludeAppointmentId = null) =>
        GetAsync<List<TimeSlotDto>>(WithQuery("api/appointments/available-slots",
            ("stylistId", stylistId), ("serviceId", serviceId), ("date", date), ("excludeAppointmentId", excludeAppointmentId)));

    public Task<AppointmentDto> CreateAsync(CreateAppointmentDto dto) =>
        PostAsync<AppointmentDto>("api/appointments", dto);

    /// <summary>filter: "upcoming", "history" or null for all.</summary>
    public Task<List<AppointmentDto>> GetMineAsync(string? filter) =>
        GetAsync<List<AppointmentDto>>(WithQuery("api/appointments/mine", ("filter", filter)));

    public Task<AppointmentDto> CancelAsync(int id) =>
        PutAsync<AppointmentDto>($"api/appointments/{id}/cancel", null);

    public Task<AppointmentDto> RescheduleAsync(int id, RescheduleDto dto) =>
        PutAsync<AppointmentDto>($"api/appointments/{id}/reschedule", dto);

    public Task<List<AppointmentDto>> GetAllAsync(AppointmentStatus? status, int? stylistId, DateOnly? from, DateOnly? to, string? search) =>
        GetAsync<List<AppointmentDto>>(WithQuery("api/appointments",
            ("status", status), ("stylistId", stylistId), ("from", from), ("to", to), ("search", search)));

    public Task<AppointmentDto> UpdateStatusAsync(int id, UpdateStatusDto dto) =>
        PutAsync<AppointmentDto>($"api/appointments/{id}/status", dto);
}
