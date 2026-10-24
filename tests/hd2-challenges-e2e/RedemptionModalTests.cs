using Hd2Challenges.Models;
using Microsoft.Playwright;
using Xunit;

namespace Hd2Challenges.EndToEnd.Tests;

[Collection(WarpathWebAppCollection.Name)]
public sealed class RedemptionModalTests(WarpathWebAppFixture webApp) : RedemptionPageTest(webApp)
{
    public static TheoryData<string> OperationRatings { get; } = new()
    {
        "operation-index-1.json",
        "operation-index-2.json",
        "operation-index-3.json",
        "operation-index-5.json",
        "operation-index-7.json"
    };

    [Theory]
    [MemberData(nameof(OperationRatings))]
    public async Task MissionRatingButtonsMatchOperationMaximum(string saveFileName)
    {
        Operation operation = await OpenMissionRatingsAsync(saveFileName);

        foreach (var (button, stars) in Modals.MissionComplete.Ratings)
        {
            if (stars > operation.MaxStars)
            {
                await Expect(button).ToHaveCountAsync(0);
            }
            else
            {
                await Expect(button).ToBeVisibleAsync();
            }
        }
    }

    [Theory]
    [MemberData(nameof(OperationRatings))]
    public async Task MissionRatingsAreInitiallyUnselected(string saveFileName)
    {
        Operation operation = await OpenMissionRatingsAsync(saveFileName);

        foreach (var (button, _) in Modals.MissionComplete.GetRatings(operation))
        {
            await Expect(button).ToHaveAttributeAsync("aria-pressed", "false");
        }
    }

    [Theory]
    [MemberData(nameof(OperationRatings))]
    public async Task ClickingMissionRatingSelectsExactlyTheStarsUpToThatRating(string saveFileName)
    {
        Operation operation = await OpenMissionRatingsAsync(saveFileName);
        var availableRatings = Modals.MissionComplete.GetRatings(operation);

        foreach (var (button, stars) in availableRatings.Reverse())
        {
            await button.ClickAsync();
            foreach (var (ratingButton, ratingStars) in availableRatings)
            {
                await Expect(ratingButton).ToHaveAttributeAsync("aria-pressed", ratingStars <= stars ? "true" : "false");
            }
        }
    }

    [Theory]
    [MemberData(nameof(OperationRatings))]
    public async Task ClickingSelectedMissionRatingClearsAllStars(string saveFileName)
    {
        Operation operation = await OpenMissionRatingsAsync(saveFileName);
        var availableRatings = Modals.MissionComplete.GetRatings(operation);

        foreach (var (button, _) in availableRatings)
        {
            await button.ClickAsync();
            await button.ClickAsync();
            foreach (var (ratingButton, _) in availableRatings)
            {
                await Expect(ratingButton).ToHaveAttributeAsync("aria-pressed", "false");
            }
        }
    }

    private async Task<Operation> OpenMissionRatingsAsync(string saveFileName)
    {
        SaveObject save = new(saveFileName);
        Operation operation = save.ResolveState(RedemptionCatalog).CurrentOperation;
        await ImportRunAsync(save);
        await Modals.MissionComplete.OpenAsync();
        return operation;
    }

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
        string[] rewardIds = (await GetValidatedRewardItemsAsync()).Select(item => item.Id).ToArray();
        string[] rewards = [.. await rewardOptions.Choices.SelectableChoices.AllTextContentsAsync()];
        await rewardOptions.CloseAsync();
        await Expect(Modals.PunishmentOptions.OpenButton).ToBeDisabledAsync();
        await Expect(Modals.MissionComplete.OpenButton).ToBeEnabledAsync();
        await Expect(CurrentMissionProgress).ToHaveAttributeAsync("data-mission-number", "1");
        await Expect(CurrentMissionProgress).ToHaveAttributeAsync("data-mission-count", DefaultStartOperation.MissionCount.ToString());

        await ReloadAsync();
        await Expect(Modals.PunishmentOptions.OpenButton).ToBeDisabledAsync();
        await Modals.MissionComplete.OpenAsync();
        await Expect(rewardOptions.Heading).ToBeVisibleAsync();
        await Expect(rewardOptions.Choices.SelectableChoices).ToHaveTextAsync(rewards);
        Assert.Equal(rewardIds.OrderBy(id => id, StringComparer.OrdinalIgnoreCase),
            (await rewardOptions.Choices.GetItemIdsAsync()).OrderBy(id => id, StringComparer.OrdinalIgnoreCase));
        await Expect(Modals.MissionComplete.ContinueToRewardsButton).ToHaveCountAsync(0);

        if (banRewards)
        {
            await rewardOptions.SkipAndBanAllButton.ClickAsync();
        }
        else
        {
            await rewardOptions.Choices.SelectableChoices.First.ClickAsync();
        }

        await Expect(rewardOptions.Dialog).ToHaveCountAsync(0);
        await Expect(Modals.PunishmentOptions.OpenButton).ToBeEnabledAsync();
        await Expect(Modals.MissionComplete.OpenButton).ToBeEnabledAsync();
        await Expect(CurrentMissionProgress).ToHaveAttributeAsync("data-mission-number", "2");
        await Expect(CurrentMissionProgress).ToHaveAttributeAsync("data-mission-count", DefaultStartOperation.MissionCount.ToString());
    }

    [Fact]
    public async Task PendingPunishmentLocksMissionCompletionUntilClaimed()
    {
        SaveObject save = SaveObject.AcquiredBooster;
        RedemptionState state = save.ResolveState(RedemptionCatalog);
        Assert.NotEmpty(state.AcquiredItems);
        await ImportRunAsync(save);
        await Expect(MissionsFailedCount).ToHaveAttributeAsync("data-missions-failed", state.MissionsFailed.ToString());
        await Modals.PunishmentOptions.OpenAsync();

        PunishmentOptionsModal punishmentOptions = Modals.PunishmentOptions;
        await Expect(punishmentOptions.Heading).ToBeVisibleAsync();
        await punishmentOptions.Choices.AssertMatchesItemsAsync(state.AcquiredItems);
        await punishmentOptions.CloseAsync();
        await Expect(Modals.MissionComplete.OpenButton).ToBeDisabledAsync();
        await Expect(punishmentOptions.OpenButton).ToBeEnabledAsync();

        await ReloadAsync();
        await Expect(MissionsFailedCount).ToHaveAttributeAsync("data-missions-failed", state.MissionsFailed.ToString());
        await Expect(Modals.MissionComplete.OpenButton).ToBeDisabledAsync();
        await punishmentOptions.OpenAsync();
        await Expect(punishmentOptions.Heading).ToBeVisibleAsync();
        await punishmentOptions.Choices.AssertMatchesItemsAsync(state.AcquiredItems);
        string? selectedPunishmentId = await punishmentOptions.Choices.SelectableChoices.First.GetAttributeAsync("data-item-id");
        Assert.NotNull(selectedPunishmentId);
        await punishmentOptions.Choices.SelectableChoices.First.ClickAsync();

        await Expect(punishmentOptions.Dialog).ToHaveCountAsync(0);
        await Expect(Modals.MissionComplete.OpenButton).ToBeEnabledAsync();
        await Expect(punishmentOptions.OpenButton).ToBeEnabledAsync();
        await Expect(MissionsFailedCount).ToHaveAttributeAsync("data-missions-failed", "1");
        await ReloadAsync();
        await Expect(MissionsFailedCount).ToHaveAttributeAsync("data-missions-failed", "1");
        await ItemGrids.AcquiredItems.AssertDoesNotContainItemAsync(selectedPunishmentId);
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
        await Expect(CurrentMissionProgress).ToHaveAttributeAsync("data-mission-count", DefaultStartOperation.MissionCount.ToString());
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
                await GetValidatedRewardItemsAsync();
                return Modals.RewardOptions;
            case BackdropDismissibleModal.PunishmentOptions:
                await ImportRunAsync(SaveObject.AcquiredBooster);
                await Modals.PunishmentOptions.OpenAsync();
                return Modals.PunishmentOptions;
            case BackdropDismissibleModal.BannedItems:
                await StartRunAsync();
                await Modals.MissionComplete.OpenAsync();
                await Modals.MissionComplete.ThreeStarsButton.ClickAsync();
                await Modals.RewardOptions.OpenAsync();
                await GetValidatedRewardItemsAsync();
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

    public enum NonDismissibleModal
    {
        RenameSlot,
        DeleteConfirmation
    }
}
