using System.Text.Json.Serialization;

namespace Hd2Challenges.Models;

public sealed record MissionProgressEntry
{
    [JsonConstructor]
    public MissionProgressEntry(
        int operationIndex,
        int missionNumber,
        bool succeeded,
        int stars,
        string? rewardItemId = null,
        string? punishmentItemId = null,
        List<string>? bannedItemIds = null)
    {
        OperationIndex = operationIndex;
        MissionNumber = missionNumber;
        Succeeded = succeeded;
        Stars = stars;
        RewardItemId = rewardItemId;
        PunishmentItemId = punishmentItemId;
        BannedItemIds = bannedItemIds ?? [];
    }

    public int OperationIndex { get; init; }
    public int MissionNumber { get; init; }
    public bool Succeeded { get; init; }
    public int Stars { get; init; }
    public string? RewardItemId { get; init; }
    public string? PunishmentItemId { get; init; }
    public List<string> BannedItemIds { get; init; }
}
