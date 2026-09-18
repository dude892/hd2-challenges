namespace Hd2Challenges.Models;

public sealed class PenitentState(
    PenitentStateDefinition definition,
    Func<string, PenitentDifficulty> resolveDifficulty,
    Func<string, int, Operation> resolveOperation,
    Func<IEnumerable<string>, ItemSet> resolveItemSet,
    Func<string, ItemSet> resolveStarterItems,
    Func<IEnumerable<string>, HashSet<Warbond>> resolveWarbonds,
    Func<IEnumerable<Warbond>, ItemSet> resolveAvailableItems
)
{
    public bool RunStarted { get; set; } = true;
    public PenitentDifficulty Difficulty { get; } = resolveDifficulty(definition.DifficultyId);
    public HashSet<Warbond> SelectedWarbonds { get; set; } = resolveWarbonds(definition.SelectedWarbonds);
    public ItemSet AvailableItems => resolveAvailableItems(SelectedWarbonds);
    public ItemSet BannedItems { get; set; } = resolveItemSet(definition.BannedItemIds);
    public ItemSet StarterItems => new(resolveStarterItems(Difficulty.Id).Where(AvailableItems.ContainsItem));
    public ItemSet AcquiredItems { get; set; } = resolveItemSet(definition.AcquiredItemIds);
    public ItemSet PendingRewardItems { get; set; } = resolveItemSet(definition.PendingRewardIds);
    public ItemSet PendingPunishmentItems { get; set; } = resolveItemSet(definition.PendingPunishmentIds);
    public Operation CurrentOperation { get; set; } = resolveOperation(definition.OperationId, definition.MissionNumber);
    public int MissionsFailed { get; set; } = definition.MissionsFailed;

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
