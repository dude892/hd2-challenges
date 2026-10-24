using Hd2Challenges.Models;

namespace Hd2Challenges.Tests;

public sealed class ItemCategoryTests
{
    public static TheoryData<ItemKind> CategoryKinds { get; } = new(Enum.GetValues<ItemKind>());

    [Fact]
    public void CategoriesHaveDistinctKindsAndExpectedLabelsInDisplayOrder()
    {
        ItemSet items = new();

        Assert.Equal(
            [
                (ItemKind.Stratagem, "Stratagems"),
                (ItemKind.Primary, "Primary Weapons"),
                (ItemKind.Secondary, "Secondary Weapons"),
                (ItemKind.Throwable, "Throwables"),
                (ItemKind.Passive, "Armor Passives"),
                (ItemKind.Booster, "Boosters")
            ],
            items.Categories.Select(category => (category.Kind, category.Label)));
        Assert.Equal(Enum.GetValues<ItemKind>().Order(), items.Categories.Select(category => category.Kind).Order());
        Assert.All(items.Categories, category =>
        {
            Assert.Empty(category);
            Assert.Same(category, items.GetCategory(category.Kind));
        });
        Assert.Same(items.Stratagems, items.GetCategory(ItemKind.Stratagem));
        Assert.Same(items.Primaries, items.GetCategory(ItemKind.Primary));
        Assert.Same(items.Secondaries, items.GetCategory(ItemKind.Secondary));
        Assert.Same(items.Throwables, items.GetCategory(ItemKind.Throwable));
        Assert.Same(items.Passives, items.GetCategory(ItemKind.Passive));
        Assert.Same(items.Boosters, items.GetCategory(ItemKind.Booster));
    }

    [Theory]
    [MemberData(nameof(CategoryKinds))]
    public void CategoryUsesCaseInsensitiveMembershipAndControlledMutations(ItemKind kind)
    {
        ItemSet items = new();
        ItemCategory category = items.GetCategory(kind);
        GameItem item = CreateItem(kind, "ITEM");
        GameItem sameItem = CreateItem(kind, "item");

        Assert.True(category.Add(item));
        Assert.False(category.Add(sameItem));
        Assert.Single(category);
        Assert.True(category.Contains(sameItem));
        Assert.True(items.ContainsItem(sameItem));
        Assert.True(category.Remove(sameItem));
        Assert.False(category.Remove(item));
        Assert.Empty(category);

        category.Add(item);
        category.Clear();

        Assert.Empty(category);
        Assert.True(items.IsEmpty);
    }

    [Theory]
    [MemberData(nameof(CategoryKinds))]
    public void CategoryRejectsItemsOfEveryOtherKind(ItemKind kind)
    {
        ItemCategory category = new ItemSet().GetCategory(kind);

        foreach (ItemKind otherKind in Enum.GetValues<ItemKind>().Where(otherKind => otherKind != kind))
        {
            Assert.Throws<ArgumentException>(() => category.Add(CreateItem(otherKind, otherKind.ToString())));
        }

        Assert.Empty(category);
        Assert.Throws<ArgumentNullException>(() => category.Add(null!));
    }

    [Theory]
    [MemberData(nameof(CategoryKinds))]
    public void ItemSetRoutesAddsRemovesAndClearsByKind(ItemKind kind)
    {
        ItemSet items = new(Enum.GetValues<ItemKind>().Select(itemKind => CreateItem(itemKind, itemKind.ToString())));
        GameItem item = Assert.Single(items.GetCategory(kind));
        GameItem[] otherItems = [.. items.Where(other => other.Kind != kind)];

        items.RemoveItem(item);
        Assert.Empty(items.GetCategory(kind));
        Assert.True(otherItems.All(items.ContainsItem));

        items.AddItem(item);
        Assert.Single(items.GetCategory(kind));
        items.ClearSet(kind);
        Assert.Empty(items.GetCategory(kind));
        Assert.True(otherItems.All(items.ContainsItem));

        items.Clear();
        Assert.All(items.Categories, category => Assert.Empty(category));
    }

    [Theory]
    [MemberData(nameof(CategoryKinds))]
    public void CloneAndOverridesKeepIndependentMembershipAndStableCategories(ItemKind kind)
    {
        ItemSet original = new(Enum.GetValues<ItemKind>().Select(itemKind => CreateItem(itemKind, itemKind.ToString())));
        ItemSet clone = original.Clone();
        ItemCategory category = clone.GetCategory(kind);
        GameItem originalItem = Assert.Single(category);
        GameItem replacement = CreateItem(kind, "replacement");
        ItemSet overrides = new([replacement]);

        clone.ApplyOverrides(overrides);
        overrides.Clear();

        Assert.Same(category, clone.GetCategory(kind));
        Assert.NotSame(original.GetCategory(kind), category);
        Assert.Equal(original.GetCategory(kind).Label, category.Label);
        Assert.Contains(originalItem, original.GetCategory(kind));
        Assert.DoesNotContain(originalItem, category);
        Assert.Same(replacement, Assert.Single(category));
        Assert.All(clone.Categories.Where(other => other.Kind != kind),
            other => Assert.Equal(original.GetCategory(other.Kind), other));

        clone.Clear();
        Assert.All(original.Categories, other => Assert.Single(other));
    }

    [Theory]
    [MemberData(nameof(CategoryKinds))]
    public void BulkOperationsPreserveSetSemanticsAndSupportSelfApplication(ItemKind kind)
    {
        GameItem first = CreateItem(kind, "first");
        GameItem duplicate = CreateItem(kind, "FIRST");
        GameItem second = CreateItem(kind, "second");
        ItemSet items = new([first]);
        ItemSet additions = new([duplicate, second]);
        ItemSet expected = new([first, second]);

        items.AddItems(additions);
        items.ApplyAdditions(additions);
        additions.Clear();
        Assert.Equal(expected.AllItems, items.AllItems);

        items.AddItems(items);
        items.ApplyAdditions(items);
        items.ApplyOverrides(items);
        Assert.Equal(expected.AllItems, items.AllItems);

        items.RemoveItems(new ItemSet([duplicate]));
        Assert.Same(second, Assert.Single(items));
        items.RemoveItems(items);
        Assert.Empty(items);
    }

    [Fact]
    public void UnknownCategoryKindIsRejected()
    {
        ItemSet items = new();

        Assert.Throws<ArgumentOutOfRangeException>(() => items.GetCategory((ItemKind)int.MaxValue));
    }

    private static GameItem CreateItem(ItemKind kind, string id)
    {
        return new GameItem(new GameItemDefinition { Id = id }, kind, null);
    }
}
