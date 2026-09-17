namespace Hd2Challenges.Models;

public sealed class ItemCategorySet
{
    public List<GameItem> Stratagems { get; set; } = [];
    public List<GameItem> Primaries { get; set; } = [];
    public List<GameItem> Secondaries { get; set; } = [];
    public List<GameItem> Throwables { get; set; } = [];
    public List<GameItem> ArmorPassives { get; set; } = [];
    public List<GameItem> Boosters { get; set; } = [];

    public List<GameItem> AllItems => [.. Stratagems, .. Primaries, .. Secondaries, .. Throwables, .. ArmorPassives, .. Boosters];

    public HashSet<string> AllInternalNames => AllItems.Select(item => item.InternalName).ToHashSet();

    public bool IsEmpty => TotalItemCount == 0;

    public int TotalItemCount => AllItems.Count;

    public bool ContainsItem(GameItem item) 
    {
        return item.Kind switch
        {
            ItemKind.Stratagem => Stratagems.Contains(item),
            ItemKind.Primary => Primaries.Contains(item),
            ItemKind.Secondary => Secondaries.Contains(item),
            ItemKind.Throwable => Throwables.Contains(item),
            ItemKind.ArmorPassive => ArmorPassives.Contains(item),
            ItemKind.Booster => Boosters.Contains(item),
            _ => false,
        };
    }

    public bool AddItem(GameItem item)
    {
        switch (item.Kind)
        {
            case ItemKind.Stratagem: Stratagems.Add(item); break;
            case ItemKind.Primary: Primaries.Add(item); break;
            case ItemKind.Secondary: Secondaries.Add(item); break;
            case ItemKind.Throwable: Throwables.Add(item); break;
            case ItemKind.ArmorPassive: ArmorPassives.Add(item); break;
            case ItemKind.Booster: Boosters.Add(item); break;
        }

        return ContainsItem(item);
    }

    public bool RemoveItem(GameItem item)
    {
        switch (item.Kind)
        {
            case ItemKind.Stratagem: Stratagems.Remove(item); break;
            case ItemKind.Primary: Primaries.Remove(item); break;
            case ItemKind.Secondary: Secondaries.Remove(item); break;
            case ItemKind.Throwable: Throwables.Remove(item); break;
            case ItemKind.ArmorPassive: ArmorPassives.Remove(item); break;
            case ItemKind.Booster: Boosters.Remove(item); break;
        }
        
        return !ContainsItem(item);
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

    public void ApplyOverrides(ItemCategorySet overrides)
    {
        if (overrides == null) return;
        
        if (overrides.Stratagems.Count != 0) Stratagems = overrides.Stratagems;
        if (overrides.Primaries.Count != 0) Primaries = overrides.Primaries;
        if (overrides.Secondaries.Count != 0) Secondaries = overrides.Secondaries;
        if (overrides.Throwables.Count != 0) Throwables = overrides.Throwables;
        if (overrides.ArmorPassives.Count != 0) ArmorPassives = overrides.ArmorPassives;
        if (overrides.Boosters.Count != 0) Boosters = overrides.Boosters;
    }

    public void ApplyAdditions(ItemCategorySet additions)
    {
        if (additions == null) return;

        Stratagems.AddRange(additions.Stratagems);
        Primaries.AddRange(additions.Primaries);
        Secondaries.AddRange(additions.Secondaries);
        Throwables.AddRange(additions.Throwables);
        ArmorPassives.AddRange(additions.ArmorPassives);
        Boosters.AddRange(additions.Boosters);
    }
}
