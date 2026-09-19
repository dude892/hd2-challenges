using System.Text.Json;
using Hd2Challenges.Models;

namespace Hd2Challenges.Services;

public sealed class SaveLibraryService(BrowserStorageService storage)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    public Task<SaveLibrary<TState>?> LoadAsync<TState>(string key) => storage.GetAsync<SaveLibrary<TState>>(key);

    public Task PersistAsync<TState>(string key, SaveLibrary<TState> library) => storage.SetAsync(key, library);

    public void CommitWorkingSlot<TState>(SaveLibrary<TState> library)
    {
        if (library.WorkingSlot is null)
        {
            return;
        }

        library.Slots.Add(library.WorkingSlot);
        library.CurrentSlotId = library.WorkingSlot.Id;
        library.WorkingSlot = null;
    }

    public void AddSlot<TState>(SaveLibrary<TState> library, SaveSlot<TState> slot)
    {
        library.WorkingSlot = null;
        library.Slots.Add(slot);
        library.CurrentSlotId = slot.Id;
    }

    public SaveSlot<TState>? SelectSlot<TState>(SaveLibrary<TState> library, Guid slotId)
    {
        library.WorkingSlot = null;
        library.CurrentSlotId = slotId;
        return library.CurrentSlot;
    }

    public void RenameCurrentSlot<TState>(SaveLibrary<TState> library, string? name)
    {
        if (library.CurrentSlot is null || string.IsNullOrWhiteSpace(name))
        {
            return;
        }

        library.CurrentSlot.Name = name;
        library.CurrentSlot.UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void DeleteSlot<TState>(SaveLibrary<TState> library, Guid slotId, Func<SaveSlot<TState>> replacementFactory)
    {
        library.WorkingSlot = null;
        library.Slots.RemoveAll(slot => slot.Id == slotId);

        if (library.Slots.Count == 0)
        {
            library.WorkingSlot = replacementFactory();
            library.CurrentSlotId = null;
        }
        else if (library.CurrentSlotId == slotId)
        {
            library.CurrentSlotId = library.Slots[0].Id;
        }
    }

    public SaveExport<TState> CreateExport<TState>(string mode, SaveSlot<TState> slot) => new() { Mode = mode, Slot = slot };

    public SaveSlot<TState>? ImportSlot<TState>(string json, string mode)
    {
        SaveExport<TState>? import = JsonSerializer.Deserialize<SaveExport<TState>>(json, JsonOptions);
        if (import?.Slot is not { } slot || import.Mode != mode || EqualityComparer<TState>.Default.Equals(slot.State, default))
        {
            return null;
        }

        slot.Id = Guid.NewGuid();
        slot.UpdatedAt = DateTimeOffset.UtcNow;
        return slot;
    }
}