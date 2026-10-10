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

    public void ApplyMissionFailure(RedemptionState state, string selectedItemId)
    {
        state.MissionHistory.Add(new(
            state.CurrentOperation.DifficultyIndex,
            state.CurrentOperation.MissionNumber,
            false,
            0,
            selectedItemId));
        state.MissionsFailed++;
        state.CurrentOperation.MissionNumber = 1;
    }

    public void RecordMissionRewardSelection(RedemptionState state, string selectedItemId)
    {
        if (state.MissionHistory.LastOrDefault() is { Succeeded: true, SelectedItemId: null } entry)
        {
            state.MissionHistory[^1] = entry with { SelectedItemId = selectedItemId };
        }
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
