using Hd2Challenges.Models;

namespace Hd2Challenges.Services;

/// <summary>
/// Manages save slots and definition-based persistence without constructing or mutating runtime runs.
/// </summary>
public sealed class RedemptionSaveLibraryService(SaveLibraryService saveLibrary)
{
    private const string StorageKey = "blazor-warpath-redemption";
    private const string ExportMode = "warpath-redemption";

    public SaveLibrary<RedemptionStateDefinition> Library { get; private set; } = new();

    private Task PersistAsync() => saveLibrary.PersistAsync(StorageKey, Library);

    public async Task LoadAsync()
    {
        Library = await saveLibrary.LoadAsync<RedemptionStateDefinition>(StorageKey) ?? new();
        bool migrated = false;
        foreach (SaveSlot<RedemptionStateDefinition> slot in Library.Slots)
        {
            migrated |= MoveLegacyCompletionToSlot(slot);
        }

        if (migrated)
        {
            await PersistAsync();
        }
    }

    public void SetWorkingSlot(RedemptionStateDefinition definition, string difficultyName)
    {
        Library.CurrentSlotId = null;
        Library.WorkingSlot = CreateWorkingSlot(definition, difficultyName);
    }

    public async Task CommitWorkingSlotAsync(
        RedemptionStateDefinition definition,
        string difficultyName,
        RunStatus status = RunStatus.InProgress)
    {
        if (Library.WorkingSlot is null)
        {
            return;
        }

        Library.WorkingSlot.State = ClearLegacyCompletion(definition);
        Library.WorkingSlot.Status = definition.RunCompleted is true ? RunStatus.Completed : status;
        Library.WorkingSlot.Name = BuildSlotName(difficultyName);
        Library.CommitWorkingSlot();
        await PersistAsync();
    }

    public async Task PersistAsync(RedemptionStateDefinition definition, RunStatus status = RunStatus.InProgress)
    {
        if (Library.CurrentSlot is null)
        {
            return;
        }

        Library.CurrentSlot.State = ClearLegacyCompletion(definition);
        Library.CurrentSlot.Status = definition.RunCompleted is true ? RunStatus.Completed : status;
        Library.CurrentSlot.UpdatedAt = DateTimeOffset.UtcNow;
        await PersistAsync();
    }

    public void SetCurrentRunStatus(RunStatus status)
    {
        if (Library.CurrentSlot is not null)
        {
            Library.CurrentSlot.Status = status;
        }
    }

    public async Task ResetWorkingSlotAsync(RedemptionStateDefinition definition, string difficultyName)
    {
        SetWorkingSlot(definition, difficultyName);
        await PersistAsync();
    }

    public async Task CreateSnapshotAsync(
        RedemptionStateDefinition definition,
        string difficultyName,
        RunStatus status = RunStatus.InProgress)
    {
        SaveSlot<RedemptionStateDefinition> slot = new()
        {
            Name = $"{BuildSlotName(difficultyName)} Snapshot",
            State = ClearLegacyCompletion(definition),
            UpdatedAt = DateTimeOffset.UtcNow,
            Status = definition.RunCompleted is true ? RunStatus.Completed : status
        };

        Library.AddSlot(slot);
        await PersistAsync();
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

    public async Task<SaveSlot<RedemptionStateDefinition>> SelectSlotAsync(Guid slotId)
    {
        SaveSlot<RedemptionStateDefinition> slot = Library.GetSlot(slotId)
            ?? throw new InvalidOperationException($"Save slot '{slotId}' was not found.");
        Library.SelectSlot(slotId);
        await PersistAsync();
        return slot;
    }

    public async Task<bool> DeleteSlotAsync(
        Guid slotId,
        Func<RedemptionStateDefinition> replacementFactory,
        string difficultyName)
    {
        (bool deleted, _) = Library.DeleteSlot(slotId, () => CreateWorkingSlot(replacementFactory(), difficultyName));

        if (!deleted)
        {
            return false;
        }

        await PersistAsync();
        return true;
    }

    public SaveExport<RedemptionStateDefinition> CreateExport(Guid slotId)
    {
        SaveSlot<RedemptionStateDefinition> slot = Library.Slots.First(entry => entry.Id == slotId);
        return saveLibrary.CreateExport(ExportMode, slot);
    }

    public async Task<SaveSlot<RedemptionStateDefinition>?> ImportAsync(string json)
    {
        SaveSlot<RedemptionStateDefinition>? slot = saveLibrary.ImportSlot<RedemptionStateDefinition>(json, ExportMode);
        if (slot is null)
        {
            return null;
        }

        MoveLegacyCompletionToSlot(slot);
        Library.AddSlot(slot);
        await PersistAsync();
        return slot;
    }

    private static SaveSlot<RedemptionStateDefinition> CreateWorkingSlot(RedemptionStateDefinition definition, string difficultyName) => new()
    {
        Name = BuildSlotName(difficultyName),
        State = ClearLegacyCompletion(definition),
        Status = definition.RunCompleted is true ? RunStatus.Completed : RunStatus.InProgress
    };

    private static bool MoveLegacyCompletionToSlot(SaveSlot<RedemptionStateDefinition> slot)
    {
        if (slot.State?.RunCompleted is not { } legacyRunCompleted)
        {
            return false;
        }

        if (legacyRunCompleted)
        {
            slot.Status = RunStatus.Completed;
        }
        slot.State = ClearLegacyCompletion(slot.State);
        return true;
    }

    private static RedemptionStateDefinition ClearLegacyCompletion(RedemptionStateDefinition definition)
    {
        definition.RunCompleted = null;
        return definition;
    }

    private static string BuildSlotName(string difficultyName) => $"{difficultyName} | {DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss}";

    public SaveSlot<RedemptionStateDefinition>? GetSlot(Guid slotId) => Library.GetSlot(slotId);
}