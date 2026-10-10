namespace Hd2Challenges.Models;

public sealed record MissionProgressEntry(int OperationIndex, int MissionNumber, bool Succeeded, int Stars);
