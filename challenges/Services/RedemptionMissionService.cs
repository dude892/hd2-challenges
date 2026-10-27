using Hd2Challenges.Models;

namespace Hd2Challenges.Services;

public sealed class RedemptionMissionService(CatalogService catalogService)
{
    private CatalogService Catalog { get; } = catalogService;

    public void AdvanceMission(RedemptionState state)
    {
        if (state.CurrentOperation.MissionNumber < state.CurrentOperation.MissionCount)
        {
            state.CurrentOperation.MissionNumber++;
        }
        else if (state.CurrentOperation.DifficultyIndex < Catalog.LastOperationIndex)
        {
            state.CurrentOperation = Catalog.GetOperation(state.CurrentOperation.DifficultyIndex + 1);
        }
    }

    public void ApplyMissionFailure(RedemptionState state, string punishmentItemId)
    {
        state.MissionHistory.Add(new(
            state.CurrentOperation.DifficultyIndex,
            state.CurrentOperation.MissionNumber,
            false,
            0,
            punishmentItemId: punishmentItemId));
        state.MissionsFailed++;
        state.CurrentOperation.MissionNumber = 1;
    }

    public void RecordMissionRewardSelection(RedemptionState state, string rewardItemId)
    {
        if (state.MissionHistory.LastOrDefault() is { Succeeded: true, RewardItemId: null } entry)
        {
            state.MissionHistory[^1] = entry with { RewardItemId = rewardItemId };
        }
    }

    public void RecordMissionBannedItems(RedemptionState state, IEnumerable<string> bannedItemIds)
    {
        if (state.MissionHistory.LastOrDefault() is not { Succeeded: true } entry)
        {
            return;
        }

        state.MissionHistory[^1] = entry with
        {
            BannedItemIds = entry.BannedItemIds
                .Concat(bannedItemIds)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList()
        };
    }

    public void RecordMissionSuccess(RedemptionState state, int stars)
    {
        state.MissionHistory.Add(new(
            state.CurrentOperation.DifficultyIndex,
            state.CurrentOperation.MissionNumber,
            true,
            Math.Clamp(stars, 0, state.CurrentOperation.MaxStars)));
    }

    public bool IsFinalMission(RedemptionState state)
    {
        return state.CurrentOperation.DifficultyIndex == Catalog.LastOperationIndex && state.CurrentOperation.IsLastMission;
    }

    public bool TryCompleteFinalMission(RedemptionState state)
    {
        if (!IsFinalMission(state))
        {
            return false;
        }

        state.RunCompleted = true;
        return true;
    }
}
