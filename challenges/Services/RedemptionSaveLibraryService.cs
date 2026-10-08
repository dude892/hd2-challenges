using Hd2Challenges.Models;

namespace Hd2Challenges.Services;

public sealed class RedemptionSaveLibraryService(
    SaveLibraryService saveLibrary,
    RedemptionCatalogService redemptionCatalog,
    CatalogService catalog)
{
    private const string StorageKey = "blazor-warpath-redemption";
    private const string ExportMode = "warpath-redemption";

    public SaveLibrary<RedemptionStateDefinition> Library { get; private set; } = new();

    private Task PersistAsync() => saveLibrary.PersistAsync(StorageKey, Library);

    public async Task<RedemptionState> LoadAsync()
    {
        Library = await saveLibrary.LoadAsync<RedemptionStateDefinition>(StorageKey) ?? new();

        if (Library.CurrentSlotId is null || Library.CurrentSlot is null && Library.Slots.Count == 0)
        {
            Library.WorkingSlot = CreateWorkingSlot("normal", catalog.GetAllWarbonds());
        }

        return ResolveCurrentState();
    }

    public async Task BeginRun(RedemptionState state)
    {
        if (Library.WorkingSlot is null)
        {
            return;
        }

        state.RunStarted = true;
        state.AcquiredItems.AddItems(state.StarterItems);
        Library.WorkingSlot.State = state.ToDefinition();
        Library.WorkingSlot.Name = BuildSlotName(state);
        Library.CommitWorkingSlot();
        await PersistAsync();
    }

    public async Task PersistAsync(RedemptionState state)
    {
        if (Library.CurrentSlot is null)
        {
            return;
        }

        Library.CurrentSlot.State = state.ToDefinition();
        Library.CurrentSlot.UpdatedAt = DateTimeOffset.UtcNow;
        await PersistAsync();
    }

    public async Task<RedemptionState> RestartAsync(RedemptionState state)
    {
        RedemptionState restartedState = redemptionCatalog.CreateRedemptionSetupState(state.Difficulty.Id, state.SelectedWarbonds);
        Library.CurrentSlotId = null;
        Library.WorkingSlot = CreateWorkingSlot(restartedState);
        await PersistAsync();
        return restartedState;
    }

    public async Task CreateSnapshotAsync(RedemptionState state)
    {
        if (!state.RunStarted)
        {
            return;
        }

        SaveSlot<RedemptionStateDefinition> slot = new()
        {
            Name = $"{BuildSlotName(state)} Snapshot",
            State = state.ToDefinition(),
            UpdatedAt = DateTimeOffset.UtcNow
        };

        Library.AddSlot(slot);
        await PersistAsync(state);
    }

    public async Task<bool> RenameSlotAsync(Guid slotId, string? name)
    {
        if (!Library.RenameSlot(slotId, name))
        {
            return false;
        }

        await PersistAsync();
        return true;
    }

    public async Task<RedemptionState> SelectSlotAsync(Guid slotId)
    {
        Library.SelectSlot(slotId);
        RedemptionState state = ResolveCurrentState();
        await PersistAsync();
        return state;
    }

    public async Task<RedemptionState> DeleteSlotAsync(Guid slotId)
    {
        Library.DeleteSlot(slotId, () => CreateWorkingSlot("normal", catalog.GetAllWarbonds()));
        RedemptionState state = ResolveCurrentState();
        await PersistAsync();
        return state;
    }

    public SaveExport<RedemptionStateDefinition> CreateExport(Guid slotId)
    {
        SaveSlot<RedemptionStateDefinition> slot = Library.Slots.First(entry => entry.Id == slotId);
        return saveLibrary.CreateExport(ExportMode, slot);
    }

    public async Task<(SaveSlot<RedemptionStateDefinition> Slot, RedemptionState State)?> ImportAsync(string json)
    {
        SaveSlot<RedemptionStateDefinition>? slot = saveLibrary.ImportSlot<RedemptionStateDefinition>(json, ExportMode);
        if (slot is null)
        {
            return null;
        }

        Library.AddSlot(slot);
        RedemptionState state = ResolveCurrentState();
        await PersistAsync();
        return (slot, state);
    }

    private RedemptionState ResolveCurrentState()
    {
        SaveSlot<RedemptionStateDefinition> slot = Library.CurrentSlot ?? Library.Slots.FirstOrDefault() ?? CreateWorkingSlot("normal", catalog.GetAllWarbonds());
        if (ReferenceEquals(slot, Library.WorkingSlot))
        {
            RedemptionState setupState = redemptionCatalog.CreateRedemptionState(slot.State);
            setupState.RunStarted = false;
            return setupState;
        }

        Library.CurrentSlotId = slot.Id;
        return redemptionCatalog.CreateRedemptionState(slot.State);
    }

    private SaveSlot<RedemptionStateDefinition> CreateWorkingSlot(string difficulty, IEnumerable<Warbond> selectedWarbonds) =>
        CreateWorkingSlot(redemptionCatalog.CreateRedemptionSetupState(difficulty, selectedWarbonds));

    private SaveSlot<RedemptionStateDefinition> CreateWorkingSlot(RedemptionState state) => new()
    {
        Name = BuildSlotName(state),
        State = state.ToDefinition()
    };

    private static string BuildSlotName(RedemptionState state) => $"{state.Difficulty.DisplayName} | {DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss}";
}