using Hd2Challenges.Models;
using Hd2Challenges.Services;

namespace Hd2Challenges.Tests;

public sealed class RedemptionSaveLibraryTests
{
    private static RedemptionSaveLibraryService CreateService() =>
        new(new SaveLibraryService(new BrowserStorageService(new InMemoryJsRuntime())));

    [Fact]
    public async Task LoadingEmptyStorageDoesNotCreateRuntimeSetupOrWorkingSlot()
    {
        RedemptionSaveLibraryService saves = CreateService();

        await saves.LoadAsync();

        Assert.Null(saves.Library.WorkingSlot);
        Assert.Empty(saves.Library.Slots);
    }

    [Fact]
    public async Task CommittingWorkingSlotPersistsSuppliedDefinition()
    {
        RedemptionSaveLibraryService saves = CreateService();
        RedemptionStateDefinition definition = new() { MissionsFailed = 2 };
        saves.SetWorkingSlot(definition, "Campaign");

        await saves.CommitWorkingSlotAsync(definition, "Campaign");
        await saves.LoadAsync();

        Assert.Null(saves.Library.WorkingSlot);
        SaveSlot<RedemptionStateDefinition> slot = Assert.Single(saves.Library.Slots);
        Assert.Equal(2, slot.State.MissionsFailed);
        Assert.StartsWith("Campaign | ", slot.Name);
        Assert.Equal(slot.Id, saves.Library.CurrentSlotId);
    }

    [Fact]
    public async Task DeletingNonCurrentSlotDoesNotRequestReplacementDefinition()
    {
        RedemptionSaveLibraryService saves = CreateService();
        saves.SetWorkingSlot(new(), "Campaign");
        await saves.CommitWorkingSlotAsync(new(), "Campaign");
        Guid previousSlotId = saves.Library.CurrentSlot!.Id;
        await saves.CreateSnapshotAsync(new(), "Campaign");

        bool deleted = await saves.DeleteSlotAsync(previousSlotId,
            () => throw new InvalidOperationException("Replacement should not be requested."), "Campaign");

        Assert.True(deleted);
        Assert.Null(saves.Library.WorkingSlot);
        Assert.Single(saves.Library.Slots);
    }

    [Fact]
    public async Task DeletingCurrentSlotStoresSuppliedReplacementDefinition()
    {
        RedemptionSaveLibraryService saves = CreateService();
        saves.SetWorkingSlot(new(), "Campaign");
        await saves.CommitWorkingSlotAsync(new(), "Campaign");
        Guid slotId = saves.Library.CurrentSlot!.Id;
        RedemptionStateDefinition replacement = new();

        bool deleted = await saves.DeleteSlotAsync(slotId, () => replacement, "Replacement");

        Assert.True(deleted);
        Assert.Same(replacement, saves.Library.WorkingSlot!.State);
        Assert.StartsWith("Replacement | ", saves.Library.WorkingSlot.Name);
        Assert.Null(saves.Library.CurrentSlotId);
        Assert.Empty(saves.Library.Slots);
    }

    [Fact]
    public async Task SelectingMissingSlotFailsWithoutChangingSelection()
    {
        RedemptionSaveLibraryService saves = CreateService();
        saves.SetWorkingSlot(new(), "Campaign");
        SaveSlot<RedemptionStateDefinition> workingSlot = saves.Library.WorkingSlot!;

        await Assert.ThrowsAsync<InvalidOperationException>(() => saves.SelectSlotAsync(Guid.NewGuid()));

        Assert.Same(workingSlot, saves.Library.WorkingSlot);
        Assert.Null(saves.Library.CurrentSlotId);
    }
}
