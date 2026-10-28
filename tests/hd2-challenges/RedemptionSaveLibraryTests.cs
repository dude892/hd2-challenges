using System.Text.Json;
using Hd2Challenges.Models;
using Hd2Challenges.Services;

namespace Hd2Challenges.Tests;

public sealed class RedemptionSaveLibraryTests
{
    private static (SaveLibraryService Generic, RedemptionSaveLibraryService Redemption) CreateServices()
    {
        SaveLibraryService generic = new(new BrowserStorageService(new InMemoryJsRuntime()));
        return (generic, new RedemptionSaveLibraryService(generic));
    }

    private static RedemptionSaveLibraryService CreateService() => CreateServices().Redemption;

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
    public async Task CommittingWorkingSlotPersistsSuppliedStatus()
    {
        RedemptionSaveLibraryService saves = CreateService();
        saves.SetWorkingSlot(new(), "Campaign");

        await saves.CommitWorkingSlotAsync(new(), "Campaign", RunStatus.Failed);
        await saves.LoadAsync();

        Assert.Equal(RunStatus.Failed, Assert.Single(saves.Library.Slots).Status);
    }

    [Fact]
    public async Task PersistingCurrentSlotUpdatesDefinitionAndStatus()
    {
        RedemptionSaveLibraryService saves = CreateService();
        saves.SetWorkingSlot(new(), "Campaign");
        await saves.CommitWorkingSlotAsync(new(), "Campaign");

        await saves.PersistAsync(new RedemptionStateDefinition { MissionsFailed = 2 }, RunStatus.Failed);
        await saves.LoadAsync();

        SaveSlot<RedemptionStateDefinition> slot = Assert.Single(saves.Library.Slots);
        Assert.Equal(2, slot.State.MissionsFailed);
        Assert.Equal(RunStatus.Failed, slot.Status);
    }

    [Theory]
    [InlineData(false, RunStatus.InProgress)]
    [InlineData(true, RunStatus.Completed)]
    public async Task LoadingMigratesLegacyCompletionIntoSlotStatus(
        bool legacyRunCompleted,
        RunStatus expectedStatus)
    {
        (SaveLibraryService generic, RedemptionSaveLibraryService saves) = CreateServices();
        SaveLibrary<RedemptionStateDefinition> library = new();
        library.AddSlot(new SaveSlot<RedemptionStateDefinition>
        {
            State = new RedemptionStateDefinition { RunCompleted = legacyRunCompleted }
        });
        await generic.PersistAsync("blazor-warpath-redemption", library);

        await saves.LoadAsync();

        SaveSlot<RedemptionStateDefinition> migrated = Assert.Single(saves.Library.Slots);
        Assert.Equal(expectedStatus, migrated.Status);
        Assert.Null(migrated.State.RunCompleted);
        SaveLibrary<RedemptionStateDefinition>? persisted =
            await generic.LoadAsync<RedemptionStateDefinition>("blazor-warpath-redemption");
        SaveSlot<RedemptionStateDefinition> persistedSlot = Assert.Single(persisted!.Slots);
        Assert.Equal(expectedStatus, persistedSlot.Status);
        Assert.Null(persistedSlot.State.RunCompleted);
    }

    [Fact]
    public async Task ImportingMigratesLegacyCompletionIntoSlotStatus()
    {
        (SaveLibraryService generic, RedemptionSaveLibraryService saves) = CreateServices();
        SaveSlot<RedemptionStateDefinition> legacy = new()
        {
            State = new RedemptionStateDefinition { RunCompleted = true }
        };
        string json = JsonSerializer.Serialize(generic.CreateExport("warpath-redemption", legacy),
            new JsonSerializerOptions(JsonSerializerDefaults.Web));

        SaveSlot<RedemptionStateDefinition>? imported = await saves.ImportAsync(json);

        Assert.NotNull(imported);
        Assert.Equal(RunStatus.Completed, imported.Status);
        Assert.Null(imported.State.RunCompleted);
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
