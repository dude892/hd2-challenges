namespace Hd2Challenges.Services;

public sealed class ToastService
{
    private readonly List<ToastMessage> _messages = [];

    public IReadOnlyList<ToastMessage> Messages => _messages;

    public event Action? Changed;

    public void Show(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        _messages.Add(new ToastMessage(Guid.NewGuid(), message));
        Changed?.Invoke();
    }

    public void Dismiss(Guid id)
    {
        if (_messages.RemoveAll(message => message.Id == id) > 0)
        {
            Changed?.Invoke();
        }
    }
}

public sealed record ToastMessage(Guid Id, string Message);