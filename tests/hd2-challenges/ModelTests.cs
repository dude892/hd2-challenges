using Hd2Challenges.Models;

namespace Hd2Challenges.Tests;

public sealed class ModelTests
{
    [Fact]
    public void GameItemEqualityUsesCaseInsensitiveIds()
    {
        GameItem item = new(new GameItemDefinition { Id = "Weapon-1" }, ItemKind.Primary, null);
        GameItem sameItem = new(new GameItemDefinition { Id = "weapon-1" }, ItemKind.Primary, null);

        Assert.Equal(item, sameItem);
        Assert.Equal(item.GetHashCode(), sameItem.GetHashCode());
        Assert.Single(new HashSet<GameItem> { item, sameItem });
    }

    [Fact]
    public void ItemSetCloneIsIndependentFromOriginalMutations()
    {
        GameItem primary = new(new GameItemDefinition { Id = "primary" }, ItemKind.Primary, null);
        GameItem booster = new(new GameItemDefinition { Id = "booster" }, ItemKind.Booster, null);
        ItemSet original = new([primary, booster]);

        ItemSet clone = original.Clone();
        clone.RemoveItem(primary);

        Assert.Equal(2, original.Count);
        Assert.Single(clone);
        Assert.Contains(primary, original.Primaries);
        Assert.Contains(booster, original.Boosters);
    }

    [Fact]
    public void ItemSetClearCategoryRemovesOnlyItemsInThatCategory()
    {
        GameItem primary = new(new GameItemDefinition { Id = "primary" }, ItemKind.Primary, null);
        GameItem booster = new(new GameItemDefinition { Id = "booster" }, ItemKind.Booster, null);
        ItemSet items = new([primary, booster]);

        items.ClearSet(ItemKind.Booster);

        Assert.Contains(primary, items);
        Assert.DoesNotContain(booster, items);
    }

    [Fact]
    public void ItemSetOverridesReplaceOnlyPopulatedCategories()
    {
        GameItem originalPrimary = new(new GameItemDefinition { Id = "original-primary" }, ItemKind.Primary, null);
        GameItem originalBooster = new(new GameItemDefinition { Id = "original-booster" }, ItemKind.Booster, null);
        ItemSet loadout = new([originalPrimary, originalBooster]);

        GameItem overridePrimary = new(new GameItemDefinition { Id = "override-primary" }, ItemKind.Primary, null);
        loadout.ApplyOverrides(new ItemSet([overridePrimary]));

        Assert.DoesNotContain(originalPrimary, loadout);
        Assert.Contains(overridePrimary, loadout);
        Assert.Contains(originalBooster, loadout);
        Assert.Equal(2, loadout.Count);
    }

    [Fact]
    public void ItemSetAdditionsUnionItemsWithoutReplacingExistingCategories()
    {
        GameItem originalPrimary = new(new GameItemDefinition { Id = "original-primary" }, ItemKind.Primary, null);
        GameItem addedBooster = new(new GameItemDefinition { Id = "added-booster" }, ItemKind.Booster, null);
        ItemSet loadout = new([originalPrimary]);

        loadout.ApplyAdditions(new ItemSet([addedBooster]));

        Assert.Contains(originalPrimary, loadout);
        Assert.Contains(addedBooster, loadout);
        Assert.Equal(2, loadout.Count);
    }

    [Fact]
    public void OperationClampsDifficultyIndex()
    {
        Operation operation = new(new OperationDefinition { MissionCount = 2 }, 0, 1);

        Assert.Equal(1, operation.DifficultyIndex);
    }

    [Fact]
    public void OperationClampsMissionNumber()
    {
        Operation operation = new(new OperationDefinition { MissionCount = 2 }, 1, 99);

        Assert.Equal(2, operation.MissionNumber);
        Assert.False(operation.IsFirstMission);
        Assert.True(operation.IsLastMission);
    }

    [Fact]
    public void OperationClampsSelectedStars()
    {
        Operation operation = new(new OperationDefinition { MaxStars = 3, MissionCount = 1 }, 1, 1)
        {
            SelectedStars = 99
        };

        Assert.Equal(3, operation.SelectedStars);
    }

    [Fact]
    public void OperationResetsSelectedStarsWhenMissionChanges()
    {
        Operation operation = new(new OperationDefinition { MaxStars = 3, MissionCount = 2 }, 1, 2)
        {
            SelectedStars = 2
        };

        operation.MissionNumber = 1;

        Assert.Equal(0, operation.SelectedStars);
    }

    [Fact]
    public void SaveLibraryCommitMakesWorkingSlotCurrent()
    {
        SaveSlot<string> working = new() { Name = "setup", State = "draft" };
        SaveLibrary<string> library = new() { WorkingSlot = working };

        library.CommitWorkingSlot();

        Assert.Null(library.WorkingSlot);
        Assert.Same(working, library.CurrentSlot);

    }

    [Fact]
    public void SaveLibraryAddSlotMakesAddedSlotCurrent()
    {
        SaveSlot<string> other = new() { Name = "other", State = "saved" };
        SaveLibrary<string> library = new();

        library.AddSlot(other);

        Assert.Same(other, library.CurrentSlot);
    }

    [Fact]
    public void SaveLibrarySelectSlotMakesSelectedSlotCurrent()
    {
        SaveSlot<string> first = new() { Name = "first", State = "draft" };
        SaveSlot<string> second = new() { Name = "second", State = "saved" };
        SaveLibrary<string> library = new();
        library.AddSlot(first);
        library.AddSlot(second);

        Assert.Same(first, library.SelectSlot(first.Id));
        Assert.Same(first, library.CurrentSlot);
    }

    [Fact]
    public void SaveLibraryRenameTrimsName()
    {
        SaveSlot<string> working = new() { Name = "setup", State = "draft" };
        SaveLibrary<string> library = new() { WorkingSlot = working };
        library.CommitWorkingSlot();

        Assert.True(library.RenameSlot(working.Id, "  Renamed  "));
        Assert.Equal("Renamed", library.CurrentSlot!.Name);
    }

    [Fact]
    public void SaveLibraryRejectsBlankRename()
    {
        SaveSlot<string> working = new() { Name = "setup", State = "draft" };
        SaveLibrary<string> library = new() { WorkingSlot = working };
        library.CommitWorkingSlot();

        Assert.False(library.RenameSlot(working.Id, " "));
    }

    [Fact]
    public void SaveLibraryRejectsRenameForUnknownSlot()
    {
        SaveLibrary<string> library = new();

        Assert.False(library.RenameSlot(Guid.NewGuid(), "Missing"));
    }

    [Fact]
    public void SaveLibraryDeletingCurrentSlotCreatesWorkingReplacement()
    {
        SaveSlot<string> working = new() { Name = "setup", State = "draft" };
        SaveLibrary<string> library = new() { WorkingSlot = working };
        library.CommitWorkingSlot();

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