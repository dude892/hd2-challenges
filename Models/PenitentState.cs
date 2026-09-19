namespace Hd2Challenges.Models;

public sealed class PenitentState(
    PenitentStateDefinition definition,
    Func<string, PenitentDifficulty> resolveDifficulty,
    Func<int, int, Operation> resolveOperation,
    Func<IEnumerable<string>, ItemSet> resolveItemSet,
    Func<string, ItemSet> resolveStarterItems,
    Func<IEnumerable<string>, HashSet<Warbond>> resolveWarbonds
)
{
    public bool RunStarted { get; set; } = true;
    public PenitentDifficulty Difficulty { get; } = resolveDifficulty(definition.DifficultyId);
    public HashSet<Warbond> SelectedWarbonds { get; set; } = resolveWarbonds(definition.SelectedWarbonds);
    public ItemSet BannedItems { get; set; } = resolveItemSet(definition.BannedItemIds);
    public ItemSet StarterItems => new(resolveStarterItems(Difficulty.Id));
    public ItemSet AcquiredItems { get; set; } = resolveItemSet(definition.AcquiredItemIds);
    public ItemSet PendingRewardItems { get; set; } = resolveItemSet(definition.PendingRewardIds);
    public ItemSet PendingPunishmentItems { get; set; } = resolveItemSet(definition.PendingPunishmentIds);
    public Operation CurrentOperation { get; set; } = resolveOperation(definition.OperationIndex, definition.MissionNumber);
    public int MissionsFailed { get; set; } = definition.MissionsFailed;
    public bool RunCompleted { get; set; } = definition.RunCompleted;

    public bool CanCompleteMission => !RunCompleted && PendingPunishmentItems.Count == 0;
    public bool CanFailMission => !RunCompleted && PendingRewardItems.Count == 0;

    public PenitentStateDefinition ToDefinition() => new()
    {
        DifficultyId = Difficulty.Id,
        OperationIndex = CurrentOperation.DifficultyIndex,
        MissionNumber = CurrentOperation.MissionNumber,
        MissionsFailed = MissionsFailed,
        RunCompleted = RunCompleted,
        SelectedWarbonds = [.. SelectedWarbonds.Select(item => item.Id)],
        AcquiredItemIds = [.. AcquiredItems.Select(item => item.Id)],
        BannedItemIds = [.. BannedItems.Select(item => item.Id)],
        PendingRewardIds = [.. PendingRewardItems.Select(item => item.Id)],
        PendingPunishmentIds = [.. PendingPunishmentItems.Select(item => item.Id)],
    };
}
