namespace Hd2Challenges.Models;

public sealed class PenitentState(
    PenitentStateDefinition definition,
    Func<string, PenitentDifficulty> resolveDifficulty,
    Func<string, int, Operation> resolveOperation,
    Func<IEnumerable<string>, ItemSet> resolveItemSet,
    Func<string, ItemSet> resolveStarterItems,
    Func<IEnumerable<string>, List<Warbond>> resolveWarbonds)
{
    public PenitentDifficulty Difficulty => resolveDifficulty(definition.DifficultyId);
    public int MissionsFailed { get; set; } = definition.MissionsFailed;
    public ItemSet BannedItems { get; set; } = resolveItemSet(definition.BannedItemInternalNames);
    public List<Warbond> SelectedWarbonds { get; set; } = resolveWarbonds(definition.SelectedWarbonds);

    public Operation CurrentOperation => resolveOperation(definition.OperationId, definition.MissionNumber);
    public ItemSet StarterItems { get; init; } = resolveStarterItems(definition.DifficultyId);
    public ItemSet AcquiredItems => resolveItemSet(definition.AcquiredItemInternalNames);
    public ItemSet PendingRewardItems => resolveItemSet(definition.PendingRewardInternalNames);
    public ItemSet PendingPunishmentItems => resolveItemSet(definition.PendingPunishmentInternalNames);

    public PenitentStateDefinition ToDefinition() => new()
    {
        DifficultyId = Difficulty.Id,
        OperationId = CurrentOperation.Id,
        MissionNumber = CurrentOperation.MissionNumber,
        MissionsFailed = MissionsFailed,
        SelectedWarbonds = [.. SelectedWarbonds.Select(item => item.InternalName)],
        AcquiredItemInternalNames = [.. AcquiredItems.Select(item => item.InternalName)],
        BannedItemInternalNames = [.. BannedItems.Select(item => item.InternalName)],
        PendingRewardInternalNames = [.. PendingRewardItems.Select(item => item.InternalName)],
        PendingPunishmentInternalNames = [.. PendingPunishmentItems.Select(item => item.InternalName)],
    };
}
