using System.Net.Http.Json;
using Hd2Challenges.Models;

namespace Hd2Challenges.Services;

public sealed class CatalogService(HttpClient httpClient)
{
    private bool _loaded;
    private IReadOnlyList<GameItem> Stratagems { get; set; } = [];

    private IReadOnlyList<GameItem> Primaries { get; set; } = [];
    private IReadOnlyList<GameItem> Secondaries { get; set; } = [];
    private IReadOnlyList<GameItem> Throwables { get; set; } = [];
    private IReadOnlyList<GameItem> Passives { get; set; } = [];
    private IReadOnlyList<GameItem> Boosters { get; set; } = [];
    
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

        Stratagems = await LoadItemsAsync("data/stratagems.json", ItemKind.Stratagem);
        Primaries = await LoadItemsAsync("data/primaries.json", ItemKind.Primary);
        Secondaries = await LoadItemsAsync("data/secondaries.json", ItemKind.Secondary);
        Throwables = await LoadItemsAsync("data/throwables.json", ItemKind.Throwable);
        Passives = await LoadItemsAsync("data/passives.json", ItemKind.Passive);
        Boosters = await LoadItemsAsync("data/boosters.json", ItemKind.Booster);

        AllItemsSet.AddItems(
            new ItemSet(
                Stratagems.Concat(Primaries).Concat(Secondaries).Concat(Throwables).Concat(Passives).Concat(Boosters)
            )
        );

        _operationDefinitions = await httpClient.GetFromJsonAsync<List<OperationDefinition>>("data/operations.json") ?? [];
        
        _loaded = true;
    }

    private OperationDefinition GetOperationDefinition(int index) => _operationDefinitions[Math.Clamp(index, 1, LastOperationIndex) - 1];
    public Operation GetOperation(int index, int missionIndex) => new(GetOperationDefinition(index), index, missionIndex);
    public Operation GetOperation(int index) => GetOperation(index, 1);

    public HashSet<Warbond> GetAllWarbonds() => [.. Warbonds];
    public HashSet<Warbond> GetWarbonds(IEnumerable<string> ids) => [.. Warbonds.Where(warbond => ids.Contains(warbond.Id) || warbond.AlwaysEnabled)];
    public ItemSet GetAllItems() => AllItemsSet.Clone();

    public IReadOnlyList<GameItem> GetItemsInCatalogOrder(IEnumerable<GameItem> items)
    {
        HashSet<GameItem> includedItems = [.. items];
        return
        [
            .. Stratagems.Where(includedItems.Contains),
            .. Primaries.Where(includedItems.Contains),
            .. Secondaries.Where(includedItems.Contains),
            .. Throwables.Where(includedItems.Contains),
            .. Passives.Where(includedItems.Contains),
            .. Boosters.Where(includedItems.Contains)
        ];
    }

    public GameItem GetItem(string id) => AllItemsSet.First(item => string.Equals(item.Id, id, StringComparison.OrdinalIgnoreCase));

    public ItemSet GetItemSet(IEnumerable<string> ids) => new(AllItemsSet.Where(item => ids.Contains(item.Id)));

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
        var definitions = await httpClient.GetFromJsonAsync<List<GameItemDefinition>>(path) ?? [];

        return
        [
            .. definitions.Select(definition => new GameItem(
                definition,
                kind,
                Warbonds.FirstOrDefault(warbond => string.Equals(
                    warbond.Id, definition.WarbondId, StringComparison.OrdinalIgnoreCase))))
        ];
    }
}
