namespace Hd2Challenges.Models;

public sealed class Operation(OperationDefinition definition, int missionNumber = 1)
{
    public string Id { get; } = definition.Id;
    public string DisplayName { get; } = definition.DisplayName;
    public int MaxStars { get; } = definition.MaxStars;
    public int MissionCount { get; } = definition.MissionCount;
    private int _missionNumber = Math.Clamp(missionNumber, 1, definition.MissionCount);

    public int MissionNumber
    {
        get => _missionNumber;
        set => _missionNumber = Math.Clamp(value, 1, MissionCount);
    }

    public string Label => $"{DisplayName}: Mission {MissionNumber}";
    public bool IsFirstMission => MissionNumber == 1;
    public bool IsLastMission => MissionNumber == MissionCount;
}
