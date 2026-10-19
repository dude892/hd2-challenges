using Microsoft.Playwright;
using Xunit;

namespace Hd2Challenges.EndToEnd.Tests;

[Collection(WarpathWebAppCollection.Name)]
public sealed class RedemptionModalTests(WarpathWebAppFixture webApp) : RedemptionPageTest(webApp)
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PendingRewardsLockMissionFailureUntilClaimedOrBanned(bool banRewards)
    {
        await StartRunAsync();
        await Modals.MissionComplete.OpenAsync();
        await Modals.MissionComplete.ThreeStarsButton.ClickAsync();
        await Modals.RewardOptions.OpenAsync();

        RewardOptionsModal rewardOptions = Modals.RewardOptions;
        await Expect(rewardOptions.Heading).ToBeVisibleAsync();
        string[] rewards = [.. await rewardOptions.Choices.GetByRole(AriaRole.Button).AllTextContentsAsync()];
        await Expect(rewardOptions.Choices).ToHaveAttributeAsync("data-item-count", rewards.Length.ToString());
        await rewardOptions.CloseAsync();
        await Expect(Modals.PunishmentOptions.OpenButton).ToBeDisabledAsync();
        await Expect(Modals.MissionComplete.OpenButton).ToBeEnabledAsync();
        await Expect(CurrentMissionProgress).ToHaveAttributeAsync("data-mission-number", "1");
        await Expect(CurrentMissionProgress).ToHaveAttributeAsync("data-mission-count", "2");

        await Page.ReloadAsync();
        await Expect(Modals.PunishmentOptions.OpenButton).ToBeDisabledAsync();
        await Modals.MissionComplete.OpenAsync();
        await Expect(rewardOptions.Heading).ToBeVisibleAsync();
        await Expect(rewardOptions.Choices.GetByRole(AriaRole.Button)).ToHaveTextAsync(rewards);
        await Expect(rewardOptions.Choices).ToHaveAttributeAsync("data-item-count", rewards.Length.ToString());
        await Expect(Modals.MissionComplete.ContinueToRewardsButton).ToHaveCountAsync(0);

        if (banRewards)
        {
            await rewardOptions.SkipAndBanAllButton.ClickAsync();
        }
        else
        {
            await rewardOptions.Choices.GetByRole(AriaRole.Button).First.ClickAsync();
        }

        await Expect(rewardOptions.Dialog).ToHaveCountAsync(0);
        await Expect(Modals.PunishmentOptions.OpenButton).ToBeEnabledAsync();
        await Expect(Modals.MissionComplete.OpenButton).ToBeEnabledAsync();
        await Expect(CurrentMissionProgress).ToHaveAttributeAsync("data-mission-number", "2");
        await Expect(CurrentMissionProgress).ToHaveAttributeAsync("data-mission-count", "2");
    }

    [Fact]
    public async Task PendingPunishmentLocksMissionCompletionUntilClaimed()
    {
        await ImportRunAsync(acquiredItemIds: ["uavrecon"]);
        await Expect(MissionsFailedCount).ToHaveAttributeAsync("data-missions-failed", "0");
        await Modals.PunishmentOptions.OpenAsync();

        PunishmentOptionsModal punishmentOptions = Modals.PunishmentOptions;
        await Expect(punishmentOptions.Heading).ToBeVisibleAsync();
        await Expect(punishmentOptions.Choices.GetByRole(AriaRole.Button)).ToHaveCountAsync(1);
        await Expect(punishmentOptions.Choices).ToHaveAttributeAsync("data-item-count", "1");
        await Expect(punishmentOptions.Choices).ToContainTextAsync("UAV Recon");
        await punishmentOptions.CloseAsync();
        await Expect(Modals.MissionComplete.OpenButton).ToBeDisabledAsync();
        await Expect(punishmentOptions.OpenButton).ToBeEnabledAsync();

        await Page.ReloadAsync();
        await Expect(MissionsFailedCount).ToHaveAttributeAsync("data-missions-failed", "0");
        await Expect(Modals.MissionComplete.OpenButton).ToBeDisabledAsync();
        await punishmentOptions.OpenAsync();
        await Expect(punishmentOptions.Heading).ToBeVisibleAsync();
        await Expect(punishmentOptions.Choices.GetByRole(AriaRole.Button)).ToHaveCountAsync(1);
        await Expect(punishmentOptions.Choices).ToHaveAttributeAsync("data-item-count", "1");
        await Expect(punishmentOptions.Choices).ToContainTextAsync("UAV Recon");
        await punishmentOptions.Choices.GetByRole(AriaRole.Button, new() { Name = "UAV Recon", Exact = true }).ClickAsync();

        await Expect(punishmentOptions.Dialog).ToHaveCountAsync(0);
        await Expect(Modals.MissionComplete.OpenButton).ToBeEnabledAsync();
        await Expect(punishmentOptions.OpenButton).ToBeEnabledAsync();
        await Expect(MissionsFailedCount).ToHaveAttributeAsync("data-missions-failed", "1");
        await Page.ReloadAsync();
        await Expect(MissionsFailedCount).ToHaveAttributeAsync("data-missions-failed", "1");
        await Expect(AcquiredBoostersRegion
            .GetByText("UAV Recon", new() { Exact = true })).ToHaveCountAsync(0);
    }

    [Theory]
    [InlineData(BackdropDismissibleModal.SaveSlots)]
    [InlineData(BackdropDismissibleModal.RunSettings)]
    [InlineData(BackdropDismissibleModal.MissionComplete)]
    public async Task CloseButtonDismissesModalWithoutChangingMission(BackdropDismissibleModal modalType)
    {
        await StartRunAsync();
        ModalObject modal = GetBackdropDismissibleModal(modalType);
        await modal.OpenAsync();

        await Expect(modal.Heading).ToBeVisibleAsync();
        await modal.CloseAsync();

        await Expect(modal.Dialog).ToHaveCountAsync(0);
        await Expect(CurrentMissionProgress).ToHaveAttributeAsync("data-mission-number", "1");
        await Expect(CurrentMissionProgress).ToHaveAttributeAsync("data-mission-count", "2");
        await Expect(Modals.MissionComplete.OpenButton).ToBeEnabledAsync();
    }

    [Theory]
    [InlineData(BackdropDismissibleModal.RunSettings)]
    [InlineData(BackdropDismissibleModal.SaveSlots)]
    [InlineData(BackdropDismissibleModal.MissionComplete)]
    [InlineData(BackdropDismissibleModal.RewardOptions)]
    [InlineData(BackdropDismissibleModal.PunishmentOptions)]
    [InlineData(BackdropDismissibleModal.BannedItems)]
    public async Task BackdropClickClosesModalAndInsideClickDoesNot(BackdropDismissibleModal modalType)
    {
        ModalObject modal = await OpenBackdropDismissibleModalAsync(modalType);
        await Expect(modal.Dialog).ToBeVisibleAsync();
        await modal.Heading.ClickAsync();
        await Expect(modal.Dialog).ToBeVisibleAsync();

        await modal.ClickBackdropAsync();
        await Expect(modal.Dialog).ToHaveCountAsync(0);
    }

    private async Task<ModalObject> OpenBackdropDismissibleModalAsync(BackdropDismissibleModal modalType)
    {
        switch (modalType)
        {
            case BackdropDismissibleModal.RunSettings:
                await StartRunAsync();
                await Modals.RunSettings.OpenAsync();
                return Modals.RunSettings;
            case BackdropDismissibleModal.SaveSlots:
                await StartRunAsync();
                await Modals.SaveSlots.OpenAsync();
                return Modals.SaveSlots;
            case BackdropDismissibleModal.MissionComplete:
                await StartRunAsync();
                await Modals.MissionComplete.OpenAsync();
                return Modals.MissionComplete;
            case BackdropDismissibleModal.RewardOptions:
                await StartRunAsync();
                await Modals.MissionComplete.OpenAsync();
                await Modals.MissionComplete.ThreeStarsButton.ClickAsync();
                await Modals.RewardOptions.OpenAsync();
                return Modals.RewardOptions;
            case BackdropDismissibleModal.PunishmentOptions:
                await ImportRunAsync(acquiredItemIds: ["uavrecon"]);
                await Modals.PunishmentOptions.OpenAsync();
                return Modals.PunishmentOptions;
            case BackdropDismissibleModal.BannedItems:
                await StartRunAsync();
                await Modals.MissionComplete.OpenAsync();
                await Modals.MissionComplete.ThreeStarsButton.ClickAsync();
                await Modals.RewardOptions.OpenAsync();
                await Modals.RewardOptions.SkipAndBanAllButton.ClickAsync();
                await Modals.BannedItems.OpenAsync();
                return Modals.BannedItems;
            default:
                throw new ArgumentOutOfRangeException(nameof(modalType), modalType, null);
        }
    }

    private ModalObject GetBackdropDismissibleModal(BackdropDismissibleModal modalType) => modalType switch
    {
        BackdropDismissibleModal.RunSettings => Modals.RunSettings,
        BackdropDismissibleModal.SaveSlots => Modals.SaveSlots,
        BackdropDismissibleModal.MissionComplete => Modals.MissionComplete,
        BackdropDismissibleModal.RewardOptions => Modals.RewardOptions,
        BackdropDismissibleModal.PunishmentOptions => Modals.PunishmentOptions,
        BackdropDismissibleModal.BannedItems => Modals.BannedItems,
        _ => throw new ArgumentOutOfRangeException(nameof(modalType), modalType, null)
    };

    public enum BackdropDismissibleModal
    {
        RunSettings,
        SaveSlots,
        MissionComplete,
        RewardOptions,
        PunishmentOptions,
        BannedItems
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

    [Theory]
    [InlineData(NonDismissibleModal.RenameSlot)]
    [InlineData(NonDismissibleModal.DeleteConfirmation)]
    public async Task BackdropClickDoesNotCloseModal(NonDismissibleModal modalType)
    {
        await StartRunAsync();
        await Modals.SaveSlots.OpenAsync();
        ModalObject modal = modalType == NonDismissibleModal.RenameSlot
            ? Modals.RenameSlot
            : Modals.DeleteConfirmation;
        await modal.OpenAsync();

        await Expect(modal.Dialog).ToBeVisibleAsync();
        await modal.ClickBackdropAsync();
        await Expect(modal.Dialog).ToBeVisibleAsync();
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
        await Page.ReloadAsync();
        await Expect(CurrentMissionProgress).ToHaveAttributeAsync("data-mission-number", "1");
        await Expect(CurrentMissionProgress).ToHaveAttributeAsync("data-mission-count", "2");
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

        await Page.ReloadAsync();
        await Expect(BeginRunButton).ToBeVisibleAsync();
    }

    public enum NonDismissibleModal
    {
        RenameSlot,
        DeleteConfirmation
    }
}
