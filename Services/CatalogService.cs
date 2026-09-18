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
    
    public readonly ItemSet AllItemsSet = new();
    public IReadOnlyList<OperationDefinition> Operations { get; private set; } = [];
    public IReadOnlyList<Warbond> Warbonds { get; set; } = [];

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

        AllItemsSet.Stratagems = [.. Stratagems];
        AllItemsSet.Primaries = [.. Primaries];
        AllItemsSet.Secondaries = [.. Secondaries];
        AllItemsSet.Throwables = [.. Throwables];
        AllItemsSet.ArmorPassives = [.. ArmorPassives];
        AllItemsSet.Boosters = [.. Boosters];

        Operations = await httpClient.GetFromJsonAsync<List<OperationDefinition>>("data/operations.json") ?? [];

        _loaded = true;
    }

    public Operation GetOperation(string id) => new(Operations.First(option => option.Id == id));

    public OperationDefinition? GetNextOperationDefinition(string operationId)
    {
        for (int i = 0; i < Operations.Count; i++)
        {
            if (Operations[i].Id == operationId)
            {
                if (i + 1 < Operations.Count)
                {
                    return Operations[i + 1];
                }

                return null;
            }
        }

        return null;
    }

    public ItemSet GetItemSet(IEnumerable<string> ids) => new()
    {
        Stratagems = [.. AllItemsSet.Stratagems.Where(item => ids.Contains(item.Id))],
        Primaries = [.. AllItemsSet.Primaries.Where(item => ids.Contains(item.Id))],
        Secondaries = [.. AllItemsSet.Secondaries.Where(item => ids.Contains(item.Id))],
        Throwables = [.. AllItemsSet.Throwables.Where(item => ids.Contains(item.Id))],
        ArmorPassives = [.. AllItemsSet.ArmorPassives.Where(item => ids.Contains(item.Id))],
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
