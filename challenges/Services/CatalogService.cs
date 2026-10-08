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
    private IReadOnlyList<GameItem> Passives { get; set; } = [];
    
    private readonly ItemSet AllItemsSet = new();
    private IReadOnlyList<OperationDefinition> _operationDefinitions { get; set; } = [];
    public int LastOperationIndex => _operationDefinitions.Count;
    private IReadOnlyList<Warbond> Warbonds { get; set; } = [];

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
        Passives = await LoadItemsAsync("data/passives.json", ItemKind.Passive);

        AllItemsSet.Stratagems = [.. Stratagems];
        AllItemsSet.Primaries = [.. Primaries];
        AllItemsSet.Secondaries = [.. Secondaries];
        AllItemsSet.Throwables = [.. Throwables];
        AllItemsSet.Passives = [.. Passives];
        AllItemsSet.Boosters = [.. Boosters];

        _operationDefinitions = await httpClient.GetFromJsonAsync<List<OperationDefinition>>("data/operations.json") ?? [];
        
        _loaded = true;
    }

    private OperationDefinition GetOperationDefinition(int index) => _operationDefinitions[Math.Clamp(index, 1, LastOperationIndex) - 1];
    public Operation GetOperation(int index, int missionIndex) => new(GetOperationDefinition(index), index, missionIndex);
    public Operation GetOperation(int index) => GetOperation(index, 1);

    public HashSet<Warbond> GetAllWarbonds() => [.. Warbonds];
    public HashSet<Warbond> GetWarbonds(IEnumerable<string> ids) => [.. Warbonds.Where(w => ids.Contains(w.Id)).Concat(Warbonds.Where(item => item.Id == "helldiversMobilize"))];

    public ItemSet GetItemSet(IEnumerable<string> ids) => new()
    {
        Stratagems = [.. AllItemsSet.Stratagems.Where(item => ids.Contains(item.Id))],
        Primaries = [.. AllItemsSet.Primaries.Where(item => ids.Contains(item.Id))],
        Secondaries = [.. AllItemsSet.Secondaries.Where(item => ids.Contains(item.Id))],
        Throwables = [.. AllItemsSet.Throwables.Where(item => ids.Contains(item.Id))],
        Passives = [.. AllItemsSet.Passives.Where(item => ids.Contains(item.Id))],
        Boosters = [.. AllItemsSet.Boosters.Where(item => ids.Contains(item.Id))]
    };

    public ItemSet FilterAllItems(IEnumerable<ItemSet> itemSets)
    {
        return AllItemsSet.Clone().RemoveItems(itemSets);
    }

    public ItemSet GetItemsByWarbonds(IEnumerable<Warbond> selectedWarbonds)
    {
        return FilterItemsByWarbonds(AllItemsSet, selectedWarbonds);
    }

    public ItemSet FilterItemsByWarbonds(IEnumerable<GameItem> items, IEnumerable<Warbond> selectedWarbonds)
    {
        HashSet<Warbond> selected = [.. selectedWarbonds];

        return new ItemSet(items.Where(item => item.Warbond is null || selected.Contains(item.Warbond)));
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
