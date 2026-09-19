namespace Hd2Challenges.Models;

public sealed class Operation(OperationDefinition definition, int index, int missionNumber = 1)
{
    public int DifficultyIndex { get; } = Math.Max(1, index);
    public string DisplayName { get; } = definition.DisplayName;
    public int MaxStars { get; } = definition.MaxStars;
    public int MissionCount { get; } = definition.MissionCount;
    private int _missionNumber = Math.Clamp(missionNumber, 1, definition.MissionCount);

    public int MissionNumber
    {
        get => _missionNumber;
        set => _missionNumber = Math.Clamp(value, 1, MissionCount);
    }

    public string Label => $"{DifficultyIndex} - {DisplayName} : Mission {MissionNumber} of {MissionCount}";
    public bool IsFirstMission => MissionNumber == 1;
    public bool IsLastMission => MissionNumber == MissionCount;
}
