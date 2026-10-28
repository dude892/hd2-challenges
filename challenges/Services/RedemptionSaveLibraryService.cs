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
        for (int index = 0; index < Library.Slots.Count; index++)
        {
            SaveSlot<RedemptionStateDefinition> slot = Library.Slots[index];
            SaveSlot<RedemptionStateDefinition> migratedSlot = MoveLegacyCompletionToSlot(slot);
            if (migratedSlot != slot)
            {
                Library.Slots[index] = migratedSlot;
                migrated = true;
            }
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
        if (Library.WorkingSlot is not { } workingSlot)
        {
            return;
        }

        Library.WorkingSlot = workingSlot with
        {
            State = definition,
            Status = status,
            Name = BuildSlotName(difficultyName)
        };
        Library.CommitWorkingSlot();
        await PersistAsync();
    }

    public async Task PersistAsync(RedemptionStateDefinition definition, RunStatus status = RunStatus.InProgress)
    {
        if (Library.CurrentSlot is not { } currentSlot)
        {
            return;
        }

        Library.ReplaceSlot(currentSlot with
        {
            State = definition,
            Status = status,
            UpdatedAt = DateTimeOffset.UtcNow
        });
        await PersistAsync();
    }

    public void SetCurrentRunStatus(RunStatus status)
    {
        if (Library.CurrentSlot is { } currentSlot)
        {
            Library.ReplaceSlot(currentSlot with { Status = status });
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
            State = definition,
            UpdatedAt = DateTimeOffset.UtcNow,
            Status = status
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

    public async Task<SaveSlot<RedemptionStateDefinition>?> ImportAsync(
        string json,
        Action<RedemptionStateDefinition>? validateDefinition = null)
    {
        SaveSlot<RedemptionStateDefinition>? slot = saveLibrary.ImportSlot<RedemptionStateDefinition>(json, ExportMode);
        if (slot is null)
        {
            return null;
        }

        slot = MoveLegacyCompletionToSlot(slot);
        validateDefinition?.Invoke(slot.State);
        Library.AddSlot(slot);
        await PersistAsync();
        return slot;
    }

    private static SaveSlot<RedemptionStateDefinition> CreateWorkingSlot(RedemptionStateDefinition definition, string difficultyName) => new()
    {
        Name = BuildSlotName(difficultyName),
        State = definition,
        Status = RunStatus.InProgress
    };

    private static SaveSlot<RedemptionStateDefinition> MoveLegacyCompletionToSlot(SaveSlot<RedemptionStateDefinition> slot)
    {
        if (slot.State?.RunCompleted is not { } legacyRunCompleted)
        {
            return slot;
        }

        return slot with
        {
            Status = legacyRunCompleted ? RunStatus.Completed : slot.Status,
            State = slot.State with { RunCompleted = null }
        };
    }

    private static string BuildSlotName(string difficultyName) => $"{difficultyName} | {DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss}";

    public SaveSlot<RedemptionStateDefinition>? GetSlot(Guid slotId) => Library.GetSlot(slotId);
}