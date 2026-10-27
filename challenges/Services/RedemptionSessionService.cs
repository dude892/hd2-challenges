using System.Diagnostics.CodeAnalysis;
using Hd2Challenges.Models;

namespace Hd2Challenges.Services;

/// <summary>
/// Owns the active runtime state, resolves saved definitions through the catalog,
/// and coordinates run lifecycle and persistence.
/// Mission rules and presentation remain outside the session.
/// </summary>
public sealed class RedemptionSessionService(
    CatalogService baseCatalog,
    RedemptionCatalogService redemptionCatalog,
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

    public async Task LoadAsync()
    {
        await redemptionCatalog.EnsureLoadedAsync();
        await saves.LoadAsync();

        if (saves.Library.CurrentSlotId is null || saves.Library.CurrentSlot is null && saves.Library.Slots.Count == 0)
        {
            _state = redemptionCatalog.CreateRedemptionSetupState(redemptionCatalog.GetDifficulty("normal"), baseCatalog.GetAllWarbonds());
            saves.SetWorkingSlot(_state.ToDefinition(), _state.Difficulty.DisplayName);
            
            return;
        }

        SaveSlot<RedemptionStateDefinition> slot = saves.Library.CurrentSlot ?? saves.Library.Slots.First();
        if (saves.Library.CurrentSlotId != slot.Id)
        {
            slot = await saves.SelectSlotAsync(slot.Id);
        }

        _state = redemptionCatalog.CreateRedemptionState(slot.State);
    }

    public void ChangeDifficulty(string difficultyId) => State.ChangeDifficulty(redemptionCatalog.GetDifficulty(difficultyId));

    public bool SetWarbondSelected(Warbond warbond, bool selected) => State.SetWarbondSelected(warbond, selected);

    public async Task BeginRunAsync()
    {
        EnsureLoaded();
        if (saves.Library.WorkingSlot is null)
        {
            return;
        }

        State.BeginRun();
        await saves.CommitWorkingSlotAsync(State.ToDefinition(), State.Difficulty.DisplayName);
    }

    public Task PersistAsync() => saves.PersistAsync(State.ToDefinition());

    public Task CreateSnapshotAsync() => State.RunStarted
        ? saves.CreateSnapshotAsync(State.ToDefinition(), State.Difficulty.DisplayName)
        : Task.CompletedTask;

    public async Task RestartAsync()
    {
        RedemptionState setup = redemptionCatalog.CreateRedemptionSetupState(State.Difficulty, State.SelectedWarbonds);
        await saves.ResetWorkingSlotAsync(setup.ToDefinition(), setup.Difficulty.DisplayName);
        _state = setup;
    }

    public async Task SelectSlotAsync(Guid slotId)
    {
        EnsureLoaded();
        SaveSlot<RedemptionStateDefinition> slot = await saves.SelectSlotAsync(slotId);
        _state = redemptionCatalog.CreateRedemptionState(slot.State);
    }

    public async Task<bool> DeleteSlotAsync(Guid slotId)
    {
        RedemptionState current = State;
        RedemptionState? replacement = null;
        bool deleted = await saves.DeleteSlotAsync(slotId, () =>
        {
            replacement = redemptionCatalog.CreateRedemptionSetupState(current.Difficulty, current.SelectedWarbonds);
            return replacement.ToDefinition();
        }, current.Difficulty.DisplayName);
        if (replacement is not null)
        {
            _state = replacement;
        }

        return deleted;
    }

    public async Task<SaveSlot<RedemptionStateDefinition>?> ImportAsync(string json)
    {
        EnsureLoaded();
        
        var result = await saves.ImportAsync(json);

        if (result is null)
        {
            return null;
        }

        _state = redemptionCatalog.CreateRedemptionState(result.State);
        return result;
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
