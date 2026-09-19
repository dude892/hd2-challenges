namespace Hd2Challenges.Models;

public sealed class PenitentDifficultyDefinition
{
    public string Id { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public int StartOperationIndex { get; set; } = 1;
    public int ScoreModifier { get; set; }
    public bool IsSuper { get; set; }
    public PenitentStarterLoadoutDefinition? LoadoutOverrides { get; set; }
    public PenitentStarterLoadoutDefinition? LoadoutAdditions { get; set; }
}
