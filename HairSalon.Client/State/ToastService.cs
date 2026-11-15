namespace HairSalon.Client.State;

public enum ToastLevel { Success, Error, Info }

public record ToastMessage(Guid Id, string Text, ToastLevel Level);

/// <summary>
/// SHARED STATE for short pop-up messages ("Service saved").
///
/// This is one way Blazor components communicate WITHOUT being parent and child:
///  - any page calls Toasts.ShowSuccess("...")
///  - the service raises the OnChange event
///  - ToastContainer (in MainLayout) is subscribed to OnChange and re-renders.
/// The service is registered once (scoped = one per browser tab in WebAssembly),
/// so all components share the same instance.
/// </summary>
public class ToastService
{
    private readonly List<ToastMessage> _toasts = new();

    public IReadOnlyList<ToastMessage> Toasts => _toasts;

    public event Action? OnChange;

    public void ShowSuccess(string text) => Show(text, ToastLevel.Success);
    public void ShowError(string text) => Show(text, ToastLevel.Error);
    public void ShowInfo(string text) => Show(text, ToastLevel.Info);

    public void Show(string text, ToastLevel level)
    {
        var toast = new ToastMessage(Guid.NewGuid(), text, level);
        _toasts.Add(toast);
        OnChange?.Invoke();
        _ = RemoveAfterDelayAsync(toast.Id);   // "fire and forget"
    }

    public void Remove(Guid id)
    {
        if (_toasts.RemoveAll(t => t.Id == id) > 0)
            OnChange?.Invoke();
    }

    private async Task RemoveAfterDelayAsync(Guid id)
    {
        await Task.Delay(TimeSpan.FromSeconds(4));
        Remove(id);
    }
}
