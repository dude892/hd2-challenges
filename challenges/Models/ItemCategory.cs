namespace Hd2Challenges.Models;

/// <summary>
/// A labeled set of items of one kind. Membership uses case-insensitive item IDs;
/// display ordering remains the responsibility of the catalog.
/// </summary>
public sealed class ItemCategory(ItemKind kind) : IReadOnlyCollection<GameItem>
{
    private readonly HashSet<GameItem> _items = [];

    public ItemKind Kind { get; } = kind;
    public string Label
    {
        get
        {
            return Kind switch
            {
                ItemKind.Stratagem => "Stratagems",
                ItemKind.Primary => "Primary Weapons",
                ItemKind.Secondary => "Secondary Weapons",
                ItemKind.Throwable => "Throwables",
                ItemKind.Passive => "Armor Passives",
                ItemKind.Booster => "Boosters",
                _ => throw new InvalidOperationException($"Unknown item kind: {Kind}")
            };
        }
    }

    public int Count => _items.Count;

    public bool Contains(GameItem item) => _items.Contains(item);

    public bool Add(GameItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        if (item.Kind != Kind)
        {
            throw new ArgumentException($"An item of kind {item.Kind} cannot be added to {Kind}.", nameof(item));
        }

        return _items.Add(item);
    }

    public bool Remove(GameItem item) => _items.Remove(item);

    public void Clear() => _items.Clear();

    internal void UnionWith(ItemCategory category)
    {
        EnsureMatchingKind(category);
        _items.UnionWith(category._items);
    }

    internal void ExceptWith(ItemCategory category)
    {
        EnsureMatchingKind(category);
        _items.ExceptWith(category._items);
    }

    internal void ReplaceWith(ItemCategory category)
    {
        EnsureMatchingKind(category);
        if (ReferenceEquals(this, category)) return;

        _items.Clear();
        _items.UnionWith(category._items);
    }

    private void EnsureMatchingKind(ItemCategory category)
    {
        if (category.Kind != Kind)
        {
            throw new ArgumentException($"A category of kind {category.Kind} cannot be applied to {Kind}.", nameof(category));
        }
    }

    public IEnumerator<GameItem> GetEnumerator() => _items.GetEnumerator();

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
}
