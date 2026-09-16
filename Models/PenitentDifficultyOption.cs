namespace Hd2Challenges.Models;

public sealed class PenitentDifficultyOption
{
    public string Id { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public int StartMission { get; set; } = 1;
    public int ScoreModifier { get; set; }
    public bool IsSuper { get; set; }
    public PenitentStarterLoadout? LoadoutOverrides { get; set; }
    public PenitentStarterLoadout? LoadoutAdditions { get; set; }
}
