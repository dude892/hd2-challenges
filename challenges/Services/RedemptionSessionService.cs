using System.Diagnostics.CodeAnalysis;
using Hd2Challenges.Models;

namespace Hd2Challenges.Services;

/// <summary>
/// Owns the active runtime state and coordinates run lifecycle and persistence.
/// Mission rules and presentation remain outside the session.
/// </summary>
public sealed class RedemptionSessionService(
    RedemptionCatalogService catalog,
    RedemptionSaveLibraryService saves)
{
    private RedemptionState? _state;

    public RedemptionState State
    {
        get
        {
            EnsureLoaded();
            return _state;
        }
    }

    public async Task LoadAsync() => _state = await saves.LoadAsync();

    public void ChangeDifficulty(string difficultyId) => State.ChangeDifficulty(catalog.GetDifficulty(difficultyId));

    public bool SetWarbondSelected(Warbond warbond, bool selected) => State.SetWarbondSelected(warbond, selected);

    public Task BeginRunAsync() => saves.BeginRun(State);

    public Task PersistAsync() => saves.PersistAsync(State);

    public Task CreateSnapshotAsync() => saves.CreateSnapshotAsync(State);

    public async Task RestartAsync() => _state = await saves.RestartAsync(State);

    public async Task SelectSlotAsync(Guid slotId)
    {
        EnsureLoaded();
        _state = await saves.SelectSlotAsync(slotId);
    }

    public async Task<bool> DeleteSlotAsync(Guid slotId)
    {
        (bool result, _state) = await saves.DeleteSlotAsync(slotId, State);

        return result;
    }

    public async Task<SaveSlot<RedemptionStateDefinition>?> ImportAsync(string json)
    {
        EnsureLoaded();
        
        var result = await saves.ImportAsync(json);

        if (result is null)
        {
            return null;
        }

        _state = result.Value.State;
        return result.Value.Slot;
    }

    [MemberNotNull(nameof(_state))]
    private void EnsureLoaded()
    {
        if (_state is null)
        {
            throw new InvalidOperationException("The Redemption session has not been loaded.");
        }
    }
}
