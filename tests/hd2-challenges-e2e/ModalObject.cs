using Microsoft.Playwright;

namespace Hd2Challenges.EndToEnd.Tests;

internal abstract class ModalObject(IPage page, string title)
{
    private readonly string _title = title;

    protected IPage Page { get; } = page;
    public ILocator Dialog => Page.GetByRole(AriaRole.Dialog, new() { Name = _title, Exact = true });
    public ILocator Heading => Dialog.GetByRole(AriaRole.Heading, new() { Name = _title, Exact = true });
    public ILocator CloseButton => Dialog.GetByRole(AriaRole.Button, new() { Name = $"Close {_title} dialog", Exact = true });
    public ILocator Backdrop => Dialog.Locator("xpath=..");
    public abstract ILocator OpenButton { get; }
    public virtual bool Dismissable => true;

    public Task OpenAsync() => OpenButton.ClickAsync();

    public virtual async Task PrepareAndOpenAsync(Func<Task> startRun)
    {
        await startRun();
        await OpenAsync();
    }

    public virtual async Task PrepareAndOpenAsync(SaveObject save, Func<SaveObject, Task> importRun)
    {
        await importRun(save);
        await OpenAsync();
    }

    public Task CloseAsync() => CloseButton.ClickAsync();

    public Task ClickBackdropAsync() => Backdrop.ClickAsync(new() { Position = new() { X = 0, Y = 0 } });
}
