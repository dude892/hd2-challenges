namespace Hd2Challenges.Models;

public sealed class ItemSet : IEnumerable<GameItem>
{
    public ItemCategory Stratagems { get; } = new(ItemKind.Stratagem);
    public ItemCategory Primaries { get; } = new(ItemKind.Primary);
    public ItemCategory Secondaries { get; } = new(ItemKind.Secondary);
    public ItemCategory Throwables { get; } = new(ItemKind.Throwable);
    public ItemCategory Passives { get; } = new(ItemKind.Passive);
    public ItemCategory Boosters { get; } = new(ItemKind.Booster);
    
    public ItemSet() { }

    public ItemSet(IEnumerable<GameItem> items)
    {
        foreach (var item in items)
        {
            AddItem(item);
        }
    }

    public ItemSet Clone() => new(this);

    public IEnumerable<ItemCategory> Categories
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

    public ItemCategory GetCategory(ItemKind kind) => kind switch
    {
        ItemKind.Stratagem => Stratagems,
        ItemKind.Primary => Primaries,
        ItemKind.Secondary => Secondaries,
        ItemKind.Throwable => Throwables,
        ItemKind.Passive => Passives,
        ItemKind.Booster => Boosters,
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
    };

    public HashSet<GameItem> AllItems => [.. Categories.SelectMany(category => category)];

    public IEnumerator<GameItem> GetEnumerator() => AllItems.GetEnumerator();

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();

    public bool IsEmpty => Count == 0;

    public int Count => AllItems.Count;

    public bool ContainsItem(GameItem item) => AllItems.Contains(item);

    public bool ContainsItems(ItemSet items) => items is not null && items.AllItems.All(AllItems.Contains);

    public ItemSet AddItem(GameItem item)
    {
        GetCategory(item.Kind).Add(item);

        return this;
    }

    public ItemSet AddItems(ItemSet items)
    {
        if (items == null) return this;

        foreach (var category in Categories)
        {
            category.UnionWith(items.GetCategory(category.Kind));
        }

        return this;
    }

    public ItemSet RemoveItem(GameItem item)
    {
        GetCategory(item.Kind).Remove(item);

        return this;
    }

    public ItemSet RemoveItems(ItemSet items)
    {
        if (items == null) return this;

        foreach (var category in Categories)
        {
            category.ExceptWith(items.GetCategory(category.Kind));
        }

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
        foreach (var category in Categories)
        {
            category.Clear();
        }

        return this;
    }

    public ItemSet ClearSet(ItemKind kind)
    {
        GetCategory(kind).Clear();

        return this;
    }

    public ItemSet ApplyOverrides(ItemSet overrides)
    {
        if (overrides == null) return this;
        
        foreach (var category in Categories)
        {
            var replacement = overrides.GetCategory(category.Kind);
            if (replacement.Count != 0) category.ReplaceWith(replacement);
        }

        return this;
    }

    public ItemSet ApplyAdditions(ItemSet additions) => AddItems(additions);
}
