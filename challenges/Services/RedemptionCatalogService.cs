using System.Data;
using System.Net.Http.Json;
using Hd2Challenges.Models;

namespace Hd2Challenges.Services;

public sealed class RedemptionCatalogService(HttpClient httpClient, CatalogService catalog)
{
    private bool _loaded;
    private CatalogService Catalog { get; } = catalog;

    public IReadOnlyList<RedemptionDifficulty> Difficulties { get; private set; } = [];
    public ItemSet StarterLoadout { get; private set; } = new();

    public async Task EnsureLoadedAsync()
    {
        if (_loaded)
        {
            return;
        }

        await Catalog.EnsureLoadedAsync();

        StarterLoadout = ResolveLoadout(await httpClient.GetFromJsonAsync<RedemptionStarterLoadoutDefinition>("data/redemption-starter-loadout.json") ?? new());

        var difficultyDefinitions = await httpClient.GetFromJsonAsync<List<RedemptionDifficultyDefinition>>("data/redemption-difficulties.json") ?? [];
        Difficulties = [.. difficultyDefinitions.Select(definition => new RedemptionDifficulty(definition, Catalog.GetItemSet, Catalog.GetOperation))];

        _loaded = true;
    }

    public RedemptionState CreateRedemptionState(RedemptionStateDefinition definition) => 
        new (definition, GetDifficulty, Catalog.GetOperation, Catalog.GetItemSet, GetStarterItems, Catalog.GetWarbonds);

    public RedemptionState CreateRedemptionState(string difficulty = "normal") =>
        CreateRedemptionState(new RedemptionStateDefinition { DifficultyId = difficulty });

    public RedemptionState CreateRedemptionState(string difficulty, IEnumerable<Warbond> selectedWarbonds) =>
        CreateRedemptionState(new RedemptionStateDefinition { DifficultyId = difficulty, SelectedWarbonds = [.. selectedWarbonds.Select(item => item.Id)] });

    public RedemptionState CreateRedemptionSetupState(string difficulty, IEnumerable<Warbond> selectedWarbonds)
    {
        RedemptionState state = CreateRedemptionState(difficulty, selectedWarbonds);
        state.RunStarted = false;
        return state;
    }

    public RedemptionDifficulty GetDifficulty(string id) => Difficulties.First(option => option.Id == id);

    public ItemSet GetStarterItems(string difficultyId, IEnumerable<Warbond> selectedWarbonds)
    {
        ItemSet starterSet = StarterLoadout.Clone();
        RedemptionDifficulty difficulty = GetDifficulty(difficultyId);

        starterSet.ApplyOverrides(difficulty.LoadoutOverrides).ApplyAdditions(difficulty.LoadoutAdditions);

        return Catalog.FilterItemsByWarbonds(starterSet, selectedWarbonds);
    }

    private ItemSet ResolveLoadout(RedemptionStarterLoadoutDefinition definition) => new()
    {
        Stratagems = Catalog.GetItemSet(definition.Stratagems).Stratagems,
        Primaries = Catalog.GetItemSet(definition.Primaries).Primaries,
        Secondaries = Catalog.GetItemSet(definition.Secondaries).Secondaries,
        Throwables = Catalog.GetItemSet(definition.Throwables).Throwables,
        Passives = Catalog.GetItemSet(definition.ArmorPassives).Passives,
        Boosters = Catalog.GetItemSet(definition.Boosters).Boosters
    };
}
