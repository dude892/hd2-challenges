namespace Hd2Challenges.Models;

public sealed class PenitentStateDefinition
{
    public string DifficultyId { get; set; } = "normal";
    public int OperationIndex { get; set; } = 3;
    public int MissionNumber { get; set; } = 0;
    public int MissionsFailed { get; set; } = 0;
    public List<string> SelectedWarbonds { get; set; } = [];
    public List<string> AcquiredItemIds { get; set; } = [];
    public List<string> BannedItemIds { get; set; } = [];
    public List<string> PendingRewardIds { get; set; } = [];
    public List<string> PendingPunishmentIds { get; set; } = [];
}
