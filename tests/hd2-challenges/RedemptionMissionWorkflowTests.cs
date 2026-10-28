using Hd2Challenges.Models;
using Hd2Challenges.Services;

namespace Hd2Challenges.Tests;

public sealed class RedemptionMissionWorkflowTests
{
    private const int HardFinaleOperationIndex = 5;
    private const int MaximumPunishmentChoices = 3;

    private static RedemptionState CreateState(CatalogTestContext context) =>
        context.RedemptionCatalog.CreateRedemptionSetupState(
            context.RedemptionCatalog.GetDifficulty("normal"),
            context.RedemptionCatalog.GetAllWarbonds());

    private static async Task<RedemptionSessionService> CreateMissionScenarioAsync(CatalogTestContext context)
    {
        RedemptionSessionService session = await context.CreateSessionAsync();
        await session.BeginRunAsync();
        return session;
    }

    private static Operation GetLastMission(RedemptionCatalogService catalog, int operationIndex)
    {
        Operation operation = catalog.GetOperation(operationIndex);
        operation.MissionNumber = operation.MissionCount;
        return operation;
    }

    [Fact]
    public async Task ChangingSetupDifficultyUpdatesOperationAndLoadout()
    {
        using CatalogTestContext context = await CatalogTestContext.CreateAsync();
        RedemptionState state = CreateState(context);
        RedemptionDifficulty difficulty = context.RedemptionCatalog.GetDifficulty("super");
        HashSet<Warbond> selectedWarbonds = state.SelectedWarbonds.ToHashSet();

        state.ChangeDifficulty(difficulty);

        Assert.Same(difficulty, state.Difficulty);
        Assert.Equal(difficulty.StartOperation.DifficultyIndex, state.CurrentOperation.DifficultyIndex);
        Assert.Equal(difficulty.StartOperation.MissionNumber, state.CurrentOperation.MissionNumber);
        Assert.NotSame(difficulty.StartOperation, state.CurrentOperation);
        Assert.Equal(selectedWarbonds, state.SelectedWarbonds.ToHashSet());
        Assert.Equal(context.RedemptionCatalog.GetStarterItems(difficulty, selectedWarbonds).ToHashSet(),
            state.StarterItems.ToHashSet());
        Assert.Equal(difficulty.Id, state.ToDefinition().DifficultyId);
        Assert.False(state.RunStarted);
    }

    [Fact]
    public async Task ChangingDifficultyAfterRunStartsFailsWithoutChangingState()
    {
        using CatalogTestContext context = await CatalogTestContext.CreateAsync();
        RedemptionState state = CreateState(context);
        state.BeginRun();
        RedemptionDifficulty previousDifficulty = state.Difficulty;
        Operation previousOperation = state.CurrentOperation;
        ItemSet previousStarterItems = state.StarterItems;

        Assert.Throws<InvalidOperationException>(() =>
            state.ChangeDifficulty(context.RedemptionCatalog.GetDifficulty("super")));

        Assert.Same(previousDifficulty, state.Difficulty);
        Assert.Same(previousOperation, state.CurrentOperation);
        Assert.Same(previousStarterItems, state.StarterItems);
    }

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
        RedemptionSessionService session = await CreateMissionScenarioAsync(context);
        Operation operation = session.State.CurrentOperation.Clone();
        int stars = operation.MaxStars;

        MissionCompletionResult result = session.CompleteMission(stars);

        Assert.Equal(MissionCompletionResult.RewardsPending, result);
        Assert.Equal(Math.Max(1, stars - 1), session.State.PendingRewardItems.Count);
        Assert.Equal(operation.DifficultyIndex, session.State.CurrentOperation.DifficultyIndex);
        Assert.Equal(operation.MissionNumber, session.State.CurrentOperation.MissionNumber);
    }

    [Fact]
    public async Task CompletingMissionPreservesExistingRewardChoices()
    {
        using CatalogTestContext context = await CatalogTestContext.CreateAsync();
        RedemptionSessionService session = await CreateMissionScenarioAsync(context);
        GameItem reward = context.Catalog.GetAllItems().First();
        session.State.PendingRewardItems.AddItem(reward);

        Assert.Equal(MissionCompletionResult.RewardsPending, session.CompleteMission(0));
        Assert.Equal(reward, Assert.Single(session.State.PendingRewardItems));
    }

    [Theory]
    [InlineData("normal", 3, 2)]
    [InlineData("super", 5, 3)]
    public async Task CompletingMissionUsesDifficultyAdjustedRewardQuantity(
        string difficultyId,
        int stars,
        int expectedCount)
    {
        using CatalogTestContext context = await CatalogTestContext.CreateAsync();
        RedemptionSessionService session = await context.CreateSessionAsync();
        session.ChangeDifficulty(difficultyId);
        await session.BeginRunAsync();

        MissionCompletionResult result = session.CompleteMission(stars);

        Assert.Equal(MissionCompletionResult.RewardsPending, result);
        Assert.Equal(expectedCount, session.State.PendingRewardItems.Count);
    }

    [Fact]
    public async Task CompletingHardFinaleOffersOnlyAntitankRewards()
    {
        using CatalogTestContext context = await CatalogTestContext.CreateAsync();
        RedemptionSessionService session = await CreateMissionScenarioAsync(context);
        session.State.CurrentOperation = GetLastMission(context.RedemptionCatalog, HardFinaleOperationIndex);

        MissionCompletionResult result = session.CompleteMission(session.State.CurrentOperation.MaxStars);

        Assert.Equal(MissionCompletionResult.RewardsPending, result);
        Assert.NotEmpty(session.State.PendingRewardItems);
        Assert.All(session.State.PendingRewardItems, item => Assert.True(item.Antitank));
    }

    [Fact]
    public async Task CompletingMissionExcludesUnavailableItemsFromRewards()
    {
        using CatalogTestContext context = await CatalogTestContext.CreateAsync();
        RedemptionSessionService session = await context.CreateSessionAsync();
        Warbond mandatory = Assert.Single(
            context.RedemptionCatalog.GetAllWarbonds(), warbond => warbond.AlwaysEnabled);
        foreach (Warbond warbond in session.State.SelectedWarbonds.Where(warbond => !warbond.AlwaysEnabled).ToArray())
        {
            session.SetWarbondSelected(warbond, false);
        }

        await session.BeginRunAsync();
        ItemSet eligible = context.RedemptionCatalog.GetItemsByWarbonds([mandatory])
            .RemoveItems([session.State.StarterItems]);
        GameItem acquired = eligible.First();
        GameItem banned = eligible.First(item => !item.Equals(acquired));
        session.State.AcquiredItems.AddItem(acquired);
        session.State.BannedItems.AddItem(banned);

        MissionCompletionResult result = session.CompleteMission(session.State.CurrentOperation.MaxStars);

        Assert.Equal(MissionCompletionResult.RewardsPending, result);
        Assert.DoesNotContain(session.State.PendingRewardItems, session.State.StarterItems.ContainsItem);
        Assert.DoesNotContain(acquired, session.State.PendingRewardItems);
        Assert.DoesNotContain(banned, session.State.PendingRewardItems);
        Assert.All(session.State.PendingRewardItems,
            item => Assert.True(item.Warbond is null || session.State.SelectedWarbonds.Contains(item.Warbond)));
    }

    [Fact]
    public async Task CompletingFinalMissionCompletesRunWithoutOfferingRewards()
    {
        using CatalogTestContext context = await CatalogTestContext.CreateAsync();
        RedemptionSessionService session = await CreateMissionScenarioAsync(context);
        session.State.CurrentOperation = GetLastMission(
            context.RedemptionCatalog, context.RedemptionCatalog.LastOperationIndex);

        MissionCompletionResult result = session.CompleteMission(session.State.CurrentOperation.MaxStars);

        Assert.Equal(MissionCompletionResult.ChallengeCompleted, result);
        Assert.Equal(RunStatus.Completed, session.CurrentRunStatus);
        Assert.Empty(session.State.PendingRewardItems);
    }

    [Fact]
    public async Task CompletingMissionWithEmptyRewardPoolDoesNotAdvance()
    {
        using CatalogTestContext context = await CatalogTestContext.CreateAsync();
        RedemptionSessionService session = await CreateMissionScenarioAsync(context);
        session.State.AcquiredItems.AddItems(context.Catalog.GetAllItems());
        Operation operation = session.State.CurrentOperation.Clone();

        MissionCompletionResult result = session.CompleteMission(operation.MaxStars);

        Assert.Equal(MissionCompletionResult.NoRewardsAvailable, result);
        Assert.Empty(session.State.PendingRewardItems);
        Assert.Equal(operation.DifficultyIndex, session.State.CurrentOperation.DifficultyIndex);
        Assert.Equal(operation.MissionNumber, session.State.CurrentOperation.MissionNumber);
        Assert.Equal(RunStatus.InProgress, session.CurrentRunStatus);
    }

    [Theory]
    [InlineData(RunStatus.Completed)]
    [InlineData(RunStatus.Failed)]
    public async Task CompletingEndedRunIsUnavailable(RunStatus status)
    {
        using CatalogTestContext context = await CatalogTestContext.CreateAsync();
        RedemptionSessionService session = await CreateMissionScenarioAsync(context);
        session.SetRunStatus(status);

        Assert.Equal(
            MissionCompletionResult.Unavailable,
            session.CompleteMission(session.State.CurrentOperation.MaxStars));
        Assert.Empty(session.State.PendingRewardItems);
    }

    [Fact]
    public async Task CompletingMissionWithPendingPunishmentIsUnavailable()
    {
        using CatalogTestContext context = await CatalogTestContext.CreateAsync();
        RedemptionSessionService session = await CreateMissionScenarioAsync(context);
        session.State.PendingPunishmentItems.AddItem(context.Catalog.GetAllItems().First());

        Assert.Equal(
            MissionCompletionResult.Unavailable,
            session.CompleteMission(session.State.CurrentOperation.MaxStars));
        Assert.Empty(session.State.PendingRewardItems);
    }

    [Fact]
    public async Task ClaimingRewardAdvancesToNextMission()
    {
        using CatalogTestContext context = await CatalogTestContext.CreateAsync();
        RedemptionSessionService session = await CreateMissionScenarioAsync(context);
        GameItem reward = context.Catalog.GetAllItems()
            .First(item => !session.State.StarterItems.ContainsItem(item));
        session.State.PendingRewardItems.AddItem(reward);
        int missionNumber = session.State.CurrentOperation.MissionNumber;

        Assert.True(session.ClaimMissionReward(reward.Id));
        Assert.Contains(reward, session.State.AcquiredItems);
        Assert.Empty(session.State.PendingRewardItems);
        Assert.Equal(missionNumber + 1, session.State.CurrentOperation.MissionNumber);
    }

    [Fact]
    public async Task SkippingRewardsAdvancesToNextOperation()
    {
        using CatalogTestContext context = await CatalogTestContext.CreateAsync();
        RedemptionSessionService session = await CreateMissionScenarioAsync(context);
        GameItem reward = context.Catalog.GetAllItems()
            .First(item => !session.State.StarterItems.ContainsItem(item));
        session.State.PendingRewardItems.AddItem(reward);
        int operationIndex = session.State.CurrentOperation.DifficultyIndex;
        session.State.CurrentOperation = GetLastMission(context.RedemptionCatalog, operationIndex);

        Assert.True(session.SkipAndBanMissionRewards());
        Assert.Contains(reward, session.State.BannedItems);
        Assert.Empty(session.State.PendingRewardItems);
        Assert.Equal(operationIndex + 1, session.State.CurrentOperation.DifficultyIndex);
        Assert.Equal(1, session.State.CurrentOperation.MissionNumber);
    }

    [Fact]
    public async Task InvalidRewardSelectionDoesNotAdvanceOrClearChoices()
    {
        using CatalogTestContext context = await CatalogTestContext.CreateAsync();
        RedemptionSessionService session = await CreateMissionScenarioAsync(context);
        GameItem reward = context.Catalog.GetAllItems().First();
        session.State.PendingRewardItems.AddItem(reward);
        Operation operation = session.State.CurrentOperation.Clone();

        Assert.False(session.ClaimMissionReward("unknown-item"));
        Assert.Equal(reward, Assert.Single(session.State.PendingRewardItems));
        Assert.DoesNotContain(reward, session.State.AcquiredItems);
        Assert.Equal(operation.DifficultyIndex, session.State.CurrentOperation.DifficultyIndex);
        Assert.Equal(operation.MissionNumber, session.State.CurrentOperation.MissionNumber);
    }

    [Fact]
    public async Task SkippingWithoutRewardsDoesNotAdvance()
    {
        using CatalogTestContext context = await CatalogTestContext.CreateAsync();
        RedemptionSessionService session = await CreateMissionScenarioAsync(context);
        Operation operation = session.State.CurrentOperation.Clone();

        Assert.False(session.SkipAndBanMissionRewards());
        Assert.Equal(operation.DifficultyIndex, session.State.CurrentOperation.DifficultyIndex);
        Assert.Equal(operation.MissionNumber, session.State.CurrentOperation.MissionNumber);
    }

    [Fact]
    public async Task PreparingFailureOffersOnlyAcquiredNonStarterItems()
    {
        using CatalogTestContext context = await CatalogTestContext.CreateAsync();
        RedemptionSessionService session = await CreateMissionScenarioAsync(context);
        ItemSet acquired = new(context.Catalog.GetAllItems()
            .Where(item => !session.State.StarterItems.ContainsItem(item))
            .Take(MaximumPunishmentChoices + 2));
        session.State.AcquiredItems.AddItems(acquired);

        Assert.True(session.PrepareMissionFailure());
        Assert.Equal(MaximumPunishmentChoices, session.State.PendingPunishmentItems.Count);
        Assert.True(acquired.ContainsItems(session.State.PendingPunishmentItems));
        Assert.DoesNotContain(session.State.PendingPunishmentItems, session.State.StarterItems.ContainsItem);
        Assert.Equal(0, session.State.MissionsFailed);
    }

    [Fact]
    public async Task PreparingFailurePreservesExistingChoices()
    {
        using CatalogTestContext context = await CatalogTestContext.CreateAsync();
        RedemptionSessionService session = await CreateMissionScenarioAsync(context);
        GameItem punishment = context.Catalog.GetAllItems().First();
        session.State.PendingPunishmentItems.AddItem(punishment);

        Assert.False(session.PrepareMissionFailure());
        Assert.Equal(punishment, Assert.Single(session.State.PendingPunishmentItems));
    }

    [Fact]
    public async Task PreparingFailureWithoutAcquiredRewardsLeavesChoicesEmpty()
    {
        using CatalogTestContext context = await CatalogTestContext.CreateAsync();
        RedemptionSessionService session = await CreateMissionScenarioAsync(context);

        Assert.True(session.PrepareMissionFailure());
        Assert.Empty(session.State.PendingPunishmentItems);
        Assert.Equal(0, session.State.MissionsFailed);
    }

    [Theory]
    [InlineData(RunStatus.Completed)]
    [InlineData(RunStatus.Failed)]
    public async Task PreparingFailureForEndedRunIsUnavailable(RunStatus status)
    {
        using CatalogTestContext context = await CatalogTestContext.CreateAsync();
        RedemptionSessionService session = await CreateMissionScenarioAsync(context);
        session.State.AcquiredItems.AddItems(context.Catalog.GetAllItems());
        session.SetRunStatus(status);

        Assert.False(session.PrepareMissionFailure());
        Assert.Empty(session.State.PendingPunishmentItems);
    }

    [Fact]
    public async Task PreparingFailureWithPendingRewardsIsUnavailable()
    {
        using CatalogTestContext context = await CatalogTestContext.CreateAsync();
        RedemptionSessionService session = await CreateMissionScenarioAsync(context);
        session.State.PendingRewardItems.AddItem(context.Catalog.GetAllItems().First());

        Assert.False(session.PrepareMissionFailure());
        Assert.Empty(session.State.PendingPunishmentItems);
    }

    [Fact]
    public async Task ClaimingPunishmentAppliesFailureAndResetsMission()
    {
        using CatalogTestContext context = await CatalogTestContext.CreateAsync();
        RedemptionSessionService session = await CreateMissionScenarioAsync(context);
        GameItem punishment = context.Catalog.GetAllItems()
            .First(item => !session.State.StarterItems.ContainsItem(item));
        session.State.AcquiredItems.AddItem(punishment);
        session.State.BannedItems.AddItem(punishment);
        session.State.PendingPunishmentItems.AddItem(punishment);
        session.State.CurrentOperation.MissionNumber = 2;
        int operationIndex = session.State.CurrentOperation.DifficultyIndex;

        Assert.True(session.ClaimMissionPunishment(punishment.Id));
        Assert.Equal(1, session.State.MissionsFailed);
        Assert.Equal(1, session.State.CurrentOperation.MissionNumber);
        Assert.Equal(operationIndex, session.State.CurrentOperation.DifficultyIndex);
        Assert.DoesNotContain(punishment, session.State.AcquiredItems);
        Assert.DoesNotContain(punishment, session.State.BannedItems);
        Assert.Empty(session.State.PendingPunishmentItems);
    }

    [Fact]
    public async Task InvalidPunishmentSelectionPreservesProgressAndChoices()
    {
        using CatalogTestContext context = await CatalogTestContext.CreateAsync();
        RedemptionSessionService session = await CreateMissionScenarioAsync(context);
        GameItem punishment = context.Catalog.GetAllItems()
            .First(item => !session.State.StarterItems.ContainsItem(item));
        session.State.AcquiredItems.AddItem(punishment);
        session.State.PendingPunishmentItems.AddItem(punishment);
        session.State.CurrentOperation.MissionNumber = 2;

        Assert.False(session.ClaimMissionPunishment("unknown-item"));
        Assert.Equal(0, session.State.MissionsFailed);
        Assert.Equal(2, session.State.CurrentOperation.MissionNumber);
        Assert.Contains(punishment, session.State.AcquiredItems);
        Assert.Equal(punishment, Assert.Single(session.State.PendingPunishmentItems));
    }
}
