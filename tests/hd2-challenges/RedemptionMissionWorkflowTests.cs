using Hd2Challenges.Models;
using Hd2Challenges.Services;

namespace Hd2Challenges.Tests;

public sealed class RedemptionMissionWorkflowTests
{
    private static RedemptionState CreateState(CatalogTestContext context) =>
        context.RedemptionCatalog.CreateRedemptionSetupState(
            context.RedemptionCatalog.GetDifficulty("normal"), context.Catalog.GetAllWarbonds());

    [Fact]
    public async Task BeginRunStartsTheRunWithItsStarterItems()
    {
        using CatalogTestContext context = await CatalogTestContext.CreateAsync();
        RedemptionState state = CreateState(context);

        state.BeginRun();

        Assert.True(state.RunStarted);
        Assert.Equal(state.StarterItems.ToHashSet(), state.AcquiredItems.ToHashSet());
    }

    [Fact]
    public async Task BeginRunPreservesAlreadyAcquiredItems()
    {
        using CatalogTestContext context = await CatalogTestContext.CreateAsync();
        RedemptionState state = CreateState(context);
        GameItem acquired = context.Catalog.GetAllItems().First(item => !state.StarterItems.ContainsItem(item));
        state.AcquiredItems.AddItem(acquired);

        state.BeginRun();
        state.BeginRun();

        Assert.Equal(state.StarterItems.Append(acquired).ToHashSet(), state.AcquiredItems.ToHashSet());
    }

    [Fact]
    public async Task CompletingMissionOffersRewardsWithoutAdvancing()
    {
        using CatalogTestContext context = await CatalogTestContext.CreateAsync();
        RedemptionState state = CreateState(context);
        RedemptionMissionService missions = new(context.Catalog, new(context.Catalog));
        Operation operation = state.CurrentOperation.Clone();
        int stars = operation.MaxStars;

        MissionCompletionResult result = missions.CompleteMission(state, stars);

        Assert.Equal(MissionCompletionResult.RewardsPending, result);
        Assert.Equal(Math.Max(1, stars - 1), state.PendingRewardItems.Count);
        Assert.Equal(operation.DifficultyIndex, state.CurrentOperation.DifficultyIndex);
        Assert.Equal(operation.MissionNumber, state.CurrentOperation.MissionNumber);
    }

    [Fact]
    public async Task CompletingMissionPreservesExistingRewardChoices()
    {
        using CatalogTestContext context = await CatalogTestContext.CreateAsync();
        RedemptionState state = CreateState(context);
        RedemptionMissionService missions = new(context.Catalog, new(context.Catalog));
        GameItem reward = context.Catalog.GetAllItems().First();
        state.PendingRewardItems.AddItem(reward);

        Assert.Equal(MissionCompletionResult.RewardsPending, missions.CompleteMission(state, 0));
        Assert.Equal(reward, Assert.Single(state.PendingRewardItems));
    }

    [Fact]
    public async Task CompletingFinalMissionCompletesRunWithoutOfferingRewards()
    {
        using CatalogTestContext context = await CatalogTestContext.CreateAsync();
        RedemptionState state = CreateState(context);
        RedemptionMissionService missions = new(context.Catalog, new(context.Catalog));
        Operation finalOperation = context.Catalog.GetOperation(context.Catalog.LastOperationIndex);
        state.CurrentOperation = context.Catalog.GetOperation(finalOperation.DifficultyIndex, finalOperation.MissionCount);

        MissionCompletionResult result = missions.CompleteMission(state, state.CurrentOperation.MaxStars);

        Assert.Equal(MissionCompletionResult.ChallengeCompleted, result);
        Assert.True(state.RunCompleted);
        Assert.Empty(state.PendingRewardItems);
    }

    [Fact]
    public async Task CompletingMissionWithEmptyRewardPoolDoesNotAdvance()
    {
        using CatalogTestContext context = await CatalogTestContext.CreateAsync();
        RedemptionState state = CreateState(context);
        RedemptionMissionService missions = new(context.Catalog, new(context.Catalog));
        state.AcquiredItems.AddItems(context.Catalog.GetAllItems());
        Operation operation = state.CurrentOperation.Clone();

        MissionCompletionResult result = missions.CompleteMission(state, operation.MaxStars);

        Assert.Equal(MissionCompletionResult.NoRewardsAvailable, result);
        Assert.Empty(state.PendingRewardItems);
        Assert.Equal(operation.DifficultyIndex, state.CurrentOperation.DifficultyIndex);
        Assert.Equal(operation.MissionNumber, state.CurrentOperation.MissionNumber);
        Assert.False(state.RunCompleted);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CompletingUnavailableMissionDoesNotOfferRewards(bool runCompleted)
    {
        using CatalogTestContext context = await CatalogTestContext.CreateAsync();
        RedemptionState state = CreateState(context);
        RedemptionMissionService missions = new(context.Catalog, new(context.Catalog));
        state.RunCompleted = runCompleted;
        if (!runCompleted)
        {
            state.PendingPunishmentItems.AddItem(context.Catalog.GetAllItems().First());
        }

        Assert.Equal(MissionCompletionResult.Unavailable, missions.CompleteMission(state, state.CurrentOperation.MaxStars));
        Assert.Empty(state.PendingRewardItems);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ResolvingRewardsAcquiresOrBansChoicesAndAdvances(bool banRewards)
    {
        using CatalogTestContext context = await CatalogTestContext.CreateAsync();
        RedemptionState state = CreateState(context);
        RedemptionMissionService missions = new(context.Catalog, new(context.Catalog));
        GameItem reward = context.Catalog.GetAllItems().First(item => !state.StarterItems.ContainsItem(item));
        state.PendingRewardItems.AddItem(reward);
        int operationIndex = state.CurrentOperation.DifficultyIndex;
        state.CurrentOperation = context.Catalog.GetOperation(
            operationIndex, context.Catalog.GetOperation(operationIndex).MissionCount);
        state.CurrentOperation.SelectedStars = state.CurrentOperation.MaxStars;

        bool resolved = banRewards
            ? missions.SkipAndBanMissionRewards(state)
            : missions.ClaimMissionReward(state, reward.Id);

        Assert.True(resolved);
        Assert.Contains(reward, banRewards ? state.BannedItems : state.AcquiredItems);
        Assert.Empty(state.PendingRewardItems);
        Assert.Equal(operationIndex + 1, state.CurrentOperation.DifficultyIndex);
        Assert.Equal(1, state.CurrentOperation.MissionNumber);
        Assert.Equal(0, state.CurrentOperation.SelectedStars);
    }

    [Fact]
    public async Task InvalidRewardSelectionDoesNotAdvanceOrClearChoices()
    {
        using CatalogTestContext context = await CatalogTestContext.CreateAsync();
        RedemptionState state = CreateState(context);
        RedemptionMissionService missions = new(context.Catalog, new(context.Catalog));
        GameItem reward = context.Catalog.GetAllItems().First();
        state.PendingRewardItems.AddItem(reward);
        Operation operation = state.CurrentOperation.Clone();

        Assert.False(missions.ClaimMissionReward(state, "unknown-item"));
        Assert.Equal(reward, Assert.Single(state.PendingRewardItems));
        Assert.Empty(state.AcquiredItems);
        Assert.Equal(operation.DifficultyIndex, state.CurrentOperation.DifficultyIndex);
        Assert.Equal(operation.MissionNumber, state.CurrentOperation.MissionNumber);
    }

    [Fact]
    public async Task SkippingWithoutRewardsDoesNotAdvance()
    {
        using CatalogTestContext context = await CatalogTestContext.CreateAsync();
        RedemptionState state = CreateState(context);
        RedemptionMissionService missions = new(context.Catalog, new(context.Catalog));
        Operation operation = state.CurrentOperation.Clone();

        Assert.False(missions.SkipAndBanMissionRewards(state));
        Assert.Equal(operation.DifficultyIndex, state.CurrentOperation.DifficultyIndex);
        Assert.Equal(operation.MissionNumber, state.CurrentOperation.MissionNumber);
    }

    [Fact]
    public async Task PreparingFailureOffersAcquiredNonStarterItemsWithoutApplyingFailure()
    {
        using CatalogTestContext context = await CatalogTestContext.CreateAsync();
        RedemptionState state = CreateState(context);
        RedemptionMissionService missions = new(context.Catalog, new(context.Catalog));
        state.BeginRun();
        GameItem acquired = context.Catalog.GetAllItems().First(item => !state.StarterItems.ContainsItem(item));
        state.AcquiredItems.AddItem(acquired);

        Assert.True(missions.PrepareMissionFailure(state));
        Assert.Equal(acquired, Assert.Single(state.PendingPunishmentItems));
        Assert.Equal(0, state.MissionsFailed);
    }

    [Fact]
    public async Task PreparingFailurePreservesExistingChoices()
    {
        using CatalogTestContext context = await CatalogTestContext.CreateAsync();
        RedemptionState state = CreateState(context);
        RedemptionMissionService missions = new(context.Catalog, new(context.Catalog));
        GameItem punishment = context.Catalog.GetAllItems().First();
        state.PendingPunishmentItems.AddItem(punishment);

        Assert.False(missions.PrepareMissionFailure(state));
        Assert.Equal(punishment, Assert.Single(state.PendingPunishmentItems));
    }

    [Fact]
    public async Task PreparingFailureWithoutAcquiredRewardsDoesNotApplyFailure()
    {
        using CatalogTestContext context = await CatalogTestContext.CreateAsync();
        RedemptionState state = CreateState(context);
        RedemptionMissionService missions = new(context.Catalog, new(context.Catalog));
        state.BeginRun();

        missions.PrepareMissionFailure(state);

        Assert.Empty(state.PendingPunishmentItems);
        Assert.Equal(0, state.MissionsFailed);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PreparingUnavailableFailureDoesNotOfferPunishments(bool runCompleted)
    {
        using CatalogTestContext context = await CatalogTestContext.CreateAsync();
        RedemptionState state = CreateState(context);
        RedemptionMissionService missions = new(context.Catalog, new(context.Catalog));
        state.AcquiredItems.AddItems(context.Catalog.GetAllItems());
        state.RunCompleted = runCompleted;
        if (!runCompleted)
        {
            state.PendingRewardItems.AddItem(context.Catalog.GetAllItems().First());
        }

        Assert.False(missions.PrepareMissionFailure(state));
        Assert.Empty(state.PendingPunishmentItems);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PunishmentSelectionAppliesFailureOnlyForAPendingItem(bool validItem)
    {
        using CatalogTestContext context = await CatalogTestContext.CreateAsync();
        RedemptionState state = CreateState(context);
        RedemptionMissionService missions = new(context.Catalog, new(context.Catalog));
        GameItem punishment = context.Catalog.GetAllItems().First(item => !state.StarterItems.ContainsItem(item));
        state.AcquiredItems.AddItem(punishment);
        state.BannedItems.AddItem(punishment);
        state.PendingPunishmentItems.AddItem(punishment);
        state.CurrentOperation = context.Catalog.GetOperation(state.CurrentOperation.DifficultyIndex, 2);
        Operation operation = state.CurrentOperation.Clone();

        bool claimed = missions.ClaimMissionPunishment(state, validItem ? punishment.Id : "unknown-item");

        Assert.Equal(validItem, claimed);
        Assert.Equal(validItem ? 1 : 0, state.MissionsFailed);
        Assert.Equal(validItem ? 1 : operation.MissionNumber, state.CurrentOperation.MissionNumber);
        Assert.Equal(operation.DifficultyIndex, state.CurrentOperation.DifficultyIndex);
        Assert.Equal(!validItem, state.AcquiredItems.ContainsItem(punishment));
        Assert.Equal(!validItem, state.BannedItems.ContainsItem(punishment));
        Assert.Equal(!validItem, state.PendingPunishmentItems.ContainsItem(punishment));
    }
}
