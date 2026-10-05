namespace Hd2Challenges.Models;

public sealed class Operation(OperationDefinition definition, int index, int missionNumber = 1)
{
    public int DifficultyIndex { get; } = Math.Max(1, index);
    public string DisplayName { get; } = definition.DisplayName;
    public int MaxStars { get; } = definition.MaxStars;
    public int MissionCount { get; } = definition.MissionCount;
    private int _missionNumber = Math.Clamp(missionNumber, 1, definition.MissionCount);
    private int _selectedStars;

    public int SelectedStars
    {
        get => _selectedStars;
        set => _selectedStars = Math.Clamp(value, 0, MaxStars);
    }

    public int MissionNumber
    {
        get => _missionNumber;
        set 
        {
            _selectedStars = 0;
            _missionNumber = Math.Clamp(value, 1, MissionCount);
        }
    }

    public bool IsFirstMission => MissionNumber == 1;
    public bool IsLastMission => MissionNumber == MissionCount;

    public string DifficultyLabel => $"{DifficultyIndex} - {DisplayName}";
    public string MissionLabel => $"Mission {MissionNumber} of {MissionCount}";
}
