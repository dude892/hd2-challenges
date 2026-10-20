using Hd2Challenges.Models;
using Hd2Challenges.Services;

namespace Hd2Challenges.Tests;

public sealed class RedemptionFlowTests
{
    [Fact]
    public async Task CatalogLoadsItemsWarbondsOperationsAndDifficultyReferences()
    {
        using CatalogTestContext context = await CatalogTestContext.CreateAsync();

        ItemSet allItems = context.Catalog.GetAllItems();
        HashSet<Warbond> warbonds = context.Catalog.GetAllWarbonds();

        Assert.NotEmpty(allItems);
        Assert.NotEmpty(warbonds);
        Assert.All(allItems, item => Assert.NotEqual(string.Empty, item.Id));
        Assert.All(allItems, item => Assert.True(Enum.IsDefined(item.Kind)));
        Assert.All(context.RedemptionCatalog.Difficulties, difficulty =>
        {
            Assert.InRange(difficulty.StartOperation.DifficultyIndex, 1, context.Catalog.LastOperationIndex);
            Assert.NotNull(difficulty.LoadoutOverrides);
            Assert.NotNull(difficulty.LoadoutAdditions);
        });
    }

    [Fact]
    public async Task CatalogResolvesIdsAndFiltersItemsByWarbond()
    {
        using CatalogTestContext context = await CatalogTestContext.CreateAsync();
        ItemSet allItems = context.Catalog.GetAllItems();
        Warbond mandatory = Assert.Single(context.Catalog.GetAllWarbonds(), warbond => warbond.AlwaysEnabled);
        Warbond optional = context.Catalog.GetAllWarbonds().First(warbond => !warbond.AlwaysEnabled);
        string[] selectedIds = allItems.Take(12).Select(item => item.Id).Append("unknown-item").ToArray();
        ItemSet resolved = context.Catalog.GetItemSet(selectedIds);
        HashSet<Warbond> resolvedWarbonds = context.Catalog.GetWarbonds([optional.Id]);
        ItemSet filtered = context.Catalog.GetItemsByWarbonds([mandatory]);
        ItemSet remaining = context.Catalog.FilterAllItems([resolved]);

        Assert.Equal(selectedIds.Length - 1, resolved.Count);
        Assert.Contains(optional, resolvedWarbonds);
        Assert.Contains(mandatory, resolvedWarbonds);
        Assert.All(filtered, item => Assert.True(item.Warbond is null || item.Warbond.Equals(mandatory)));
        Assert.DoesNotContain(remaining, item => resolved.ContainsItem(item));

        ItemSet clone = context.Catalog.GetAllItems();
        clone.Clear();
        Assert.NotEmpty(context.Catalog.GetAllItems());
    }

    [Fact]
    public async Task CatalogOrdersSelectedItemsByCategoryRatherThanInputOrder()
    {
        using CatalogTestContext context = await CatalogTestContext.CreateAsync();
        ItemSet allItems = context.Catalog.GetAllItems();
        GameItem[] selectedItems =
        [
            allItems.Boosters.First(),
            allItems.Passives.First(),
            allItems.Throwables.First(),
            allItems.Secondaries.First(),
            allItems.Primaries.First(),
            allItems.Stratagems.First()
        ];

        IReadOnlyList<GameItem> orderedItems = context.Catalog.GetItemsInCatalogOrder(selectedItems);

        Assert.Equal(
            [ItemKind.Stratagem, ItemKind.Primary, ItemKind.Secondary, ItemKind.Throwable, ItemKind.Passive, ItemKind.Booster],
            orderedItems.Select(item => item.Kind));
        Assert.Equal(selectedItems.Length, orderedItems.Count);
    }

    [Fact]
    public async Task SetupResolvesDifficultyLoadoutAndWarbondSelection()
    {
        using CatalogTestContext context = await CatalogTestContext.CreateAsync();
        HashSet<Warbond> allWarbonds = context.Catalog.GetAllWarbonds();
        RedemptionDifficulty normal = context.RedemptionCatalog.GetDifficulty("normal");
        RedemptionDifficulty super = context.RedemptionCatalog.GetDifficulty("super");
        RedemptionState normalState = context.RedemptionCatalog.CreateRedemptionSetupState(normal, allWarbonds);
        RedemptionState superState = context.RedemptionCatalog.CreateRedemptionSetupState(super, allWarbonds);

        Assert.Contains(normalState.StarterItems, item => item.Id == "p2peacemaker");
        Assert.Contains(superState.StarterItems.Passives, item => item.Id == "integratedexplosives");
        Assert.Contains(superState.StarterItems.Passives, item => item.Id == "extrapadding");
        Assert.Contains(superState.StarterItems.Stratagems, item => item.Id == "sh20ballisticshieldbackpack");

        Warbond alwaysEnabled = Assert.Single(allWarbonds, warbond => warbond.AlwaysEnabled);
        Warbond optional = allWarbonds.First(warbond => !warbond.AlwaysEnabled);
        RedemptionState selectionState = context.RedemptionCatalog.CreateRedemptionSetupState(normal, [alwaysEnabled]);
        Assert.False(selectionState.SetWarbondSelected(alwaysEnabled, false));
        Assert.True(selectionState.SelectedWarbonds.Contains(alwaysEnabled));
        Assert.True(selectionState.SetWarbondSelected(optional, true));
        Assert.False(selectionState.SetWarbondSelected(optional, true));
        Assert.True(selectionState.SetWarbondSelected(optional, false));
    }

    [Fact]
    public async Task StateDefinitionRoundTripsRuntimeIdsAndProgress()
    {
        using CatalogTestContext context = await CatalogTestContext.CreateAsync();
        RedemptionDifficulty difficulty = context.RedemptionCatalog.GetDifficulty("normal");
        RedemptionState state = context.RedemptionCatalog.CreateRedemptionSetupState(difficulty, context.Catalog.GetAllWarbonds());
        state.CurrentOperation = context.Catalog.GetOperation(6, 2);
        state.MissionsFailed = 2;
        state.RunCompleted = true;
        state.AcquiredItems.AddItems(state.StarterItems);
        state.PendingRewardItems.AddItem(context.Catalog.GetAllItems().First(item => !state.StarterItems.ContainsItem(item)));

        RedemptionStateDefinition definition = state.ToDefinition();
        RedemptionState restored = context.RedemptionCatalog.CreateRedemptionState(definition);

        Assert.Equal("normal", definition.DifficultyId);
        Assert.Equal(6, restored.CurrentOperation.DifficultyIndex);
        Assert.Equal(2, restored.CurrentOperation.MissionNumber);
        Assert.Equal(2, restored.MissionsFailed);
        Assert.True(restored.RunCompleted);
        Assert.True(restored.AcquiredItems.ContainsItems(state.StarterItems));
        Assert.Equal(state.PendingRewardItems.Select(item => item.Id), restored.PendingRewardItems.Select(item => item.Id));
        Assert.Equal(state.SelectedWarbonds.Select(item => item.Id).ToHashSet(), restored.SelectedWarbonds.Select(item => item.Id).ToHashSet());
    }

    [Fact]
    public void WarbondSelectionRecalculatesTheStarterLoadout()
    {
        Warbond mandatory = new() { Id = "mandatory", AlwaysEnabled = true };
        Warbond optional = new() { Id = "optional" };
        GameItem optionalStarter = new() { Id = "optional-starter" };
        optionalStarter.PostInitSetup(ItemKind.Primary, [optional]);
        RedemptionDifficulty difficulty = new(
            new RedemptionDifficultyDefinition { Id = "test", DisplayName = "Test" },
            _ => new ItemSet(),
            _ => new Operation(new OperationDefinition { DisplayName = "Start", MissionCount = 1 }, 1));
        RedemptionState state = new(difficulty, [mandatory], (_, selected) =>
        {
            ItemSet starterItems = new();
            if (selected.Contains(optional))
            {
                starterItems.AddItem(optionalStarter);
            }

            return starterItems;
        });

        Assert.DoesNotContain(optionalStarter, state.StarterItems);
        Assert.True(state.SetWarbondSelected(optional, true));
        Assert.Contains(optionalStarter, state.StarterItems);
        Assert.True(state.SetWarbondSelected(optional, false));
        Assert.DoesNotContain(optionalStarter, state.StarterItems);
    }

    [Fact]
    public async Task MissionServiceAdvancesOperationsAndResetsFailureProgress()
    {
        using CatalogTestContext context = await CatalogTestContext.CreateAsync();
        RedemptionState state = context.RedemptionCatalog.CreateRedemptionSetupState(
            context.RedemptionCatalog.GetDifficulty("normal"), context.Catalog.GetAllWarbonds());
        RedemptionMissionService missions = new(context.Catalog);
        int firstOperation = state.CurrentOperation.DifficultyIndex;

        state.CurrentOperation.SelectedStars = 3;
        missions.AdvanceMission(state);
        Assert.Equal(2, state.CurrentOperation.MissionNumber);
        Assert.Equal(0, state.CurrentOperation.SelectedStars);

        missions.AdvanceMission(state);
        Assert.Equal(firstOperation + 1, state.CurrentOperation.DifficultyIndex);
        Assert.Equal(1, state.CurrentOperation.MissionNumber);

        state.CurrentOperation = context.Catalog.GetOperation(firstOperation + 2, 2);
        state.CurrentOperation.SelectedStars = 2;
        missions.ApplyMissionFailure(state);
        Assert.Equal(1, state.MissionsFailed);
        Assert.Equal(1, state.CurrentOperation.MissionNumber);
        Assert.Equal(0, state.CurrentOperation.SelectedStars);
    }

    [Fact]
    public async Task MissionServiceOnlyCompletesOnFinalMissionOfFinalOperation()
    {
        using CatalogTestContext context = await CatalogTestContext.CreateAsync();
        RedemptionState state = context.RedemptionCatalog.CreateRedemptionSetupState(
            context.RedemptionCatalog.GetDifficulty("normal"), context.Catalog.GetAllWarbonds());
        RedemptionMissionService missions = new(context.Catalog);

        Assert.False(missions.IsFinalMission(state));
        Assert.False(missions.TryCompleteFinalMission(state));
        Assert.False(state.RunCompleted);

        int finalOperation = context.Catalog.LastOperationIndex;
        int finalMission = context.Catalog.GetOperation(finalOperation).MissionCount;
        state.CurrentOperation = context.Catalog.GetOperation(finalOperation, finalMission);

        Assert.True(missions.IsFinalMission(state));
        Assert.True(missions.TryCompleteFinalMission(state));
        Assert.True(state.RunCompleted);
    }

    [Fact]
    public async Task MissionServiceDoesNotAdvancePastTheFinalOperation()
    {
        using CatalogTestContext context = await CatalogTestContext.CreateAsync();
        RedemptionState state = context.RedemptionCatalog.CreateRedemptionSetupState(
            context.RedemptionCatalog.GetDifficulty("normal"), context.Catalog.GetAllWarbonds());
        RedemptionMissionService missions = new(context.Catalog);
        int finalOperation = context.Catalog.LastOperationIndex;
        int finalMission = context.Catalog.GetOperation(finalOperation).MissionCount;
        state.CurrentOperation = context.Catalog.GetOperation(finalOperation, finalMission);

        missions.AdvanceMission(state);

        Assert.Equal(finalOperation, state.CurrentOperation.DifficultyIndex);
        Assert.Equal(finalMission, state.CurrentOperation.MissionNumber);
    }

    [Fact]
    public async Task RewardPoolExcludesStarterAcquiredBannedAndUnselectedItems()
    {
        using CatalogTestContext context = await CatalogTestContext.CreateAsync();
        Warbond mandatory = Assert.Single(context.Catalog.GetAllWarbonds(), warbond => warbond.AlwaysEnabled);
        RedemptionState state = context.RedemptionCatalog.CreateRedemptionSetupState(
            context.RedemptionCatalog.GetDifficulty("normal"), [mandatory]);
        RedemptionRewardService rewards = new(context.Catalog);
        GameItem acquired = context.Catalog.GetAllItems().First(item => !state.StarterItems.ContainsItem(item));
        GameItem banned = context.Catalog.GetAllItems().First(item =>
            !state.StarterItems.ContainsItem(item) && !item.Equals(acquired));
        state.AcquiredItems.AddItem(acquired);
        state.BannedItems.AddItem(banned);

        ItemSet pool = rewards.GetRewardPool(state);

        Assert.NotEmpty(pool);
        Assert.DoesNotContain(pool, item => state.StarterItems.ContainsItem(item));
        Assert.DoesNotContain(pool, item => item.Equals(acquired));
        Assert.DoesNotContain(pool, item => item.Equals(banned));
        Assert.DoesNotContain(pool, item => item.Warbond is not null && !item.Warbond.AlwaysEnabled && !state.SelectedWarbonds.Contains(item.Warbond));
    }

    [Theory]
    [InlineData("normal", 3, 2)]
    [InlineData("super", 5, 3)]
    public async Task RewardRollHonorsStarCountAndSuperDifficulty(string difficultyId, int stars, int expectedMaximum)
    {
        using CatalogTestContext context = await CatalogTestContext.CreateAsync();
        RedemptionState state = context.RedemptionCatalog.CreateRedemptionSetupState(
            context.RedemptionCatalog.GetDifficulty(difficultyId), context.Catalog.GetAllWarbonds());
        RedemptionRewardService rewards = new(context.Catalog);

        ItemSet rolled = rewards.RollRewardItems(state, stars);

        Assert.InRange(rolled.Count, 1, expectedMaximum);
        Assert.True(rewards.GetRewardPool(state).ContainsItems(rolled));
    }

    [Fact]
    public async Task HardFinaleRewardRollOnlyOffersAntitankItems()
    {
        using CatalogTestContext context = await CatalogTestContext.CreateAsync();
        RedemptionState state = context.RedemptionCatalog.CreateRedemptionSetupState(
            context.RedemptionCatalog.GetDifficulty("normal"), context.Catalog.GetAllWarbonds());
        state.CurrentOperation = context.Catalog.GetOperation(5, context.Catalog.GetOperation(5).MissionCount);
        RedemptionRewardService rewards = new(context.Catalog);
        ItemSet antitankPool = new(rewards.GetRewardPool(state).Where(item => item.Antitank));

        ItemSet rolled = rewards.RollRewardItems(state, state.CurrentOperation.MaxStars);

        Assert.NotEmpty(antitankPool);
        Assert.NotEmpty(rolled);
        Assert.All(rolled, item => Assert.True(item.Antitank));
        Assert.True(antitankPool.ContainsItems(rolled));
    }

    [Fact]
    public async Task PendingRewardCanBeClaimedOrBannedAndCannotBeDuplicated()
    {
        using CatalogTestContext context = await CatalogTestContext.CreateAsync();
        RedemptionState state = context.RedemptionCatalog.CreateRedemptionSetupState(
            context.RedemptionCatalog.GetDifficulty("normal"), context.Catalog.GetAllWarbonds());
        RedemptionRewardService rewards = new(context.Catalog);
        GameItem reward = rewards.GetRewardPool(state).First();
        state.PendingRewardItems.AddItem(reward);

        Assert.False(rewards.EnsurePendingRewards(state, 3));
        Assert.False(rewards.ClaimReward(state, "not-a-reward"));
        Assert.True(rewards.ClaimReward(state, reward.Id));
        Assert.Contains(reward, state.AcquiredItems);
        Assert.Empty(state.PendingRewardItems);

        state.PendingRewardItems.AddItem(reward);
        Assert.True(rewards.BanPendingRewards(state));
        Assert.Contains(reward, state.BannedItems);
        Assert.Empty(state.PendingRewardItems);
        Assert.False(rewards.BanPendingRewards(state));
    }

    [Fact]
    public async Task PendingChoicesInitializeOnceAndHandleAnEmptyRewardPool()
    {
        using CatalogTestContext context = await CatalogTestContext.CreateAsync();
        RedemptionState state = context.RedemptionCatalog.CreateRedemptionSetupState(
            context.RedemptionCatalog.GetDifficulty("normal"), context.Catalog.GetAllWarbonds());
        RedemptionRewardService rewards = new(context.Catalog);

        Assert.True(rewards.EnsurePendingRewards(state, state.CurrentOperation.MaxStars));
        Assert.NotEmpty(state.PendingRewardItems);
        int pendingCount = state.PendingRewardItems.Count;
        Assert.False(rewards.EnsurePendingRewards(state, 1));
        Assert.Equal(pendingCount, state.PendingRewardItems.Count);

        state.PendingRewardItems.Clear();
        state.AcquiredItems.AddItems(context.Catalog.GetAllItems());
        Assert.True(rewards.EnsurePendingRewards(state, 1));
        Assert.Empty(state.PendingRewardItems);

        RedemptionState noAcquiredItems = context.RedemptionCatalog.CreateRedemptionSetupState(
            context.RedemptionCatalog.GetDifficulty("normal"), context.Catalog.GetAllWarbonds());
        Assert.True(rewards.EnsurePendingPunishments(noAcquiredItems));
        Assert.Empty(noAcquiredItems.PendingPunishmentItems);
    }

    [Fact]
    public async Task PunishmentSelectionIsBoundedAndClaimRemovesTheItem()
    {
        using CatalogTestContext context = await CatalogTestContext.CreateAsync();
        RedemptionState state = context.RedemptionCatalog.CreateRedemptionSetupState(
            context.RedemptionCatalog.GetDifficulty("normal"), context.Catalog.GetAllWarbonds());
        RedemptionRewardService rewards = new(context.Catalog);
        ItemSet acquired = new(context.Catalog.GetAllItems().Take(5));
        state.AcquiredItems.AddItems(acquired);

        Assert.True(rewards.EnsurePendingPunishments(state));
        Assert.InRange(state.PendingPunishmentItems.Count, 1, 3);
        Assert.True(state.AcquiredItems.ContainsItems(state.PendingPunishmentItems));
        Assert.False(rewards.EnsurePendingPunishments(state));

        GameItem punishment = state.PendingPunishmentItems.First();
        state.BannedItems.AddItem(punishment);
        Assert.False(rewards.ClaimPunishment(state, "not-a-punishment"));
        Assert.True(rewards.ClaimPunishment(state, punishment.Id));
        Assert.DoesNotContain(punishment, state.AcquiredItems);
        Assert.DoesNotContain(punishment, state.BannedItems);
        Assert.Empty(state.PendingPunishmentItems);
    }

    [Fact]
    public async Task MissionActionAvailabilityTracksPendingChoicesAndCompletion()
    {
        using CatalogTestContext context = await CatalogTestContext.CreateAsync();
        RedemptionState state = context.RedemptionCatalog.CreateRedemptionSetupState(
            context.RedemptionCatalog.GetDifficulty("normal"), context.Catalog.GetAllWarbonds());
        GameItem item = context.Catalog.GetAllItems().First();

        Assert.True(state.CanCompleteMission);
        Assert.True(state.CanFailMission);
        state.PendingRewardItems.AddItem(item);
        Assert.True(state.CanCompleteMission);
        Assert.False(state.CanFailMission);
        state.PendingRewardItems.Clear();
        state.PendingPunishmentItems.AddItem(item);
        Assert.False(state.CanCompleteMission);
        Assert.True(state.CanFailMission);
        state.PendingPunishmentItems.Clear();
        state.RunCompleted = true;
        Assert.False(state.CanCompleteMission);
        Assert.False(state.CanFailMission);
    }

    [Fact]
    public async Task SummaryReportsCurrentAndFinalScoreAndSelectedWarbonds()
    {
        using CatalogTestContext context = await CatalogTestContext.CreateAsync();
        RedemptionDifficulty difficulty = context.RedemptionCatalog.GetDifficulty("normal");
        Warbond mandatory = Assert.Single(context.Catalog.GetAllWarbonds(), warbond => warbond.AlwaysEnabled);
        RedemptionState state = context.RedemptionCatalog.CreateRedemptionSetupState(difficulty, [mandatory]);
        state.MissionsFailed = 2;

        Assert.Contains($"Current Score: {difficulty.ScoreModifier - 100}", state.SummaryText);
        Assert.Contains(mandatory.DisplayName, state.SummaryText);

        state.RunCompleted = true;
        Assert.Contains($"Final Score: {difficulty.ScoreModifier - 100}", state.SummaryText);
        Assert.Contains("Run Completed", state.SummaryText);
    }
}