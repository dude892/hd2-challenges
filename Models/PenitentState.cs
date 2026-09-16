namespace Hd2Challenges.Models;

public sealed class PenitentState
{
    public string Difficulty { get; set; } = "normal";
    public int MissionCounter { get; set; } = 1;
    public int SelectedStars { get; set; } = 3;
    public int TimeRemaining { get; set; }
    public int MissionsFailed { get; set; }
    public string? SpecialistId { get; set; }
    public List<string> SelectedWarbondCodes { get; set; } = [];
    public List<string> AcquiredItemInternalNames { get; set; } = [];
    public List<string> BannedItemInternalNames { get; set; } = [];
    public List<string> PendingRewardInternalNames { get; set; } = [];
    public List<string> PendingPunishmentInternalNames { get; set; } = [];
    public List<int> MissionTimes { get; set; } = [];
    public WeightProfile Weights { get; set; } = new();
    public bool IsSpecialistSelected => !string.IsNullOrWhiteSpace(SpecialistId);
}
