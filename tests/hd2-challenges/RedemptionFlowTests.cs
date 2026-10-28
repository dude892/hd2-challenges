using Hd2Challenges.Models;
using Hd2Challenges.Services;

namespace Hd2Challenges.Tests;

public sealed class RedemptionFlowTests
{
    private static (RedemptionState State, Warbond Optional, GameItem OptionalStarter) CreateWarbondSelectionScenario()
    {
        Warbond mandatory = new() { Id = "mandatory", AlwaysEnabled = true };
        Warbond optional = new() { Id = "optional" };
        GameItem optionalStarter = new(new GameItemDefinition { Id = "optional-starter" }, ItemKind.Primary, null);
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

        return (state, optional, optionalStarter);
    }

    [Fact]
    public async Task CatalogLoadsItemsWithValidIdsAndKinds()
    {
        using CatalogTestContext context = await CatalogTestContext.CreateAsync();

        ItemSet allItems = context.Catalog.GetAllItems();

        Assert.NotEmpty(allItems);
        Assert.All(allItems, item => Assert.NotEqual(string.Empty, item.Id));
        Assert.All(allItems, item => Assert.True(Enum.IsDefined(item.Kind)));
    }

    [Fact]
    public async Task CatalogLoadsWarbonds()
    {
        using CatalogTestContext context = await CatalogTestContext.CreateAsync();

        Assert.NotEmpty(context.Catalog.GetAllWarbonds());
    }

    [Fact]
    public async Task RedemptionDifficultiesReferenceValidOperationsAndLoadouts()
    {
        using CatalogTestContext context = await CatalogTestContext.CreateAsync();

        Assert.All(context.RedemptionCatalog.Difficulties, difficulty =>
        {
            Assert.InRange(difficulty.StartOperation.DifficultyIndex, 1, context.Catalog.LastOperationIndex);
            Assert.NotNull(difficulty.LoadoutOverrides);
            Assert.NotNull(difficulty.LoadoutAdditions);
        });
    }

    [Fact]
    public async Task CatalogResolvesKnownItemIdsAndIgnoresUnknownIds()
    {
        using CatalogTestContext context = await CatalogTestContext.CreateAsync();
        ItemSet allItems = context.Catalog.GetAllItems();
        string[] selectedIds = allItems.Take(12).Select(item => item.Id).Append("unknown-item").ToArray();
        ItemSet resolved = context.Catalog.GetItemSet(selectedIds);

        Assert.Equal(selectedIds.Length - 1, resolved.Count);
    }

    [Fact]
    public async Task CatalogResolvingOptionalWarbondIdsIncludesMandatoryWarbond()
    {
        using CatalogTestContext context = await CatalogTestContext.CreateAsync();
        Warbond mandatory = Assert.Single(context.Catalog.GetAllWarbonds(), warbond => warbond.AlwaysEnabled);
        Warbond optional = context.Catalog.GetAllWarbonds().First(warbond => !warbond.AlwaysEnabled);

        HashSet<Warbond> resolvedWarbonds = context.Catalog.GetWarbonds([optional.Id]);

        Assert.Contains(optional, resolvedWarbonds);
        Assert.Contains(mandatory, resolvedWarbonds);
    }

    [Fact]
    public async Task CatalogFiltersItemsBySelectedWarbond()
    {
        using CatalogTestContext context = await CatalogTestContext.CreateAsync();
        Warbond mandatory = Assert.Single(context.Catalog.GetAllWarbonds(), warbond => warbond.AlwaysEnabled);

        ItemSet filtered = context.Catalog.GetItemsByWarbonds([mandatory]);

        Assert.All(filtered, item => Assert.True(item.Warbond is null || item.Warbond.Equals(mandatory)));
    }

    [Fact]
    public async Task CatalogFilterAllItemsExcludesProvidedItems()
    {
        using CatalogTestContext context = await CatalogTestContext.CreateAsync();
        ItemSet resolved = new(context.Catalog.GetAllItems().Take(12));

        ItemSet remaining = context.Catalog.FilterAllItems([resolved]);

        Assert.DoesNotContain(remaining, resolved.ContainsItem);
    }

    [Fact]
    public async Task CatalogReturnsAnIsolatedCopyOfAllItems()
    {
        using CatalogTestContext context = await CatalogTestContext.CreateAsync();
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
    public async Task SetupResolvesNormalDifficultyStarterLoadout()
    {
        using CatalogTestContext context = await CatalogTestContext.CreateAsync();
        HashSet<Warbond> allWarbonds = context.Catalog.GetAllWarbonds();
        RedemptionDifficulty normal = context.RedemptionCatalog.GetDifficulty("normal");
        RedemptionState normalState = context.RedemptionCatalog.CreateRedemptionSetupState(normal, allWarbonds);

        Assert.Contains(normalState.StarterItems, item => item.Id == "p2peacemaker");
    }

    [Fact]
    public async Task SetupResolvesSuperDifficultyLoadoutOverrides()
    {
        using CatalogTestContext context = await CatalogTestContext.CreateAsync();
        RedemptionDifficulty super = context.RedemptionCatalog.GetDifficulty("super");
        RedemptionState superState = context.RedemptionCatalog.CreateRedemptionSetupState(
            super, context.Catalog.GetAllWarbonds());

        Assert.Contains(superState.StarterItems.Passives, item => item.Id == "integratedexplosives");
        Assert.Contains(superState.StarterItems.Passives, item => item.Id == "extrapadding");
        Assert.Contains(superState.StarterItems.Stratagems, item => item.Id == "sh20ballisticshieldbackpack");
    }

    [Fact]
    public async Task SetupDoesNotAllowDeselectingAlwaysEnabledWarbond()
    {
        using CatalogTestContext context = await CatalogTestContext.CreateAsync();
        Warbond alwaysEnabled = Assert.Single(
            context.Catalog.GetAllWarbonds(), warbond => warbond.AlwaysEnabled);
        RedemptionState selectionState = context.RedemptionCatalog.CreateRedemptionSetupState(
            context.RedemptionCatalog.GetDifficulty("normal"), [alwaysEnabled]);

        Assert.False(selectionState.SetWarbondSelected(alwaysEnabled, false));
        Assert.True(selectionState.SelectedWarbonds.Contains(alwaysEnabled));
    }

    [Fact]
    public async Task SetupAllowsSelectingOptionalWarbond()
    {
        using CatalogTestContext context = await CatalogTestContext.CreateAsync();
        Warbond alwaysEnabled = Assert.Single(
            context.Catalog.GetAllWarbonds(), warbond => warbond.AlwaysEnabled);
        Warbond optional = context.Catalog.GetAllWarbonds().First(warbond => !warbond.AlwaysEnabled);
        RedemptionState selectionState = context.RedemptionCatalog.CreateRedemptionSetupState(
            context.RedemptionCatalog.GetDifficulty("normal"), [alwaysEnabled]);

        Assert.True(selectionState.SetWarbondSelected(optional, true));
        Assert.False(selectionState.SetWarbondSelected(optional, true));
        Assert.Contains(optional, selectionState.SelectedWarbonds);
    }

    [Fact]
    public async Task SetupAllowsDeselectingOptionalWarbond()
    {
        using CatalogTestContext context = await CatalogTestContext.CreateAsync();
        Warbond optional = context.Catalog.GetAllWarbonds().First(warbond => !warbond.AlwaysEnabled);
        RedemptionState selectionState = context.RedemptionCatalog.CreateRedemptionSetupState(
            context.RedemptionCatalog.GetDifficulty("normal"), context.Catalog.GetAllWarbonds());

        Assert.True(selectionState.SetWarbondSelected(optional, false));
        Assert.DoesNotContain(optional, selectionState.SelectedWarbonds);
    }

    [Fact]
    public async Task StateDefinitionRoundTripsRuntimeIdsAndProgress()
    {
        using CatalogTestContext context = await CatalogTestContext.CreateAsync();
        RedemptionDifficulty difficulty = context.RedemptionCatalog.GetDifficulty("normal");
        RedemptionState state = context.RedemptionCatalog.CreateRedemptionSetupState(difficulty, context.Catalog.GetAllWarbonds());
        state.CurrentOperation = context.Catalog.GetOperation(6, 2);
        state.MissionsFailed = 2;
        state.AcquiredItems.AddItems(state.StarterItems);
        state.PendingRewardItems.AddItem(context.Catalog.GetAllItems().First(item => !state.StarterItems.ContainsItem(item)));

        RedemptionStateDefinition definition = state.ToDefinition();
        RedemptionState restored = context.RedemptionCatalog.CreateRedemptionState(definition);

        Assert.Equal("normal", definition.DifficultyId);
        Assert.Equal(6, restored.CurrentOperation.DifficultyIndex);
        Assert.Equal(2, restored.CurrentOperation.MissionNumber);
        Assert.Equal(2, restored.MissionsFailed);
        Assert.True(restored.RunStarted);
        Assert.True(restored.AcquiredItems.ContainsItems(state.StarterItems));
        Assert.Equal(state.PendingRewardItems.Select(item => item.Id), restored.PendingRewardItems.Select(item => item.Id));
        Assert.Equal(state.SelectedWarbonds.Select(item => item.Id).ToHashSet(), restored.SelectedWarbonds.Select(item => item.Id).ToHashSet());
        Assert.All(state.AcquiredItems.Categories, category =>
        {
            ItemCategory restoredCategory = restored.AcquiredItems.GetCategory(category.Kind);
            Assert.Equal(category.Label, restoredCategory.Label);
            Assert.Equal(category.ToHashSet(), restoredCategory.ToHashSet());
        });
    }

    [Fact]
    public void SelectingWarbondAddsItsStarterItems()
    {
        (RedemptionState state, Warbond optional, GameItem optionalStarter) = CreateWarbondSelectionScenario();

        Assert.DoesNotContain(optionalStarter, state.StarterItems);
        Assert.True(state.SetWarbondSelected(optional, true));
        Assert.Contains(optionalStarter, state.StarterItems);
    }

    [Fact]
    public void DeselectingWarbondRemovesItsStarterItems()
    {
        (RedemptionState state, Warbond optional, GameItem optionalStarter) = CreateWarbondSelectionScenario();
        state.SetWarbondSelected(optional, true);

        Assert.True(state.SetWarbondSelected(optional, false));
        Assert.DoesNotContain(optionalStarter, state.StarterItems);
    }

}