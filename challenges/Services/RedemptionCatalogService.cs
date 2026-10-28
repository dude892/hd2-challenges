using System.Net.Http.Json;
using Hd2Challenges.Models;

namespace Hd2Challenges.Services;

public sealed class RedemptionCatalogService(HttpClient httpClient, CatalogService catalog)
{
    private bool _loaded;
    private ItemSet StarterLoadout { get; set; } = new();

    public IReadOnlyList<RedemptionDifficulty> Difficulties { get; private set; } = [];

    public int LastOperationIndex => catalog.LastOperationIndex;

    public HashSet<Warbond> GetAllWarbonds() => catalog.GetAllWarbonds();

    public IReadOnlyList<GameItem> GetItemsInCatalogOrder(IEnumerable<GameItem> items) => catalog.GetItemsInCatalogOrder(items);

    public Operation GetOperation(int index) => catalog.GetOperation(index);

    public ItemSet GetItemsByWarbonds(IEnumerable<Warbond> selectedWarbonds) => catalog.GetItemsByWarbonds(selectedWarbonds);

    public async Task EnsureLoadedAsync()
    {
        if (_loaded)
        {
            return;
        }

        await catalog.EnsureLoadedAsync();

        var starterLoadoutDefinition = await httpClient.GetFromJsonAsync<RedemptionStarterLoadoutDefinition>("data/redemption-starter-loadout.json")
            ?? throw new InvalidOperationException("Required catalog data 'data/redemption-starter-loadout.json' deserialized to null.");
        StarterLoadout = catalog.GetItemSet(starterLoadoutDefinition.GetItemNames());

        var difficultyDefinitions = await httpClient.GetFromJsonAsync<List<RedemptionDifficultyDefinition>>("data/redemption-difficulties.json")
            ?? throw new InvalidOperationException("Required catalog data 'data/redemption-difficulties.json' deserialized to null.");
        Difficulties = [.. difficultyDefinitions.Select(definition => new RedemptionDifficulty(definition, catalog.GetItemSet, catalog.GetOperation))];

        _loaded = true;
    }

    public RedemptionState CreateRedemptionState(RedemptionStateDefinition definition) => 
        new (definition, GetDifficulty, catalog.GetOperation, catalog.GetItemSet, GetStarterItems, catalog.GetWarbonds);

    public RedemptionState CreateRedemptionSetupState(RedemptionDifficulty difficulty, IEnumerable<Warbond> selectedWarbonds) =>
        new(difficulty, selectedWarbonds, GetStarterItems);

    public RedemptionDifficulty GetDifficulty(string id) => Difficulties.First(option => option.Id == id);

    public ItemSet GetStarterItems(RedemptionDifficulty difficulty, IEnumerable<Warbond> selectedWarbonds) =>
        catalog.FilterItemsByWarbonds(
            StarterLoadout.Clone()
                .ApplyOverrides(difficulty.LoadoutOverrides)
                .ApplyAdditions(difficulty.LoadoutAdditions),
            selectedWarbonds
        );
}
