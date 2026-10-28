namespace Hd2Challenges.Models;

public sealed class RedemptionState
{
    private readonly Func<RedemptionDifficulty, IEnumerable<Warbond>, ItemSet> _resolveStarterItems;
    private readonly HashSet<Warbond> _selectedWarbonds;

    public RedemptionState(
        RedemptionDifficulty difficulty,
        IEnumerable<Warbond> selectedWarbonds,
        Func<RedemptionDifficulty, IEnumerable<Warbond>, ItemSet> resolveStarterItems)
    {
        Difficulty = difficulty;
        _selectedWarbonds = [.. selectedWarbonds];
        CurrentOperation = difficulty.StartOperation.Clone();
        _resolveStarterItems = resolveStarterItems;
        StarterItems = _resolveStarterItems(Difficulty, _selectedWarbonds);
        RunStarted = false;
    }

    public RedemptionState(
        RedemptionStateDefinition definition,
        Func<string, RedemptionDifficulty> resolveDifficulty,
        Func<int, int, Operation> resolveOperation,
        Func<IEnumerable<string>, ItemSet> resolveItemSet,
        Func<RedemptionDifficulty, IEnumerable<Warbond>, ItemSet> resolveStarterItems,
        Func<IEnumerable<string>, HashSet<Warbond>> resolveWarbonds)
    {
        Difficulty = resolveDifficulty(definition.DifficultyId);
        _selectedWarbonds = resolveWarbonds(definition.SelectedWarbonds);
        BannedItems = resolveItemSet(definition.BannedItemIds);
        AcquiredItems = resolveItemSet(definition.AcquiredItemIds);
        PendingRewardItems = resolveItemSet(definition.PendingRewardIds);
        PendingPunishmentItems = resolveItemSet(definition.PendingPunishmentIds);
        CurrentOperation = resolveOperation(definition.OperationIndex, definition.MissionNumber);
        MissionsFailed = definition.MissionsFailed;
        _resolveStarterItems = resolveStarterItems;
        StarterItems = _resolveStarterItems(Difficulty, _selectedWarbonds);
    }

    public bool RunStarted { get; set; } = true;
    public RedemptionDifficulty Difficulty { get; private set; }
    public IReadOnlySet<Warbond> SelectedWarbonds => _selectedWarbonds;
    public ItemSet BannedItems { get; } = new();
    public ItemSet AcquiredItems { get; } = new();
    public ItemSet PendingRewardItems { get; set; } = new();
    public ItemSet PendingPunishmentItems { get; set; } = new();
    public Operation CurrentOperation { get; set; }
    public int MissionsFailed { get; set; }

    public ItemSet StarterItems { get; private set; }

    public void BeginRun()
    {
        RunStarted = true;
        AcquiredItems.AddItems(StarterItems);
    }

    public void ChangeDifficulty(RedemptionDifficulty difficulty)
    {
        if (RunStarted)
        {
            throw new InvalidOperationException("Difficulty cannot be changed after the run has started.");
        }

        Difficulty = difficulty;
        CurrentOperation = difficulty.StartOperation.Clone();
        StarterItems = _resolveStarterItems(difficulty, _selectedWarbonds);
    }

    public bool SetWarbondSelected(Warbond warbond, bool isSelected)
    {
        if (warbond.AlwaysEnabled)
        {
            return false;
        }

        bool changed = isSelected ? _selectedWarbonds.Add(warbond) : _selectedWarbonds.Remove(warbond);

        if (changed)
        {
            StarterItems = _resolveStarterItems(Difficulty, _selectedWarbonds);
        }

        return changed;
    }

    public RedemptionStateDefinition ToDefinition() => new()
    {
        DifficultyId = Difficulty.Id,
        OperationIndex = CurrentOperation.DifficultyIndex,
        MissionNumber = CurrentOperation.MissionNumber,
        MissionsFailed = MissionsFailed,
        SelectedWarbonds = [.. SelectedWarbonds.Select(item => item.Id)],
        AcquiredItemIds = [.. AcquiredItems.Select(item => item.Id)],
        BannedItemIds = [.. BannedItems.Select(item => item.Id)],
        PendingRewardIds = [.. PendingRewardItems.Select(item => item.Id)],
        PendingPunishmentIds = [.. PendingPunishmentItems.Select(item => item.Id)],
    };
}
