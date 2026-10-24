using Hd2Challenges.Models;
using Xunit;

namespace Hd2Challenges.EndToEnd.Tests;

[Collection(WarpathWebAppCollection.Name)]
public sealed class RedemptionModalTests(WarpathWebAppFixture webApp) : RedemptionPageTest(webApp)
{
    public static TheoryData<SaveObject> OperationRatings =>
    [
        RedemptionSaves.OperationIndex1,
        RedemptionSaves.OperationIndex2,
        RedemptionSaves.OperationIndex3,
        RedemptionSaves.OperationIndex5,
        RedemptionSaves.OperationIndex7
    ];

    [Theory]
    [MemberData(nameof(OperationRatings))]
    public async Task MissionRatingButtonsMatchOperationMaximum(SaveObject save)
    {
        Operation operation = save.ResolveState(RedemptionCatalog).CurrentOperation;
        await Modals.MissionComplete.PrepareAndOpenAsync(save, ImportRunAsync);

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
    public async Task MissionRatingsAreInitiallyUnselected(SaveObject save)
    {
        Operation operation = save.ResolveState(RedemptionCatalog).CurrentOperation;
        await Modals.MissionComplete.PrepareAndOpenAsync(save, ImportRunAsync);

        foreach (var (button, _) in Modals.MissionComplete.GetRatings(operation))
        {
            await Expect(button).ToHaveAttributeAsync("aria-pressed", "false");
        }
    }

    [Theory]
    [MemberData(nameof(OperationRatings))]
    public async Task ClickingMissionRatingSelectsExactlyTheStarsUpToThatRating(SaveObject save)
    {
        Operation operation = save.ResolveState(RedemptionCatalog).CurrentOperation;
        await Modals.MissionComplete.PrepareAndOpenAsync(save, ImportRunAsync);
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
    public async Task ClickingSelectedMissionRatingClearsAllStars(SaveObject save)
    {
        Operation operation = save.ResolveState(RedemptionCatalog).CurrentOperation;
        await Modals.MissionComplete.PrepareAndOpenAsync(save, ImportRunAsync);
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
        SaveObject save = RedemptionSaves.AcquiredBooster;
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

}
