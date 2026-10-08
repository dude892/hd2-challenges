namespace Hd2Challenges.Models;

public sealed class RedemptionDifficultyDefinition
{
    public string Id { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public int StartOperationIndex { get; set; } = 1;
    public int ScoreModifier { get; set; }
    public bool IsSuper { get; set; }
    public RedemptionStarterLoadoutDefinition? LoadoutOverrides { get; set; }
    public RedemptionStarterLoadoutDefinition? LoadoutAdditions { get; set; }
}
