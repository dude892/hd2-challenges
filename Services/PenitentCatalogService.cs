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

    public PenitentDifficulty GetDifficulty(string id) => Difficulties.First(option => option.Id == id);

    public ItemSet GetStarterItems(string difficultyId)
    {
        ItemSet starterSet = StarterLoadout.Clone();
        PenitentDifficulty difficulty = GetDifficulty(difficultyId);

        starterSet.ApplyOverrides(difficulty.LoadoutOverrides).ApplyAdditions(difficulty.LoadoutAdditions);

        return starterSet;
    }

    private ItemSet ResolveLoadout(PenitentStarterLoadoutDefinition definition) => new()
    {
        Stratagems = Catalog.GetItemSet(definition.Stratagems).Stratagems,
        Primaries = Catalog.GetItemSet(definition.Primaries).Primaries,
        Secondaries = Catalog.GetItemSet(definition.Secondaries).Secondaries,
        Throwables = Catalog.GetItemSet(definition.Throwables).Throwables,
        ArmorPassives = Catalog.GetItemSet(definition.ArmorPassives).ArmorPassives,
        Boosters = Catalog.GetItemSet(definition.Boosters).Boosters
    };
}
