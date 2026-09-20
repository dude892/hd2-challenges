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