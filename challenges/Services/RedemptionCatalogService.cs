using System.Net.Http.Json;
using Hd2Challenges.Models;

namespace Hd2Challenges.Services;

public sealed class RedemptionCatalogService(HttpClient httpClient, CatalogService catalog)
{
    private bool _loaded;
    private CatalogService Catalog { get; } = catalog;
    private ItemSet StarterLoadout { get; set; } = new();

    public IReadOnlyList<RedemptionDifficulty> Difficulties { get; private set; } = [];

    public async Task EnsureLoadedAsync()
    {
        if (_loaded)
        {
            return;
        }

        await Catalog.EnsureLoadedAsync();

        var starterLoadoutDefinition = await httpClient.GetFromJsonAsync<RedemptionStarterLoadoutDefinition>("data/redemption-starter-loadout.json")
            ?? throw new InvalidOperationException("Required catalog data 'data/redemption-starter-loadout.json' deserialized to null.");
        StarterLoadout = Catalog.GetItemSet(starterLoadoutDefinition.GetItemNames());

        var difficultyDefinitions = await httpClient.GetFromJsonAsync<List<RedemptionDifficultyDefinition>>("data/redemption-difficulties.json")
            ?? throw new InvalidOperationException("Required catalog data 'data/redemption-difficulties.json' deserialized to null.");
        Difficulties = [.. difficultyDefinitions.Select(definition => new RedemptionDifficulty(definition, Catalog.GetItemSet, Catalog.GetOperation))];

        _loaded = true;
    }

    public RedemptionState CreateRedemptionState(RedemptionStateDefinition definition) => 
        new (definition, GetDifficulty, Catalog.GetOperation, Catalog.GetItemSet, GetStarterItems, Catalog.GetWarbonds);

    public RedemptionState CreateRedemptionSetupState(RedemptionDifficulty difficulty, IEnumerable<Warbond> selectedWarbonds) =>
        new(difficulty, selectedWarbonds, GetStarterItems);

    public RedemptionDifficulty GetDifficulty(string id) => Difficulties.First(option => option.Id == id);

    public ItemSet GetStarterItems(RedemptionDifficulty difficulty, IEnumerable<Warbond> selectedWarbonds) =>
        Catalog.FilterItemsByWarbonds(
            StarterLoadout.Clone()
                .ApplyOverrides(difficulty.LoadoutOverrides)
                .ApplyAdditions(difficulty.LoadoutAdditions),
            selectedWarbonds
        );
}
