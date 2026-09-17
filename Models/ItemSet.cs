namespace Hd2Challenges.Models;

public sealed class ItemSet : IEnumerable<GameItem>
{
    public HashSet<GameItem> Stratagems { get; set; } = [];
    public HashSet<GameItem> Primaries { get; set; } = [];
    public HashSet<GameItem> Secondaries { get; set; } = [];
    public HashSet<GameItem> Throwables { get; set; } = [];
    public HashSet<GameItem> ArmorPassives { get; set; } = [];
    public HashSet<GameItem> Boosters { get; set; } = [];
    
    public ItemSet Clone() => new()
    {
        Stratagems = [.. Stratagems],
        Primaries = [.. Primaries],
        Secondaries = [.. Secondaries],
        Throwables = [.. Throwables],
        ArmorPassives = [.. ArmorPassives],
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
            yield return ArmorPassives;
            yield return Boosters;
        }
    }

    public HashSet<GameItem> AllItems => [.. Categories.SelectMany(category => category)];

    public IEnumerator<GameItem> GetEnumerator() => AllItems.GetEnumerator();

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();

    public bool IsEmpty => Count == 0;

    public int Count => AllItems.Count;

    public bool ContainsItem(GameItem item) => AllItems.Contains(item);

    public bool ContainsItems(ItemSet items) => items is not null && items.AllItems.All(AllItems.Contains);

    public bool AddItem(GameItem item)
    {
        return item.Kind switch
        {
            ItemKind.Stratagem => Stratagems.Add(item),
            ItemKind.Primary => Primaries.Add(item),
            ItemKind.Secondary => Secondaries.Add(item),
            ItemKind.Throwable => Throwables.Add(item),
            ItemKind.ArmorPassive => ArmorPassives.Add(item),
            ItemKind.Booster => Boosters.Add(item),
            _ => false
        };
    }

    public bool AddItems(ItemSet items)
    {
        if (items == null) return false;

        Stratagems.UnionWith(items.Stratagems);
        Primaries.UnionWith(items.Primaries);
        Secondaries.UnionWith(items.Secondaries);
        Throwables.UnionWith(items.Throwables);
        ArmorPassives.UnionWith(items.ArmorPassives);
        Boosters.UnionWith(items.Boosters);

        return ContainsItems(items);
    }

    public bool RemoveItem(GameItem item)
    {
        return item.Kind switch
        {
            ItemKind.Stratagem => Stratagems.Remove(item),
            ItemKind.Primary => Primaries.Remove(item),
            ItemKind.Secondary => Secondaries.Remove(item),
            ItemKind.Throwable => Throwables.Remove(item),
            ItemKind.ArmorPassive => ArmorPassives.Remove(item),
            ItemKind.Booster => Boosters.Remove(item),
            _ => false
        };
    }

    public bool RemoveItems(ItemSet items)
    {
        if (items == null) return false;

        Stratagems.ExceptWith(items.Stratagems);
        Primaries.ExceptWith(items.Primaries);
        Secondaries.ExceptWith(items.Secondaries);
        Throwables.ExceptWith(items.Throwables);
        ArmorPassives.ExceptWith(items.ArmorPassives);
        Boosters.ExceptWith(items.Boosters);

        return !ContainsItems(items);
    }

    public void ClearSet(ItemKind kind)
    {
        switch (kind)
        {
            case ItemKind.Stratagem: Stratagems.Clear(); break;
            case ItemKind.Primary: Primaries.Clear(); break;
            case ItemKind.Secondary: Secondaries.Clear(); break;
            case ItemKind.Throwable: Throwables.Clear(); break;
            case ItemKind.ArmorPassive: ArmorPassives.Clear(); break;
            case ItemKind.Booster: Boosters.Clear(); break;
        }
    }

    public void ApplyOverrides(ItemSet overrides)
    {
        if (overrides == null) return;
        
        if (overrides.Stratagems.Count != 0) Stratagems = overrides.Stratagems;
        if (overrides.Primaries.Count != 0) Primaries = overrides.Primaries;
        if (overrides.Secondaries.Count != 0) Secondaries = overrides.Secondaries;
        if (overrides.Throwables.Count != 0) Throwables = overrides.Throwables;
        if (overrides.ArmorPassives.Count != 0) ArmorPassives = overrides.ArmorPassives;
        if (overrides.Boosters.Count != 0) Boosters = overrides.Boosters;
    }

    public void ApplyAdditions(ItemSet additions)
    {
        if (additions == null) return;

        Stratagems.UnionWith(additions.Stratagems);
        Primaries.UnionWith(additions.Primaries);
        Secondaries.UnionWith(additions.Secondaries);
        Throwables.UnionWith(additions.Throwables);
        ArmorPassives.UnionWith(additions.ArmorPassives);
        Boosters.UnionWith(additions.Boosters);
    }
}
