using System.Data;
using System.Net.Http.Json;
using Hd2Challenges.Models;

namespace Hd2Challenges.Services;

public sealed class PenitentCatalogService(HttpClient httpClient, CatalogService catalog)
{
    private bool _loaded;
    private CatalogService Catalog { get; } = catalog;

    public IReadOnlyList<PenitentDifficulty> Difficulties { get; private set; } = [];
    public ItemSet StarterLoadout { get; private set; } = new();

    public async Task EnsureLoadedAsync()
    {
        if (_loaded)
        {
            return;
        }

        await Catalog.EnsureLoadedAsync();

        StarterLoadout = ResolveLoadout(
            await httpClient.GetFromJsonAsync<PenitentStarterLoadoutDefinition>("data/penitent-starter-loadout.json") ?? new());

        var difficultyDefinitions = await httpClient.GetFromJsonAsync<List<PenitentDifficultyDefinition>>("data/penitent-difficulties.json") ?? [];
        Difficulties = [.. difficultyDefinitions.Select(definition => new PenitentDifficulty(definition, Catalog.GetItemSet, Catalog.GetOperation))];

        _loaded = true;
    }

    public PenitentState CreatePenitentState(PenitentStateDefinition definition) => 
        new (definition, GetDifficulty, Catalog.GetOperation, Catalog.GetItemSet, GetStarterItems, Catalog.GetWarbonds);

    public PenitentState CreatePenitentState(string difficulty = "normal") =>
        CreatePenitentState(new PenitentStateDefinition { DifficultyId = difficulty });

    public PenitentState CreatePenitentState(string difficulty, IEnumerable<Warbond> selectedWarbonds) =>
        CreatePenitentState(new PenitentStateDefinition { DifficultyId = difficulty, SelectedWarbonds = [.. selectedWarbonds.Select(item => item.Id)] });

    public PenitentState CreatePenitentSetupState(string difficulty, IEnumerable<Warbond> selectedWarbonds)
    {
        PenitentState state = CreatePenitentState(difficulty, selectedWarbonds);
        state.RunStarted = false;
        return state;
    }

    public PenitentDifficulty GetDifficulty(string id) => Difficulties.First(option => option.Id == id);

    public ItemSet GetStarterItems(string difficultyId, IEnumerable<Warbond> selectedWarbonds)
    {
        ItemSet starterSet = StarterLoadout.Clone();
        PenitentDifficulty difficulty = GetDifficulty(difficultyId);

        starterSet.ApplyOverrides(difficulty.LoadoutOverrides).ApplyAdditions(difficulty.LoadoutAdditions);

        return Catalog.FilterItemsByWarbonds(starterSet, selectedWarbonds);
    }

    private ItemSet ResolveLoadout(PenitentStarterLoadoutDefinition definition) => new()
    {
        Stratagems = Catalog.GetItemSet(definition.Stratagems).Stratagems,
        Primaries = Catalog.GetItemSet(definition.Primaries).Primaries,
        Secondaries = Catalog.GetItemSet(definition.Secondaries).Secondaries,
        Throwables = Catalog.GetItemSet(definition.Throwables).Throwables,
        Passives = Catalog.GetItemSet(definition.ArmorPassives).Passives,
        Boosters = Catalog.GetItemSet(definition.Boosters).Boosters
    };
}
