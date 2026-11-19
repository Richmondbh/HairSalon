using HairSalon.Client.Auth;
using HairSalon.Shared.Dtos;

namespace HairSalon.Client.Services;

/// <summary>Calls the api/notifications endpoints.</summary>
public class NotificationApiClient(HttpClient http, JwtAuthenticationStateProvider authState)
    : ApiClientBase(http, authState)
{
    public Task<List<NotificationDto>> GetMineAsync(bool unreadOnly = false) =>
        GetAsync<List<NotificationDto>>(WithQuery("api/notifications", ("unreadOnly", unreadOnly ? true : null)));

    public Task<int> GetUnreadCountAsync() => GetAsync<int>("api/notifications/unread-count");

    public Task MarkAsReadAsync(int id) => PutAsync($"api/notifications/{id}/read");

    public Task MarkAllAsReadAsync() => PutAsync("api/notifications/read-all");

    public Task DeleteAsync(int id) => DeleteAsync($"api/notifications/{id}");
}
