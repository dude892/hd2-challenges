using System.Text.Json;
using Hd2Challenges.Models;
using Hd2Challenges.Services;

namespace Hd2Challenges.Tests;

public sealed class PersistenceAndAdapterTests
{
    private static SaveLibraryService CreateSaveLibraryService() =>
        new(new BrowserStorageService(new InMemoryJsRuntime()));

    private static SaveSlot<RedemptionStateDefinition> CreateDefinitionSlot() => new()
    {
        Name = "campaign",
        State = new RedemptionStateDefinition { DifficultyId = "super", MissionsFailed = 2 }
    };

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RedemptionSessionLoadsCatalogBeforeResolvingState(bool hasSavedRun)
    {
        using CatalogTestContext context = await CatalogTestContext.CreateAsync(loadCatalog: false);
        SaveLibraryService genericSaveLibrary = CreateSaveLibraryService();
        if (hasSavedRun)
        {
            SaveLibrary<RedemptionStateDefinition> library = new();
            library.AddSlot(CreateDefinitionSlot());
            await genericSaveLibrary.PersistAsync("blazor-warpath-redemption", library);
        }

        RedemptionSaveLibraryService saveLibrary = new(genericSaveLibrary);
        RedemptionSessionService session = new(context.RedemptionCatalog, saveLibrary);

        await session.LoadAsync();
        RedemptionState state = session.State;

        Assert.Equal(hasSavedRun, state.RunStarted);
        Assert.Same(context.RedemptionCatalog.GetDifficulty(state.Difficulty.Id), state.Difficulty);
        Assert.Equal(context.RedemptionCatalog.GetStarterItems(state.Difficulty, state.SelectedWarbonds).ToHashSet(),
            state.StarterItems.ToHashSet());
    }

    [Fact]
    public async Task BrowserStorageReturnsNullForMissingValues()
    {
        InMemoryJsRuntime jsRuntime = new();
        BrowserStorageService storage = new(jsRuntime);

        Assert.Null(await storage.GetAsync<SaveLibrary<string>>("missing"));
    }

    [Fact]
    public async Task BrowserStorageRoundTripsSerializedValues()
    {
        InMemoryJsRuntime jsRuntime = new();
        BrowserStorageService storage = new(jsRuntime);
        SaveLibrary<string> library = new();
        library.AddSlot(new SaveSlot<string> { Name = "save", State = "state" });
        await storage.SetAsync("library", library);
        SaveLibrary<string>? restored = await storage.GetAsync<SaveLibrary<string>>("library");

        Assert.NotNull(restored);
        Assert.Equal("state", Assert.Single(restored.Slots).State);
        Assert.Null(restored.WorkingSlot);
    }

    [Fact]
    public async Task BrowserStorageRemovesSerializedValues()
    {
        InMemoryJsRuntime jsRuntime = new();
        BrowserStorageService storage = new(jsRuntime);
        await storage.SetAsync("library", new SaveLibrary<string>());

        await storage.RemoveAsync("library");
        Assert.Null(await storage.GetAsync<SaveLibrary<string>>("library"));
    }

    [Fact]
    public async Task SaveLibraryServiceImportsExportedSlotWithNewId()
    {
        SaveLibraryService service = CreateSaveLibraryService();
        SaveSlot<RedemptionStateDefinition> slot = new()
        {
            Name = "campaign",
            State = new RedemptionStateDefinition { DifficultyId = "super", MissionsFailed = 2 }
        };
        SaveExport<RedemptionStateDefinition> export = service.CreateExport("redemption", slot);
        string json = JsonSerializer.Serialize(export, new JsonSerializerOptions(JsonSerializerDefaults.Web));

        SaveSlot<RedemptionStateDefinition>? imported = service.ImportSlot<RedemptionStateDefinition>(json, "redemption");

        Assert.NotNull(imported);
        Assert.NotEqual(slot.Id, imported.Id);
        Assert.Equal(slot.Name, imported.Name);
        Assert.Equal(slot.State.DifficultyId, imported.State.DifficultyId);
        Assert.Equal(2, imported.State.MissionsFailed);
    }

    [Fact]
    public void SaveLibraryServiceRejectsExportWithWrongMode()
    {
        SaveLibraryService service = CreateSaveLibraryService();
        SaveExport<RedemptionStateDefinition> export = service.CreateExport("redemption", CreateDefinitionSlot());
        string json = JsonSerializer.Serialize(export, new JsonSerializerOptions(JsonSerializerDefaults.Web));

        Assert.Null(service.ImportSlot<RedemptionStateDefinition>(json, "other"));
    }

    [Fact]
    public void SaveLibraryServiceRejectsExportWithoutState()
    {
        SaveLibraryService service = CreateSaveLibraryService();
        SaveSlot<RedemptionStateDefinition> slot = new() { Name = "empty", State = null! };
        string json = JsonSerializer.Serialize(
            service.CreateExport("redemption", slot), new JsonSerializerOptions(JsonSerializerDefaults.Web));

        Assert.Null(service.ImportSlot<RedemptionStateDefinition>(json, "redemption"));
    }

    [Fact]
    public async Task SaveLibraryServicePersistsAndLoadsLibrary()
    {
        SaveLibraryService service = CreateSaveLibraryService();
        SaveLibrary<string> library = new();
        library.AddSlot(new SaveSlot<string> { Name = "persisted", State = "value" });
        await service.PersistAsync("library", library);
        SaveLibrary<string>? loaded = await service.LoadAsync<string>("library");
        
        Assert.Equal("value", Assert.Single(loaded!.Slots).State);
    }

    [Fact]
    public async Task RedemptionSessionLoadsWorkingSetupSlot()
    {
        using RedemptionSaveTestContext context = await RedemptionSaveTestContext.CreateAsync();
        await context.Session.LoadAsync();
        RedemptionState state = context.Session.State;

        Assert.False(state.RunStarted);
        Assert.NotNull(context.SaveLibrary.Library.WorkingSlot);
        Assert.Equal("normal", context.SaveLibrary.Library.WorkingSlot.State.DifficultyId);
        Assert.Empty(context.SaveLibrary.Library.Slots);
    }

        [Fact]
        public async Task RedemptionSessionDoesNotCreateSnapshotBeforeRunStarts()
        {
            using RedemptionSaveTestContext context = await RedemptionSaveTestContext.CreateAsync();
            await context.Session.LoadAsync();

            await context.Session.CreateSnapshotAsync();

            Assert.Empty(context.SaveLibrary.Library.Slots);
        }

        [Fact]
        public async Task RedemptionSessionBeginRunCommitsWorkingSlot()
        {
            using RedemptionSaveTestContext context = await RedemptionSaveTestContext.CreateAsync();
            await context.Session.LoadAsync();
            RedemptionState state = context.Session.State;

            await context.Session.BeginRunAsync();

            Assert.True(state.RunStarted);
            Assert.True(state.AcquiredItems.ContainsItems(state.StarterItems));
            Assert.Single(context.SaveLibrary.Library.Slots);
            Assert.Null(context.SaveLibrary.Library.WorkingSlot);
        }

        [Fact]
        public async Task RedemptionSaveLibraryCreatesSnapshotFromCurrentState()
        {
            using RedemptionSaveTestContext context = await RedemptionSaveTestContext.CreateAsync();
            await context.Session.LoadAsync();
            RedemptionState state = context.Session.State;
            await context.Session.BeginRunAsync();
            state.MissionsFailed = 1;
            await context.Session.PersistAsync();
            await context.Session.CreateSnapshotAsync();

            Assert.Equal(2, context.SaveLibrary.Library.Slots.Count);
            SaveSlot<RedemptionStateDefinition> snapshot = Assert.IsType<SaveSlot<RedemptionStateDefinition>>(
                context.SaveLibrary.Library.CurrentSlot);
            Assert.Contains("Snapshot", snapshot.Name);
            Assert.Equal(1, snapshot.State.MissionsFailed);
        }

        [Fact]
        public async Task RedemptionSaveLibraryRenamesSlotAndRejectsBlankName()
        {
            using RedemptionSaveTestContext context = await RedemptionSaveTestContext.CreateAsync();
            await context.Session.LoadAsync();
            await context.Session.BeginRunAsync();
            Guid slotId = Assert.Single(context.SaveLibrary.Library.Slots).Id;

            Assert.True(await context.SaveLibrary.RenameSlotAsync(slotId, "  Campaign  "));
            Assert.Equal("Campaign", context.SaveLibrary.GetSlot(slotId)!.Name);
            Assert.False(await context.SaveLibrary.RenameSlotAsync(slotId, " "));
        }

        [Fact]
        public async Task RedemptionSaveLibraryImportsExportAsNewSlot()
        {
            using RedemptionSaveTestContext context = await RedemptionSaveTestContext.CreateAsync();
            await context.Session.LoadAsync();
            RedemptionState state = context.Session.State;
            await context.Session.BeginRunAsync();
            state.MissionsFailed = 1;
            await context.Session.PersistAsync();
            SaveSlot<RedemptionStateDefinition> original = Assert.Single(context.SaveLibrary.Library.Slots);
            string exportJson = JsonSerializer.Serialize(
                context.SaveLibrary.CreateExport(original.Id), new JsonSerializerOptions(JsonSerializerDefaults.Web));

            SaveSlot<RedemptionStateDefinition>? imported =
                await context.SaveLibrary.ImportAsync(exportJson);

            Assert.NotNull(imported);
            Assert.NotEqual(original.Id, imported.Id);
            Assert.Equal(original.Name, imported.Name);
            Assert.Equal(1, imported.State.MissionsFailed);
            Assert.Equal(2, context.SaveLibrary.Library.Slots.Count);
        }

        [Fact]
        public async Task RedemptionSessionSelectsSlotAndRestoresItsState()
        {
            using RedemptionSaveTestContext context = await RedemptionSaveTestContext.CreateAsync();
            await context.Session.LoadAsync();
            RedemptionState state = context.Session.State;
            await context.Session.BeginRunAsync();
            Guid originalId = Assert.Single(context.SaveLibrary.Library.Slots).Id;
            state.MissionsFailed = 1;
            await context.Session.PersistAsync();
            await context.Session.CreateSnapshotAsync();

            await context.Session.SelectSlotAsync(originalId);
            RedemptionState selected = context.Session.State;

            Assert.Equal(1, selected.MissionsFailed);
            Assert.Equal(originalId, context.SaveLibrary.Library.CurrentSlotId);
        }

        [Fact]
        public async Task RedemptionSessionDeletesNonCurrentSlotWithoutChangingSelectedState()
        {
            using RedemptionSaveTestContext context = await RedemptionSaveTestContext.CreateAsync();
            await context.Session.LoadAsync();
            await context.Session.BeginRunAsync();
            Guid originalId = Assert.Single(context.SaveLibrary.Library.Slots).Id;
            await context.Session.CreateSnapshotAsync();
            Guid snapshotId = context.SaveLibrary.Library.CurrentSlot!.Id;
            await context.Session.SelectSlotAsync(snapshotId);
            RedemptionState selected = context.Session.State;

            bool deleted = await context.Session.DeleteSlotAsync(originalId);

            Assert.True(deleted);
            Assert.Same(selected, context.Session.State);
            Assert.Single(context.SaveLibrary.Library.Slots);
        }

        [Fact]
        public async Task RedemptionSessionDeletingCurrentSlotCreatesWorkingReplacement()
        {
            using RedemptionSaveTestContext context = await RedemptionSaveTestContext.CreateAsync();
            await context.Session.LoadAsync();
            await context.Session.BeginRunAsync();
            Guid currentSlotId = Assert.Single(context.SaveLibrary.Library.Slots).Id;

            bool deleted = await context.Session.DeleteSlotAsync(currentSlotId);
            RedemptionState replacement = context.Session.State;

            Assert.True(deleted);
            Assert.False(replacement.RunStarted);
            Assert.Null(context.SaveLibrary.Library.CurrentSlotId);
            Assert.NotNull(context.SaveLibrary.Library.WorkingSlot);
            Assert.Empty(context.SaveLibrary.Library.Slots);
        }

        [Fact]
        public async Task RedemptionSessionRestartCreatesWorkingSetupState()
        {
            using RedemptionSaveTestContext context = await RedemptionSaveTestContext.CreateAsync();
            await context.Session.LoadAsync();
            await context.Session.BeginRunAsync();

            await context.Session.RestartAsync();
            RedemptionState restarted = context.Session.State;

            Assert.False(restarted.RunStarted);
            Assert.NotNull(context.SaveLibrary.Library.WorkingSlot);
            Assert.Null(context.SaveLibrary.Library.CurrentSlotId);
        }

        [Fact]
        public async Task RedemptionSaveLibraryReloadRestoresPersistedSlots()
        {
            using RedemptionSaveTestContext context = await RedemptionSaveTestContext.CreateAsync();
            await context.Session.LoadAsync();
            await context.Session.BeginRunAsync();
            RedemptionSaveLibraryService reloadedService = new(context.GenericSaveLibrary);

            await reloadedService.LoadAsync();

            Assert.Single(reloadedService.Library.Slots);
        }

        [Fact]
        public async Task InvalidRedemptionImportDoesNotChangeTheLibrary()
        {
            using RedemptionSaveTestContext context = await RedemptionSaveTestContext.CreateAsync();
            await context.Session.LoadAsync();

            SaveSlot<RedemptionStateDefinition>? imported =
                await context.SaveLibrary.ImportAsync("{}");

            Assert.Null(imported);
            Assert.Empty(context.SaveLibrary.Library.Slots);
            Assert.NotNull(context.SaveLibrary.Library.WorkingSlot);
        }

        [Fact]
        public async Task FileExportSanitizesTextDownloadNames()
        {
            InMemoryJsRuntime jsRuntime = new();
            FileExportService exports = new(jsRuntime);

            await exports.DownloadTextAsync("summary/campaign.txt", "summary");

            var download = Assert.Single(jsRuntime.Downloads);
            Assert.Equal("summary-campaign.txt", download.FileName);
            Assert.Equal("text/plain;charset=utf-8", download.ContentType);
            Assert.Equal("summary", download.Content);
        }

        [Fact]
        public async Task FileExportSerializesJsonPayloads()
        {
            InMemoryJsRuntime jsRuntime = new();
            FileExportService exports = new(jsRuntime);

            await exports.DownloadJsonAsync("campaign/save.json", new { Id = "run-1" });

            var download = Assert.Single(jsRuntime.Downloads);
            Assert.Equal("campaign-save.json", download.FileName);
            Assert.Equal("application/json;charset=utf-8", download.ContentType);
            Assert.Contains("\"id\": \"run-1\"", download.Content);
        }

        [Fact]
        public void ToastServiceIgnoresBlankMessages()
        {
            ToastService toasts = new();
            int changedCount = 0;
            toasts.Changed += () => changedCount++;

            toasts.Show(" ");

            Assert.Empty(toasts.Messages);
            Assert.Equal(0, changedCount);
        }

        [Fact]
        public void ToastServiceNotifiesWhenMessageIsShown()
        {
            ToastService toasts = new();
            int changedCount = 0;
            toasts.Changed += () => changedCount++;

            toasts.Show("Saved", autoCloseMilliseconds: null);

            Assert.Equal("Saved", Assert.Single(toasts.Messages).Message);
            Assert.Equal(1, changedCount);
        }

        [Fact]
        public void ToastServiceDoesNotNotifyWhenUnknownMessageIsDismissed()
        {
            ToastService toasts = new();
            toasts.Show("Saved", autoCloseMilliseconds: null);
            int changedCount = 0;
            toasts.Changed += () => changedCount++;

            toasts.Dismiss(Guid.NewGuid());

            Assert.Single(toasts.Messages);
            Assert.Equal(0, changedCount);
        }

        [Fact]
        public void ToastServiceNotifiesWhenMessageIsDismissed()
        {
            ToastService toasts = new();
            toasts.Show("Saved", autoCloseMilliseconds: null);
            ToastMessage message = Assert.Single(toasts.Messages);
            int changedCount = 0;
            toasts.Changed += () => changedCount++;

            toasts.Dismiss(message.Id);

            Assert.Empty(toasts.Messages);
            Assert.Equal(1, changedCount);
        }

    }