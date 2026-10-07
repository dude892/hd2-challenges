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

    public void ApplyMissionFailure(RedemptionState state)
    {
        state.MissionsFailed++;
        state.CurrentOperation.MissionNumber = 1;
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
