namespace Hd2Challenges.Models;

public sealed class PenitentState(
    PenitentStateDefinition definition,
    Func<string, PenitentDifficulty> resolveDifficulty,
    Func<string, int, Operation> resolveOperation,
    Func<IEnumerable<string>, ItemSet> resolveItemSet,
    Func<string, ItemSet> resolveStarterItems,
    Func<IEnumerable<string>, List<Warbond>> resolveWarbonds
)
{
    public PenitentDifficulty Difficulty { get; } = resolveDifficulty(definition.DifficultyId);
    public int MissionsFailed { get; set; } = definition.MissionsFailed;
    public ItemSet BannedItems { get; set; } = resolveItemSet(definition.BannedItemIds);
    public List<Warbond> SelectedWarbonds { get; set; } = resolveWarbonds(definition.SelectedWarbonds);
    public Operation CurrentOperation { get; set; } = resolveOperation(definition.OperationId, definition.MissionNumber);
    public ItemSet StarterItems { get; } = resolveStarterItems(definition.DifficultyId);
    public ItemSet AcquiredItems { get; set; } = resolveItemSet(definition.AcquiredItemIds);
    public ItemSet PendingRewardItems { get; set; } = resolveItemSet(definition.PendingRewardIds);
    public ItemSet PendingPunishmentItems { get; set; } = resolveItemSet(definition.PendingPunishmentIds);

    public bool CanEditPenitentSetup => CurrentOperation.Id == Difficulty.StartOperation.Id && CurrentOperation.IsFirstMission;

    public PenitentStateDefinition ToDefinition() => new()
    {
        DifficultyId = Difficulty.Id,
        OperationId = CurrentOperation.Id,
        MissionNumber = CurrentOperation.MissionNumber,
        MissionsFailed = MissionsFailed,
        SelectedWarbonds = [.. SelectedWarbonds.Select(item => item.Id)],
        AcquiredItemIds = [.. AcquiredItems.Select(item => item.Id)],
        BannedItemIds = [.. BannedItems.Select(item => item.Id)],
        PendingRewardIds = [.. PendingRewardItems.Select(item => item.Id)],
        PendingPunishmentIds = [.. PendingPunishmentItems.Select(item => item.Id)],
    };
}
