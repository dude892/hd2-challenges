using System.Net.Http.Json;
using Hd2Challenges.Models;

namespace Hd2Challenges.Services;

public sealed class CatalogService(HttpClient httpClient)
{
    private bool _loaded;

    public IReadOnlyList<Warbond> Warbonds { get; private set; } = [];
    public IReadOnlyList<GameItem> Primaries { get; private set; } = [];
    public IReadOnlyList<GameItem> Secondaries { get; private set; } = [];
    public IReadOnlyList<GameItem> Throwables { get; private set; } = [];
    public IReadOnlyList<GameItem> Boosters { get; private set; } = [];
    public IReadOnlyList<GameItem> Stratagems { get; private set; } = [];
    public IReadOnlyList<GameItem> ArmorPassives { get; private set; } = [];
    public IReadOnlyList<PenitentDifficultyOption> PenitentDifficulties { get; private set; } = [];
    public PenitentStarterLoadoutDefinition PenitentStarterLoadout { get; private set; } = new();

    public IReadOnlyList<GameItem> AllItems => [.. Stratagems, .. Primaries, .. Secondaries, .. Throwables, .. ArmorPassives, .. Boosters];

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

        PenitentStarterLoadout = await httpClient.GetFromJsonAsync<PenitentStarterLoadoutDefinition>("data/penitent-starter-loadout.json") ?? new();
        PenitentDifficulties = await httpClient.GetFromJsonAsync<List<PenitentDifficultyOption>>("data/penitent-difficulties.json") ?? [];

        _loaded = true;
    }

    public GameItem? GetItem(string identifier) =>
        AllItems.FirstOrDefault(item =>
            string.Equals(item.InternalName, identifier, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(item.DisplayName, identifier, StringComparison.OrdinalIgnoreCase));

    public IReadOnlyList<GameItem> GetItems(IEnumerable<string> identifiers) => identifiers.Select(GetItem).OfType<GameItem>().ToList();

    public ItemCategorySet ResolveLoadout(PenitentStarterLoadoutDefinition loadout) => new()
    {
        Stratagems = GetItems(loadout.Stratagems).ToList(),
        Primaries = GetItems(loadout.Primaries).ToList(),
        Secondaries = GetItems(loadout.Secondaries).ToList(),
        Throwables = GetItems(loadout.Throwables).ToList(),
        ArmorPassives = GetItems(loadout.ArmorPassives).ToList(),
        Boosters = GetItems(loadout.Boosters).ToList()
    };

    private async Task<IReadOnlyList<GameItem>> LoadItemsAsync(string path, ItemKind kind)
    {
        var items = await httpClient.GetFromJsonAsync<List<GameItem>>(path) ?? [];

        foreach (var item in items)
        {
            item.Kind = kind;
            item.Tags ??= [];
            item.ResolveWarbond(Warbonds);
        }

        return items;
    }
}
