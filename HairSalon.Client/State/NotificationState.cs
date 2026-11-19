using HairSalon.Client.Services;

namespace HairSalon.Client.State;

/// <summary>
/// SHARED STATE: the number of unread notifications.
/// NotificationBell (in the menu) shows the number; the Notifications page changes it
/// when you mark something as read. Because both use THIS object, the bell updates
/// immediately - the two components never need to know about each other.
/// </summary>
public class NotificationState(NotificationApiClient notificationApi)
{
    public int UnreadCount { get; private set; }

    public event Action? OnChange;

    /// <summary>Asks the API for the latest count. Returns how many NEW unread arrived.</summary>
    public async Task<int> RefreshAsync()
    {
        try
        {
            var count = await notificationApi.GetUnreadCountAsync();
            var newOnes = Math.Max(0, count - UnreadCount);
            SetUnreadCount(count);
            return newOnes;
        }
        catch (ApiException)
        {
            return 0;   // polling must never crash the page
        }
    }

    public void SetUnreadCount(int count)
    {
        if (count == UnreadCount) return;
        UnreadCount = Math.Max(0, count);
        OnChange?.Invoke();
    }
}
