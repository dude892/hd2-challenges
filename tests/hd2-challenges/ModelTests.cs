using Hd2Challenges.Models;

namespace Hd2Challenges.Tests;

public sealed class ModelTests
{
    [Fact]
    public void GameItemEqualityUsesCaseInsensitiveIds()
    {
        GameItem item = new() { Id = "Weapon-1" };
        GameItem sameItem = new() { Id = "weapon-1" };

        Assert.Equal(item, sameItem);
        Assert.Equal(item.GetHashCode(), sameItem.GetHashCode());
        Assert.Single(new HashSet<GameItem> { item, sameItem });
    }

    [Fact]
    public void ItemSetKeepsCategoriesAndMutationsIndependentAcrossClones()
    {
        GameItem primary = new() { Id = "primary" };
        primary.PostInitSetup(ItemKind.Primary, []);
        GameItem booster = new() { Id = "booster" };
        booster.PostInitSetup(ItemKind.Booster, []);
        ItemSet original = new([primary, booster]);

        ItemSet clone = original.Clone();
        clone.RemoveItem(primary);
        clone.ClearSet(ItemKind.Booster);

        Assert.Equal(2, original.Count);
        Assert.Empty(clone);
        Assert.Contains(primary, original.Primaries);
        Assert.Contains(booster, original.Boosters);
    }

    [Fact]
    public void ItemSetOverridesReplaceOnlyPopulatedCategoriesAndAdditionsUnionItems()
    {
        GameItem originalPrimary = new() { Id = "original-primary" };
        originalPrimary.PostInitSetup(ItemKind.Primary, []);
        GameItem originalBooster = new() { Id = "original-booster" };
        originalBooster.PostInitSetup(ItemKind.Booster, []);
        ItemSet loadout = new([originalPrimary, originalBooster]);

        GameItem overridePrimary = new() { Id = "override-primary" };
        overridePrimary.PostInitSetup(ItemKind.Primary, []);
        GameItem addedBooster = new() { Id = "added-booster" };
        addedBooster.PostInitSetup(ItemKind.Booster, []);
        loadout.ApplyOverrides(new ItemSet([overridePrimary]))
            .ApplyAdditions(new ItemSet([originalBooster, addedBooster]));

        Assert.DoesNotContain(originalPrimary, loadout);
        Assert.Contains(overridePrimary, loadout);
        Assert.Contains(originalBooster, loadout);
        Assert.Contains(addedBooster, loadout);
        Assert.Equal(3, loadout.Count);
    }

    [Fact]
    public void OperationClampsValuesAndResetsStarsWhenMissionChanges()
    {
        Operation operation = new(new OperationDefinition
        {
            DisplayName = "Hard",
            MaxStars = 3,
            MissionCount = 2
        }, 0, 99);

        Assert.Equal(1, operation.DifficultyIndex);
        Assert.Equal(2, operation.MissionNumber);
        operation.SelectedStars = 99;
        Assert.Equal(3, operation.SelectedStars);

        operation.MissionNumber = 1;

        Assert.Equal(1, operation.MissionNumber);
        Assert.Equal(0, operation.SelectedStars);
        Assert.True(operation.IsFirstMission);
        Assert.False(operation.IsLastMission);
    }

    [Fact]
    public void SaveLibraryManagesWorkingCurrentAndReplacementSlots()
    {
        SaveSlot<string> working = new() { Name = "setup", State = "draft" };
        SaveLibrary<string> library = new() { WorkingSlot = working };

        library.CommitWorkingSlot();

        Assert.Null(library.WorkingSlot);
        Assert.Same(working, library.CurrentSlot);

        SaveSlot<string> other = new() { Name = "other", State = "saved" };
        library.AddSlot(other);
        Assert.Same(other, library.CurrentSlot);
        Assert.Same(working, library.SelectSlot(working.Id));

        Assert.True(library.RenameSlot(working.Id, "  Renamed  "));
        Assert.Equal("Renamed", working.Name);
        Assert.False(library.RenameSlot(working.Id, " "));
        Assert.False(library.RenameSlot(Guid.NewGuid(), "Missing"));

        (bool deleted, bool replacementCreated) = library.DeleteSlot(working.Id, () => new SaveSlot<string> { Name = "replacement" });

        Assert.True(deleted);
        Assert.True(replacementCreated);
        Assert.Equal("replacement", library.WorkingSlot?.Name);
        Assert.Same(library.WorkingSlot, library.CurrentSlot);
    }

    [Fact]
    public void DeletingLastCurrentSlotCreatesWorkingReplacement()
    {
        SaveSlot<int> slot = new() { State = 1 };
        SaveLibrary<int> library = new();
        library.AddSlot(slot);

        (bool deleted, bool replacementCreated) = library.DeleteSlot(slot.Id, () => new SaveSlot<int> { State = 2 });

        Assert.True(deleted);
        Assert.True(replacementCreated);
        Assert.Empty(library.Slots);
        Assert.Null(library.CurrentSlotId);
        Assert.Equal(2, library.CurrentSlot?.State);
    }
}