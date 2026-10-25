using Xunit;

namespace Hd2Challenges.EndToEnd.Tests;

[Collection(WarpathWebAppCollection.Name)]
public sealed class ModalTests(WarpathWebAppFixture webApp) : RedemptionPageTest(webApp)
{
    public static TheoryData<Type> ModalTypes { get; } =
    [
        (new TheoryDataRow<Type>(typeof(RunSettingsModal)) { Label = "modalType: RunSettingsModal" }),
        (new TheoryDataRow<Type>(typeof(SaveSlotsModal)) { Label = "modalType: SaveSlotsModal" }),
        (new TheoryDataRow<Type>(typeof(MissionCompleteModal)) { Label = "modalType: MissionCompleteModal" }),
        (new TheoryDataRow<Type>(typeof(RewardOptionsModal)) { Label = "modalType: RewardOptionsModal" }),
        (new TheoryDataRow<Type>(typeof(PunishmentOptionsModal)) { Label = "modalType: PunishmentOptionsModal" }),
        (new TheoryDataRow<Type>(typeof(BannedItemsModal)) { Label = "modalType: BannedItemsModal" }),
        (new TheoryDataRow<Type>(typeof(RenameSlotModal)) { Label = "modalType: RenameSlotModal" }),
        (new TheoryDataRow<Type>(typeof(DeleteConfirmationModal)) { Label = "modalType: DeleteConfirmationModal" })
    ];

    private async Task<ModalObject> OpenModalAsync(Type modalType)
    {
        ModalObject modal = Modals.All.Single(modal => modal.GetType() == modalType);
        if (modal is PunishmentOptionsModal)
        {
            await modal.PrepareAndOpenAsync(RedemptionSaves.AcquiredBooster, ImportRunAsync);
        }
        else
        {
            await modal.PrepareAndOpenAsync(StartRunAsync);
        }
        return modal;
    }

    [Theory]
    [MemberData(nameof(ModalTypes))]
    public async Task CloseButtonDismissesModalWithoutChangingMission(Type modalType)
    {
        ModalObject modal = await OpenModalAsync(modalType);

        await Expect(modal.Heading).ToBeVisibleAsync();
        string? operationIndex = await CurrentOperationProgress.GetAttributeAsync("data-operation-index");
        string? missionNumber = await CurrentMissionProgress.GetAttributeAsync("data-mission-number");
        string? missionCount = await CurrentMissionProgress.GetAttributeAsync("data-mission-count");
        Assert.NotNull(operationIndex);
        Assert.NotNull(missionNumber);
        Assert.NotNull(missionCount);
        bool missionCompleteEnabled = await Modals.MissionComplete.OpenButton.IsEnabledAsync();

        await modal.CloseAsync();

        await Expect(modal.Dialog).ToHaveCountAsync(0);
        await Expect(CurrentOperationProgress).ToHaveAttributeAsync("data-operation-index", operationIndex);
        await Expect(CurrentMissionProgress).ToHaveAttributeAsync("data-mission-number", missionNumber);
        await Expect(CurrentMissionProgress).ToHaveAttributeAsync("data-mission-count", missionCount);
        await Expect(Modals.MissionComplete.OpenButton).ToBeEnabledAsync(new() { Enabled = missionCompleteEnabled });
    }

    [Theory]
    [MemberData(nameof(ModalTypes))]
    public async Task BackdropClickRespectsModalDismissability(Type modalType)
    {
        ModalObject modal = await OpenModalAsync(modalType);
        await Expect(modal.Dialog).ToBeVisibleAsync();
        await modal.ClickBackdropAsync();

        if (modal.Dismissable)
        {
            await Expect(modal.Dialog).ToHaveCountAsync(0);
        }
        else
        {
            await Expect(modal.Dialog).ToBeVisibleAsync();
        }
    }

    [Theory]
    [MemberData(nameof(ModalTypes))]
    public async Task InsideClickDoesNotCloseModal(Type modalType)
    {
        ModalObject modal = await OpenModalAsync(modalType);
        await modal.Heading.ClickAsync();

        await Expect(modal.Dialog).ToBeVisibleAsync();
    }

    [Fact]
    public async Task RenameModalValidatesNameAndCancelDiscardsChanges()
    {
        await StartRunAsync();
        await Modals.SaveSlots.OpenAsync();
        await Modals.RenameSlot.OpenAsync();

        RenameSlotModal renameModal = Modals.RenameSlot;
        string originalName = await renameModal.SlotNameInput.InputValueAsync();
        await Expect(renameModal.SaveNameButton).ToBeDisabledAsync();
        await renameModal.SlotNameInput.FillAsync("   ");
        await Expect(renameModal.SaveNameButton).ToBeDisabledAsync();
        await renameModal.SlotNameInput.FillAsync("Unsaved modal test name");
        await Expect(renameModal.SaveNameButton).ToBeEnabledAsync();
        await renameModal.CancelButton.ClickAsync();

        await Expect(renameModal.Dialog).ToHaveCountAsync(0);
        await Expect(Modals.SaveSlots.Dialog).ToBeVisibleAsync();
        await Expect(Modals.SaveSlots.Dialog).ToContainTextAsync(originalName);
        await Expect(Modals.SaveSlots.Dialog).Not.ToContainTextAsync("Unsaved modal test name");
        await Modals.RenameSlot.OpenAsync();
        await Expect(renameModal.SlotNameInput).ToHaveValueAsync(originalName);
    }

    [Fact]
    public async Task CancelingDeleteConfirmationPreservesSave()
    {
        await StartRunAsync();
        await Modals.SaveSlots.OpenAsync();
        await Modals.DeleteConfirmation.OpenAsync();

        await Expect(Modals.DeleteConfirmation.Dialog).ToBeVisibleAsync();
        await Modals.DeleteConfirmation.CancelButton.ClickAsync();

        await Expect(Modals.DeleteConfirmation.Dialog).ToHaveCountAsync(0);
        await Expect(Modals.SaveSlots.DeleteButton).ToBeVisibleAsync();
        await Modals.SaveSlots.CloseAsync();
        await ReloadAsync();
        await Expect(CurrentMissionProgress).ToHaveAttributeAsync("data-mission-number", "1");
        await Expect(CurrentMissionProgress).ToHaveAttributeAsync("data-mission-count", DefaultStartOperation.MissionCount.ToString());
    }

    [Fact]
    public async Task ConfirmingDeleteRemovesSaveAndReturnsToSetup()
    {
        await StartRunAsync();
        await Modals.SaveSlots.OpenAsync();
        await Modals.DeleteConfirmation.OpenAsync();
        await Modals.DeleteConfirmation.ConfirmDeleteButton.ClickAsync();

        await Expect(Modals.DeleteConfirmation.Dialog).ToHaveCountAsync(0);
        await Expect(Modals.SaveSlots.DeleteButton).ToHaveCountAsync(0);
        await Modals.SaveSlots.CloseAsync();
        await Expect(BeginRunButton).ToBeVisibleAsync();

        await ReloadAsync();
        await Expect(BeginRunButton).ToBeVisibleAsync();
    }
}
