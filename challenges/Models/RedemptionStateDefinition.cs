using System.Collections.Immutable;
using System.Text.Json.Serialization;

namespace Hd2Challenges.Models;

public sealed record RedemptionStateDefinition
{
    public string DifficultyId { get; init; } = "normal";
    public int OperationIndex { get; init; } = 3;
    public int MissionNumber { get; init; }
    public int MissionsFailed { get; init; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? RunCompleted { get; init; }
    public ImmutableArray<string> SelectedWarbonds { get; init; } = ImmutableArray<string>.Empty;
    public ImmutableArray<string> AcquiredItemIds { get; init; } = ImmutableArray<string>.Empty;
    public ImmutableArray<string> BannedItemIds { get; init; } = ImmutableArray<string>.Empty;
    public ImmutableArray<string> PendingRewardIds { get; init; } = ImmutableArray<string>.Empty;
    public ImmutableArray<string> PendingPunishmentIds { get; init; } = ImmutableArray<string>.Empty;
}
