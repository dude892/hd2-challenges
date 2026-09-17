namespace Hd2Challenges.Models;

public sealed class OperationDefinition
{
    public string Id { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public int MaxStars { get; set; } = 1;
    public int MissionCount { get; set; } = 1;
}
