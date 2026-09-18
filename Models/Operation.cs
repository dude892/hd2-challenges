namespace Hd2Challenges.Models;

public sealed class Operation(OperationDefinition definition, int missionNumber = 1)
{
    public string Id { get; } = definition.Id;
    public string DisplayName { get; } = definition.DisplayName;
    public int MaxStars { get; } = definition.MaxStars;
    public int MissionCount { get; } = definition.MissionCount;
    public int MissionNumber { get; set; } = missionNumber;

    public string Label => $"{DisplayName}: Mission {MissionNumber}";
    public bool IsComplete => MissionNumber == MissionCount;
}
