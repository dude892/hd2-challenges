using Microsoft.Playwright;

namespace Hd2Challenges.EndToEnd.Tests;

internal sealed class RedemptionModals(IPage page)
{
    public MissionCompleteModal MissionComplete => new(page);
    public RewardOptionsModal RewardOptions => new(page, MissionComplete);
    public PunishmentOptionsModal PunishmentOptions => new(page);
    public BannedItemsModal BannedItems => new(page);
    public RunSettingsModal RunSettings => new(page);
    public SaveSlotsModal SaveSlots => new(page);
    public RenameSlotModal RenameSlot => new(page, SaveSlots);
    public DeleteConfirmationModal DeleteConfirmation => new(page, SaveSlots);
}

internal abstract class ModalObject
{
    private readonly string _title;

    protected ModalObject(IPage page, string title)
    {
        Page = page;
        _title = title;
    }

    protected IPage Page { get; }
    public ILocator Dialog => Page.GetByRole(AriaRole.Dialog, new() { Name = _title, Exact = true });
    public ILocator Heading => Dialog.GetByRole(AriaRole.Heading, new() { Name = _title, Exact = true });
    public ILocator CloseButton => Dialog.GetByRole(AriaRole.Button, new() { Name = "Close dialog", Exact = true });
    public ILocator Backdrop => Dialog.Locator("xpath=..");
    public abstract ILocator OpenButton { get; }

    public Task OpenAsync() => OpenButton.ClickAsync();

    public Task CloseAsync() => CloseButton.ClickAsync();

    public Task ClickBackdropAsync() => Backdrop.ClickAsync(new() { Position = new() { X = 0, Y = 0 } });
}

internal sealed class MissionCompleteModal(IPage page) : ModalObject(page, "Mission Complete")
{
    public override ILocator OpenButton => Page.GetByRole(AriaRole.Button, new() { Name = "Mission Complete", Exact = true });
    public ILocator ThreeStarsButton => Dialog.GetByRole(AriaRole.Button, new() { Name = "3 stars", Exact = true });
    public ILocator ContinueToRewardsButton => Dialog.GetByRole(AriaRole.Button, new() { Name = "Continue to Rewards", Exact = true });
}

internal sealed class RewardOptionsModal(IPage page, MissionCompleteModal missionComplete) : ModalObject(page, "Reward Options")
{
    public override ILocator OpenButton => missionComplete.ContinueToRewardsButton;
    public ILocator Choices => Dialog.GetByRole(AriaRole.Group, new() { Name = "Reward choices", Exact = true });
    public ILocator SkipAndBanAllButton => Dialog.GetByRole(AriaRole.Button, new() { Name = "Skip and Ban All", Exact = true });
}

internal sealed class PunishmentOptionsModal(IPage page) : ModalObject(page, "Punishment Options")
{
    public override ILocator OpenButton => Page.GetByRole(AriaRole.Button, new() { Name = "Mission Failed", Exact = true });
    public ILocator Choices => Dialog.GetByRole(AriaRole.Group, new() { Name = "Punishment choices", Exact = true });
}

internal sealed class BannedItemsModal(IPage page) : ModalObject(page, "Banned Items")
{
    public override ILocator OpenButton => Page.GetByRole(AriaRole.Button, new() { Name = "Banned Items", Exact = true });
    public ILocator Items => Dialog.GetByRole(AriaRole.Group, new() { Name = "Banned items", Exact = true });
}

internal sealed class RunSettingsModal(IPage page) : ModalObject(page, "Run Settings")
{
    public override ILocator OpenButton => Page.GetByRole(AriaRole.Button, new() { Name = "Run Settings", Exact = true });
}

internal sealed class SaveSlotsModal(IPage page) : ModalObject(page, "Warpath: Redemption Save Slots")
{
    public override ILocator OpenButton => Page.GetByRole(AriaRole.Button, new() { Name = "Save Slots", Exact = true });
    public ILocator CreateSnapshotButton => Dialog.GetByRole(AriaRole.Button, new() { Name = "Create Snapshot", Exact = true });
    public ILocator ImportSaveInput => Dialog.GetByLabel("Import Save");
    public ILocator Slots => Dialog.GetByRole(AriaRole.Group);
    public ILocator EditNameButton => Dialog.GetByRole(AriaRole.Button, new() { Name = "Edit name", Exact = true });
    public ILocator DeleteButton => Dialog.GetByRole(AriaRole.Button, new() { Name = "Delete", Exact = true });
    public ILocator ExportButton => Dialog.GetByRole(AriaRole.Button, new() { Name = "Export", Exact = true });
}

internal sealed class RenameSlotModal(IPage page, SaveSlotsModal saveSlots) : ModalObject(page, "Rename Save Slot")
{
    public override ILocator OpenButton => saveSlots.EditNameButton;
    public ILocator SlotNameInput => Dialog.GetByLabel("Slot name");
    public ILocator SaveNameButton => Dialog.GetByRole(AriaRole.Button, new() { Name = "Save name", Exact = true });
    public ILocator CancelButton => Dialog.GetByRole(AriaRole.Button, new() { Name = "Cancel", Exact = true });
}

internal sealed class DeleteConfirmationModal(IPage page, SaveSlotsModal saveSlots) : ModalObject(page, "Delete Current Save?")
{
    public override ILocator OpenButton => saveSlots.DeleteButton;
    public ILocator CancelButton => Dialog.GetByRole(AriaRole.Button, new() { Name = "Cancel", Exact = true });
    public ILocator ConfirmDeleteButton => Dialog.GetByRole(AriaRole.Button, new() { Name = "Delete Save", Exact = true });
}