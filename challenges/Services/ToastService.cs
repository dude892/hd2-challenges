namespace Hd2Challenges.Services;

public sealed record ToastMessage(Guid Id, string Message, bool IsDismissing = false);

public sealed class ToastService
{
    private readonly List<ToastMessage> _messages;
    private readonly IReadOnlyList<ToastMessage> _readOnlyMessages;

    public ToastService()
    {
        _messages = [];
        _readOnlyMessages = _messages.AsReadOnly();
    }

    public IReadOnlyList<ToastMessage> Messages => _readOnlyMessages;

    public event Action? Changed;

    public void Show(string message, int? autoCloseMilliseconds = 5000)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        ArgumentOutOfRangeException.ThrowIfNegative(autoCloseMilliseconds.GetValueOrDefault());

        var toast = new ToastMessage(Guid.NewGuid(), message);
        _messages.Add(toast);
        Changed?.Invoke();

        if (autoCloseMilliseconds.HasValue)
        {
            _ = DismissAfterDelayAsync(toast.Id, autoCloseMilliseconds.Value);
        }
    }

    public void Dismiss(Guid id)
    {
        if (_messages.RemoveAll(message => message.Id == id) > 0)
        {
            Changed?.Invoke();
        }
    }

    private async Task DismissAfterDelayAsync(Guid id, int delay)
    {
        await Task.Delay(delay);

        var index = _messages.FindIndex(message => message.Id == id);
        if (index >= 0)
        {
            _messages[index] = _messages[index] with { IsDismissing = true };
            Changed?.Invoke();
        }
    }
}