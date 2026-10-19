using System.Text.Json;
using Hd2Challenges.Models;
using Hd2Challenges.Services;

namespace Hd2Challenges.Tests;

public sealed class PersistenceAndAdapterTests
{
    [Fact]
    public async Task BrowserStorageRoundTripsAndRemovesSerializedValues()
    {
        InMemoryJsRuntime jsRuntime = new();
        BrowserStorageService storage = new(jsRuntime);

        Assert.Null(await storage.GetAsync<SaveLibrary<string>>("missing"));

        SaveLibrary<string> library = new();
        library.AddSlot(new SaveSlot<string> { Name = "save", State = "state" });
        await storage.SetAsync("library", library);
        SaveLibrary<string>? restored = await storage.GetAsync<SaveLibrary<string>>("library");

        Assert.NotNull(restored);
        Assert.Equal("state", Assert.Single(restored.Slots).State);
        Assert.Null(restored.WorkingSlot);

        await storage.RemoveAsync("library");
        Assert.Null(await storage.GetAsync<SaveLibrary<string>>("library"));
    }

    [Fact]
    public async Task SaveLibraryServicePersistsAndValidatesExportMode()
    {
        InMemoryJsRuntime jsRuntime = new();
        SaveLibraryService service = new(new BrowserStorageService(jsRuntime));
        SaveSlot<RedemptionStateDefinition> slot = new()
        {
            Name = "campaign",
            State = new RedemptionStateDefinition { DifficultyId = "super", MissionsFailed = 2 }
        };
        SaveExport<RedemptionStateDefinition> export = service.CreateExport("redemption", slot);
        string json = JsonSerializer.Serialize(export, new JsonSerializerOptions(JsonSerializerDefaults.Web));

        SaveSlot<RedemptionStateDefinition>? imported = service.ImportSlot<RedemptionStateDefinition>(json, "redemption");
        SaveSlot<RedemptionStateDefinition>? wrongMode = service.ImportSlot<RedemptionStateDefinition>(json, "other");
        SaveExport<RedemptionStateDefinition> emptyExport = service.CreateExport("redemption", new SaveSlot<RedemptionStateDefinition>
        {
            Name = "empty",
            State = null!
        });
        string emptyJson = JsonSerializer.Serialize(emptyExport, new JsonSerializerOptions(JsonSerializerDefaults.Web));

        Assert.NotNull(imported);
        Assert.NotEqual(slot.Id, imported.Id);
        Assert.Equal(slot.Name, imported.Name);
        Assert.Equal(slot.State.DifficultyId, imported.State.DifficultyId);
        Assert.Equal(2, imported.State.MissionsFailed);
        Assert.Null(wrongMode);
        Assert.Null(service.ImportSlot<RedemptionStateDefinition>(emptyJson, "redemption"));

        SaveLibrary<string> library = new();
        library.AddSlot(new SaveSlot<string> { Name = "persisted", State = "value" });
        await service.PersistAsync("library", library);
        SaveLibrary<string>? loaded = await service.LoadAsync<string>("library");
        Assert.Equal("value", Assert.Single(loaded!.Slots).State);
    }

    [Fact]
    public async Task RedemptionSaveLifecyclePersistsSnapshotsImportsAndSlotChanges()
    {
        using CatalogTestContext context = await CatalogTestContext.CreateAsync();
        InMemoryJsRuntime jsRuntime = new();
        SaveLibraryService genericSaveLibrary = new(new BrowserStorageService(jsRuntime));
        RedemptionSaveLibraryService saveLibrary = new(genericSaveLibrary, context.RedemptionCatalog, context.Catalog);

        RedemptionState state = await saveLibrary.LoadAsync();
        Assert.False(state.RunStarted);
        Assert.NotNull(saveLibrary.Library.WorkingSlot);
        Assert.Empty(saveLibrary.Library.Slots);

        await saveLibrary.CreateSnapshotAsync(state);
        Assert.Empty(saveLibrary.Library.Slots);

        await saveLibrary.BeginRun(state);
        Assert.True(state.RunStarted);
        Assert.True(state.AcquiredItems.ContainsItems(state.StarterItems));
        SaveSlot<RedemptionStateDefinition> original = Assert.Single(saveLibrary.Library.Slots);
        Guid originalId = original.Id;

        state.MissionsFailed = 1;
        await saveLibrary.PersistAsync(state);
        await saveLibrary.CreateSnapshotAsync(state);
        Assert.Equal(2, saveLibrary.Library.Slots.Count);
        SaveSlot<RedemptionStateDefinition> snapshot = Assert.IsType<SaveSlot<RedemptionStateDefinition>>(saveLibrary.Library.CurrentSlot);
        Assert.Contains("Snapshot", snapshot.Name);

        Assert.True(await saveLibrary.RenameSlotAsync(originalId, "  Campaign  "));
        Assert.Equal("Campaign", saveLibrary.GetSlot(originalId)!.Name);
        Assert.False(await saveLibrary.RenameSlotAsync(originalId, " "));

        string exportJson = JsonSerializer.Serialize(
            saveLibrary.CreateExport(originalId), new JsonSerializerOptions(JsonSerializerDefaults.Web));
        (SaveSlot<RedemptionStateDefinition> Slot, RedemptionState State)? import = await saveLibrary.ImportAsync(exportJson);
        Assert.NotNull(import);
        Assert.NotEqual(originalId, import.Value.Slot.Id);
        Assert.Equal("Campaign", import.Value.Slot.Name);
        Assert.Equal(1, import.Value.State.MissionsFailed);
        Assert.Equal(3, saveLibrary.Library.Slots.Count);

        RedemptionState selected = await saveLibrary.SelectSlotAsync(originalId);
        Assert.Equal(1, selected.MissionsFailed);
        (bool deletedSnapshot, RedemptionState unchanged) = await saveLibrary.DeleteSlotAsync(snapshot.Id, selected);
        Assert.True(deletedSnapshot);
        Assert.Same(selected, unchanged);
        Assert.Equal(2, saveLibrary.Library.Slots.Count);

        (bool deletedCurrent, RedemptionState replacement) = await saveLibrary.DeleteSlotAsync(originalId, selected);
        Assert.True(deletedCurrent);
        Assert.False(replacement.RunStarted);
        Assert.Null(saveLibrary.Library.CurrentSlotId);
        Assert.NotNull(saveLibrary.Library.WorkingSlot);

        RedemptionState restarted = await saveLibrary.RestartAsync(replacement);
        Assert.False(restarted.RunStarted);
        Assert.Single(saveLibrary.Library.Slots);
        Assert.NotNull(saveLibrary.Library.WorkingSlot);

        RedemptionSaveLibraryService reloadedService = new(genericSaveLibrary, context.RedemptionCatalog, context.Catalog);
        RedemptionState reloaded = await reloadedService.LoadAsync();
        Assert.False(reloaded.RunStarted);
        Assert.Single(reloadedService.Library.Slots);
    }

    [Fact]
    public async Task InvalidRedemptionImportDoesNotChangeTheLibrary()
    {
        using CatalogTestContext context = await CatalogTestContext.CreateAsync();
        InMemoryJsRuntime jsRuntime = new();
        SaveLibraryService genericSaveLibrary = new(new BrowserStorageService(jsRuntime));
        RedemptionSaveLibraryService saveLibrary = new(genericSaveLibrary, context.RedemptionCatalog, context.Catalog);
        await saveLibrary.LoadAsync();

        (SaveSlot<RedemptionStateDefinition> Slot, RedemptionState State)? imported = await saveLibrary.ImportAsync("{}");

        Assert.Null(imported);
        Assert.Empty(saveLibrary.Library.Slots);
        Assert.NotNull(saveLibrary.Library.WorkingSlot);
    }

    [Fact]
    public async Task FileExportSanitizesNamesAndSerializesJsonPayloads()
    {
        InMemoryJsRuntime jsRuntime = new();
        FileExportService exports = new(jsRuntime);

        await exports.DownloadTextAsync("summary:campaign.txt", "summary");
        await exports.DownloadJsonAsync("campaign:save.json", new { Id = "run-1" });

        Assert.Equal(2, jsRuntime.Downloads.Count);
        Assert.Equal("summary-campaign.txt", jsRuntime.Downloads[0].FileName);
        Assert.Equal("text/plain;charset=utf-8", jsRuntime.Downloads[0].ContentType);
        Assert.Equal("summary", jsRuntime.Downloads[0].Content);
        Assert.Equal("campaign-save.json", jsRuntime.Downloads[1].FileName);
        Assert.Equal("application/json;charset=utf-8", jsRuntime.Downloads[1].ContentType);
        Assert.Contains("\"id\": \"run-1\"", jsRuntime.Downloads[1].Content);
    }

    [Fact]
    public void ToastServiceIgnoresBlankMessagesAndNotifiesOnChanges()
    {
        ToastService toasts = new();
        int changedCount = 0;
        toasts.Changed += () => changedCount++;

        toasts.Show(" ");
        Assert.Empty(toasts.Messages);
        Assert.Equal(0, changedCount);

        toasts.Show("Saved");
        ToastMessage message = Assert.Single(toasts.Messages);
        Assert.Equal(1, changedCount);
        toasts.Dismiss(Guid.NewGuid());
        Assert.Single(toasts.Messages);
        Assert.Equal(1, changedCount);

        toasts.Dismiss(message.Id);
        Assert.Empty(toasts.Messages);
        Assert.Equal(2, changedCount);
    }
}