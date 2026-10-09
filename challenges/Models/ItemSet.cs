namespace Hd2Challenges.Models;

public sealed class ItemSet : IEnumerable<GameItem>
{
    public HashSet<GameItem> Stratagems { get; set; } = [];
    public HashSet<GameItem> Primaries { get; set; } = [];
    public HashSet<GameItem> Secondaries { get; set; } = [];
    public HashSet<GameItem> Throwables { get; set; } = [];
    public HashSet<GameItem> Passives { get; set; } = [];
    public HashSet<GameItem> Boosters { get; set; } = [];
    
    public ItemSet() { }

    public ItemSet(IEnumerable<GameItem> items)
    {
        Stratagems = [.. items.Where(item => item.Kind == ItemKind.Stratagem)];
        Primaries = [.. items.Where(item => item.Kind == ItemKind.Primary)];
        Secondaries = [.. items.Where(item => item.Kind == ItemKind.Secondary)];
        Throwables = [.. items.Where(item => item.Kind == ItemKind.Throwable)];
        Passives = [.. items.Where(item => item.Kind == ItemKind.Passive)];
        Boosters = [.. items.Where(item => item.Kind == ItemKind.Booster)];
    }

    public ItemSet Clone() => new()
    {
        Stratagems = [.. Stratagems],
        Primaries = [.. Primaries],
        Secondaries = [.. Secondaries],
        Throwables = [.. Throwables],
        Passives = [.. Passives],
        Boosters = [.. Boosters]
    };

    public IEnumerable<HashSet<GameItem>> Categories
    {
        get
        {
            yield return Stratagems;
            yield return Primaries;
            yield return Secondaries;
            yield return Throwables;
            yield return Passives;
            yield return Boosters;
        }
    }

    public IEnumerable<(string Label, IEnumerable<GameItem> Items)> CategoryGroups =>
    [
        ("Stratagems", Stratagems.OrderBy(display => display.DisplayName, StringComparer.OrdinalIgnoreCase)),
        ("Primaries", Primaries.OrderBy(display => display.DisplayName, StringComparer.OrdinalIgnoreCase)),
        ("Secondaries", Secondaries.OrderBy(display => display.DisplayName, StringComparer.OrdinalIgnoreCase)),
        ("Throwables", Throwables.OrderBy(display => display.DisplayName, StringComparer.OrdinalIgnoreCase)),
        ("Armor Passives", Passives.OrderBy(display => display.DisplayName, StringComparer.OrdinalIgnoreCase)),
        ("Boosters", Boosters.OrderBy(display => display.DisplayName, StringComparer.OrdinalIgnoreCase))
    ];

    public HashSet<GameItem> AllItems => [.. Categories.SelectMany(category => category)];

    public IEnumerator<GameItem> GetEnumerator() => AllItems.GetEnumerator();

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();

    public bool IsEmpty => Count == 0;

    public int Count => AllItems.Count;

    public bool ContainsItem(GameItem item) => AllItems.Contains(item);

    public bool ContainsItems(ItemSet items) => items is not null && items.AllItems.All(AllItems.Contains);

    public ItemSet AddItem(GameItem item)
    {
        switch (item.Kind)
        {
            case ItemKind.Stratagem: Stratagems.Add(item); break;
            case ItemKind.Primary: Primaries.Add(item); break;
            case ItemKind.Secondary: Secondaries.Add(item); break;
            case ItemKind.Throwable: Throwables.Add(item); break;
            case ItemKind.Passive: Passives.Add(item); break;
            case ItemKind.Booster: Boosters.Add(item); break;
        }

        return this;
    }

    public ItemSet AddItems(ItemSet items)
    {
        if (items == null) return this;

        Stratagems.UnionWith(items.Stratagems);
        Primaries.UnionWith(items.Primaries);
        Secondaries.UnionWith(items.Secondaries);
        Throwables.UnionWith(items.Throwables);
        Passives.UnionWith(items.Passives);
        Boosters.UnionWith(items.Boosters);

        return this;
    }

    public ItemSet RemoveItem(GameItem item)
    {
        switch (item.Kind)
        {
            case ItemKind.Stratagem: Stratagems.Remove(item); break;
            case ItemKind.Primary: Primaries.Remove(item); break;
            case ItemKind.Secondary: Secondaries.Remove(item); break;
            case ItemKind.Throwable: Throwables.Remove(item); break;
            case ItemKind.Passive: Passives.Remove(item); break;
            case ItemKind.Booster: Boosters.Remove(item); break;
        }

        return this;
    }

    public ItemSet RemoveItems(ItemSet items)
    {
        if (items == null) return this;

        Stratagems.ExceptWith(items.Stratagems);
        Primaries.ExceptWith(items.Primaries);
        Secondaries.ExceptWith(items.Secondaries);
        Throwables.ExceptWith(items.Throwables);
        Passives.ExceptWith(items.Passives);
        Boosters.ExceptWith(items.Boosters);

        return this;
    }

    public ItemSet RemoveItems(IEnumerable<ItemSet> itemSets)
    {
        if (itemSets == null) return this;

        foreach (var itemSet in itemSets)
        {
            RemoveItems(itemSet);
        }

        return this;
    }

    public ItemSet Clear()
    {
        Stratagems.Clear();
        Primaries.Clear();
        Secondaries.Clear();
        Throwables.Clear();
        Passives.Clear();
        Boosters.Clear();

        return this;
    }

    public ItemSet ClearSet(ItemKind kind)
    {
        switch (kind)
        {
            case ItemKind.Stratagem: Stratagems.Clear(); break;
            case ItemKind.Primary: Primaries.Clear(); break;
            case ItemKind.Secondary: Secondaries.Clear(); break;
            case ItemKind.Throwable: Throwables.Clear(); break;
            case ItemKind.Passive: Passives.Clear(); break;
            case ItemKind.Booster: Boosters.Clear(); break;
        }

        return this;
    }

    public ItemSet ApplyOverrides(ItemSet overrides)
    {
        if (overrides == null) return this;
        
        if (overrides.Stratagems.Count != 0) Stratagems = [.. overrides.Stratagems];
        if (overrides.Primaries.Count != 0) Primaries = [.. overrides.Primaries];
        if (overrides.Secondaries.Count != 0) Secondaries = [.. overrides.Secondaries];
        if (overrides.Throwables.Count != 0) Throwables = [.. overrides.Throwables];
        if (overrides.Passives.Count != 0) Passives = [.. overrides.Passives];
        if (overrides.Boosters.Count != 0) Boosters = [.. overrides.Boosters];

        return this;
    }

    public ItemSet ApplyAdditions(ItemSet additions)
    {
        if (additions == null) return this;

        Stratagems.UnionWith(additions.Stratagems);
        Primaries.UnionWith(additions.Primaries);
        Secondaries.UnionWith(additions.Secondaries);
        Throwables.UnionWith(additions.Throwables);
        Passives.UnionWith(additions.Passives);
        Boosters.UnionWith(additions.Boosters);

        return this;
    }
}
