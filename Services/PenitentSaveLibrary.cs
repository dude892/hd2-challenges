using Hd2Challenges.Models;

namespace Hd2Challenges.Services;

public sealed class PenitentSaveLibrary(
    SaveLibraryService saveLibrary,
    PenitentCatalogService penitentCatalog,
    CatalogService catalog)
{
    private const string StorageKey = "blazor-penitent-crusade";
    private const string ExportMode = "penitent-crusade";

    public SaveLibrary<PenitentStateDefinition> Library { get; private set; } = new();

    public async Task<PenitentState> LoadAsync()
    {
        Library = await saveLibrary.LoadAsync<PenitentStateDefinition>(StorageKey) ?? new();

        if (Library.CurrentSlotId is null || Library.CurrentSlot is null && Library.Slots.Count == 0)
        {
            Library.WorkingSlot = CreateWorkingSlot("normal", catalog.GetAllWarbonds());
        }

        return ResolveCurrentState();
    }

    public void BeginRun(PenitentState state)
    {
        if (Library.WorkingSlot is null)
        {
            return;
        }

        Library.WorkingSlot.State = state.ToDefinition();
        saveLibrary.CommitWorkingSlot(Library);
    }

    public async Task PersistAsync(PenitentState state)
    {
        if (Library.CurrentSlot is null)
        {
            return;
        }

        Library.CurrentSlot.State = state.ToDefinition();
        Library.CurrentSlot.UpdatedAt = DateTimeOffset.UtcNow;
        await saveLibrary.PersistAsync(StorageKey, Library);
    }

    public async Task<PenitentState> RestartAsync(PenitentState state)
    {
        PenitentState restartedState = penitentCatalog.CreatePenitentSetupState(state.Difficulty.Id, state.SelectedWarbonds);
        Library.CurrentSlotId = null;
        Library.WorkingSlot = CreateWorkingSlot(restartedState);
        await saveLibrary.PersistAsync(StorageKey, Library);
        return restartedState;
    }

    public async Task CreateSnapshotAsync(PenitentState state)
    {
        if (!state.RunStarted)
        {
            return;
        }

        SaveSlot<PenitentStateDefinition> slot = new()
        {
            Name = $"{BuildSlotName(state)} Snapshot",
            State = state.ToDefinition(),
            UpdatedAt = DateTimeOffset.UtcNow
        };

        saveLibrary.AddSlot(Library, slot);
        await PersistAsync(state);
    }

    public async Task RenameCurrentSlotAsync(string? name)
    {
        saveLibrary.RenameCurrentSlot(Library, name);
        await saveLibrary.PersistAsync(StorageKey, Library);
    }

    public async Task<PenitentState> SelectSlotAsync(Guid slotId)
    {
        saveLibrary.SelectSlot(Library, slotId);
        PenitentState state = ResolveCurrentState();
        await saveLibrary.PersistAsync(StorageKey, Library);
        return state;
    }

    public async Task<PenitentState> DeleteSlotAsync(Guid slotId)
    {
        saveLibrary.DeleteSlot(Library, slotId, () => CreateWorkingSlot("normal", catalog.GetAllWarbonds()));
        PenitentState state = ResolveCurrentState();
        await saveLibrary.PersistAsync(StorageKey, Library);
        return state;
    }

    public SaveExport<PenitentStateDefinition> CreateExport(Guid slotId)
    {
        SaveSlot<PenitentStateDefinition> slot = Library.Slots.First(entry => entry.Id == slotId);
        return saveLibrary.CreateExport(ExportMode, slot);
    }

    public async Task<(SaveSlot<PenitentStateDefinition> Slot, PenitentState State)?> ImportAsync(string json)
    {
        SaveSlot<PenitentStateDefinition>? slot = saveLibrary.ImportSlot<PenitentStateDefinition>(json, ExportMode);
        if (slot is null)
        {
            return null;
        }

        saveLibrary.AddSlot(Library, slot);
        PenitentState state = ResolveCurrentState();
        await saveLibrary.PersistAsync(StorageKey, Library);
        return (slot, state);
    }

    private PenitentState ResolveCurrentState()
    {
        SaveSlot<PenitentStateDefinition> slot = Library.CurrentSlot ?? Library.Slots.FirstOrDefault() ?? CreateWorkingSlot("normal", catalog.GetAllWarbonds());
        if (ReferenceEquals(slot, Library.WorkingSlot))
        {
            PenitentState setupState = penitentCatalog.CreatePenitentState(slot.State);
            setupState.RunStarted = false;
            return setupState;
        }

        Library.CurrentSlotId = slot.Id;
        return penitentCatalog.CreatePenitentState(slot.State);
    }

    private SaveSlot<PenitentStateDefinition> CreateWorkingSlot(string difficulty, IEnumerable<Warbond> selectedWarbonds) =>
        CreateWorkingSlot(penitentCatalog.CreatePenitentSetupState(difficulty, selectedWarbonds));

    private SaveSlot<PenitentStateDefinition> CreateWorkingSlot(PenitentState state) => new()
    {
        Name = BuildSlotName(state),
        State = state.ToDefinition()
    };

    private static string BuildSlotName(PenitentState state) =>
        $"{state.Difficulty.DisplayName} | {DateTimeOffset.UtcNow:yyyy-MM-dd HH:mm:ss}";
}