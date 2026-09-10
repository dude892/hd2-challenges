using System.Net.Http.Json;
using Hd2Challenges.Models;

namespace Hd2Challenges.Services;

public sealed class CatalogService(HttpClient httpClient)
{
    private bool _loaded;

    public IReadOnlyList<GameItem> Primaries { get; private set; } = [];
    public IReadOnlyList<GameItem> Secondaries { get; private set; } = [];
    public IReadOnlyList<GameItem> Throwables { get; private set; } = [];
    public IReadOnlyList<GameItem> Boosters { get; private set; } = [];
    public IReadOnlyList<GameItem> Stratagems { get; private set; } = [];
    public IReadOnlyList<GameItem> ArmorPassives { get; private set; } = [];
    public IReadOnlyList<PenitentSpecialist> PenitentSpecialists { get; private set; } = [];

    public IReadOnlyList<WarbondOption> Warbonds { get; } =
    [
        new("warbond0", "Super Citizen Edition"),
        new("warbond1", "Superstore"),
        new("warbond2", "Pre-Order Bonus"),
        new("warbond3", "Helldivers Mobilize", true),
        new("warbond4", "Steeled Veterans"),
        new("warbond5", "Cutting Edge"),
        new("warbond6", "Democratic Detonation"),
        new("warbond7", "Polar Patriots"),
        new("warbond8", "Viper Commandos"),
        new("warbond9", "Freedom's Flame"),
        new("warbond10", "Chemical Agents"),
        new("warbond11", "Truth Enforcers"),
        new("warbond12", "Urban Legends"),
        new("warbond13", "Servants of Freedom"),
        new("warbond14", "Borderline Justice"),
        new("warbond15", "Masters of Ceremony"),
        new("warbond16", "Force of Law"),
        new("warbond17", "Control Group"),
        new("warbond18", "KILLZONE"),
        new("warbond19", "Halo ODST"),
        new("warbond20", "Dust Devils"),
        new("warbond21", "Python Commandos"),
        new("warbond22", "Redacted Regiment"),
        new("warbond23", "Siege Breakers"),
        new("warbond24", "Entrenched Division"),
        new("warbond25", "Exo Experts"),
        new("warbond26", "Warhammer 40k")
    ];

    public IReadOnlyList<GameItem> AllItems =>
        [.. Stratagems, .. Primaries, .. Secondaries, .. Throwables, .. ArmorPassives, .. Boosters];

    public async Task EnsureLoadedAsync()
    {
        if (_loaded)
        {
            return;
        }

        Primaries = await LoadItemsAsync("data/primaries.json", ItemKind.Primary);
        Secondaries = await LoadItemsAsync("data/secondaries.json", ItemKind.Secondary);
        Throwables = await LoadItemsAsync("data/throwables.json", ItemKind.Throwable);
        Boosters = await LoadItemsAsync("data/boosters.json", ItemKind.Booster);
        Stratagems = await LoadItemsAsync("data/stratagems.json", ItemKind.Stratagem);
        ArmorPassives = await LoadItemsAsync("data/armor-passives.json", ItemKind.ArmorPassive);
        PenitentSpecialists = await httpClient.GetFromJsonAsync<List<PenitentSpecialist>>("data/penitent-specialists.json") ?? [];
        _loaded = true;
    }

    public GameItem? GetItem(string internalName) =>
        AllItems.FirstOrDefault(item => string.Equals(item.InternalName, internalName, StringComparison.Ordinal));

    public IReadOnlyList<GameItem> GetItems(IEnumerable<string> internalNames) =>
        internalNames
            .Select(GetItem)
            .OfType<GameItem>()
            .ToList();

    private async Task<IReadOnlyList<GameItem>> LoadItemsAsync(string path, ItemKind kind)
    {
        var items = await httpClient.GetFromJsonAsync<List<GameItem>>(path) ?? [];
        foreach (var item in items)
        {
            item.Kind = kind;
            item.Tags ??= [];
        }

        return items;
    }
}
