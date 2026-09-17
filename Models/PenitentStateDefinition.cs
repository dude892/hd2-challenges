namespace Hd2Challenges.Models;

public sealed class PenitentStateDefinition
{
    public string DifficultyId { get; set; } = "normal";
    public string OperationId { get; set; } = "";
    public int MissionNumber { get; set; }
    public int MissionsFailed { get; set; }
    public List<string> SelectedWarbonds { get; set; } = [];
    public List<string> AcquiredItemInternalNames { get; set; } = [];
    public List<string> BannedItemInternalNames { get; set; } = [];
    public List<string> PendingRewardInternalNames { get; set; } = [];
    public List<string> PendingPunishmentInternalNames { get; set; } = [];
}
