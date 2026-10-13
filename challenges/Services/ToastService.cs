namespace Hd2Challenges.Services;

public sealed class ToastService(IConfiguration configuration)
{
    private readonly List<ToastMessage> _messages = [];
    private readonly TimeSpan _autoCloseDelay = configuration.GetValue<TimeSpan>("Toast:AutoCloseDelay");

    public IReadOnlyList<ToastMessage> Messages => _messages;

    public event Action? Changed;

    public void Show(string message, bool autoClose = true)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        var toast = new ToastMessage(Guid.NewGuid(), message);
        _messages.Add(toast);
        Changed?.Invoke();

        if (autoClose)
        {
            _ = DismissAfterDelayAsync(toast.Id);
        }
    }

    public void Dismiss(Guid id)
    {
        if (_messages.RemoveAll(message => message.Id == id) > 0)
        {
            Changed?.Invoke();
        }
    }

    private async Task DismissAfterDelayAsync(Guid id)
    {
        await Task.Delay(_autoCloseDelay);
        Dismiss(id);
    }
}

public sealed record ToastMessage(Guid Id, string Message);