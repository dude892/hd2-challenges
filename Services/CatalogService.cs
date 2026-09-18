using System.Net.Http.Json;
using Hd2Challenges.Models;

namespace Hd2Challenges.Services;

public sealed class CatalogService(HttpClient httpClient)
{
    private bool _loaded;

    private IReadOnlyList<GameItem> Primaries { get; set; } = [];
    private IReadOnlyList<GameItem> Secondaries { get; set; } = [];
    private IReadOnlyList<GameItem> Throwables { get; set; } = [];
    private IReadOnlyList<GameItem> Boosters { get; set; } = [];
    private IReadOnlyList<GameItem> Stratagems { get; set; } = [];
    private IReadOnlyList<GameItem> ArmorPassives { get; set; } = [];
    private PenitentStarterLoadoutDefinition _penitentStarterLoadout = new();
    
    public readonly ItemSet AllItemsSet = new();
    public IReadOnlyList<OperationDefinition> Operations { get; private set; } = [];
    public IReadOnlyList<Warbond> Warbonds { get; set; } = [];
    public IReadOnlyList<PenitentDifficulty> PenitentDifficulties { get; private set; } = [];
    public ItemSet PenitentStarterLoadout { get; private set; } = new();

    public async Task EnsureLoadedAsync()
    {
        if (_loaded)
        {
            return;
        }

        Warbonds = await httpClient.GetFromJsonAsync<List<Warbond>>("data/warbonds.json") ?? [];

        Primaries = await LoadItemsAsync("data/primaries.json", ItemKind.Primary);
        Secondaries = await LoadItemsAsync("data/secondaries.json", ItemKind.Secondary);
        Throwables = await LoadItemsAsync("data/throwables.json", ItemKind.Throwable);
        Boosters = await LoadItemsAsync("data/boosters.json", ItemKind.Booster);
        Stratagems = await LoadItemsAsync("data/stratagems.json", ItemKind.Stratagem);
        ArmorPassives = await LoadItemsAsync("data/armor-passives.json", ItemKind.ArmorPassive);

        _penitentStarterLoadout = await httpClient.GetFromJsonAsync<PenitentStarterLoadoutDefinition>("data/penitent-starter-loadout.json") ?? new();
        AllItemsSet.Stratagems = [.. Stratagems];
        AllItemsSet.Primaries = [.. Primaries];
        AllItemsSet.Secondaries = [.. Secondaries];
        AllItemsSet.Throwables = [.. Throwables];
        AllItemsSet.ArmorPassives = [.. ArmorPassives];
        AllItemsSet.Boosters = [.. Boosters];

        PenitentStarterLoadout = ResolveLoadout(_penitentStarterLoadout);

        var operationDefinitions = await httpClient.GetFromJsonAsync<List<OperationDefinition>>("data/operations.json") ?? [];
        Operations = operationDefinitions;

        var difficultyDefinitions = await httpClient.GetFromJsonAsync<List<PenitentDifficultyDefinition>>("data/penitent-difficulties.json") ?? [];
        PenitentDifficulties = difficultyDefinitions
            .Select(definition => new PenitentDifficulty(definition, GetItemSet, GetOperation))
            .ToList();

        _loaded = true;
    }

    public Operation GetOperation(string id)
    {
        var definition = Operations.First(option => option.Id == id);
        return new Operation(definition);
    }

    public ItemSet GetItemSet(IEnumerable<string> internalNames) => new()
    {
        Stratagems = [.. AllItemsSet.Stratagems.Where(item => internalNames.Contains(item.InternalName))],
        Primaries = [.. AllItemsSet.Primaries.Where(item => internalNames.Contains(item.InternalName))],
        Secondaries = [.. AllItemsSet.Secondaries.Where(item => internalNames.Contains(item.InternalName))],
        Throwables = [.. AllItemsSet.Throwables.Where(item => internalNames.Contains(item.InternalName))],
        ArmorPassives = [.. AllItemsSet.ArmorPassives.Where(item => internalNames.Contains(item.InternalName))],
        Boosters = [.. AllItemsSet.Boosters.Where(item => internalNames.Contains(item.InternalName))]
    };

    public ItemSet ResolveLoadout(PenitentStarterLoadoutDefinition definition) => new()
    {
        Stratagems = GetItemSet(definition.Stratagems).Stratagems,
        Primaries = GetItemSet(definition.Primaries).Primaries,
        Secondaries = GetItemSet(definition.Secondaries).Secondaries,
        Throwables = GetItemSet(definition.Throwables).Throwables,
        ArmorPassives = GetItemSet(definition.ArmorPassives).ArmorPassives,
        Boosters = GetItemSet(definition.Boosters).Boosters
    };

    public ItemSet FilterAllItems(IEnumerable<ItemSet> itemSets)
    {
        return AllItemsSet.Clone().RemoveItems(itemSets);
    }

    private async Task<IReadOnlyList<GameItem>> LoadItemsAsync(string path, ItemKind kind)
    {
        var items = await httpClient.GetFromJsonAsync<List<GameItem>>(path) ?? [];

        foreach (var item in items)
        {
            item.PostInitSetup(kind, Warbonds);
        }

        return items;
    }
}
