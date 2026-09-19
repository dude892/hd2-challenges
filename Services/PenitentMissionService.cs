using Hd2Challenges.Models;

namespace Hd2Challenges.Services;

public sealed class PenitentMissionService(CatalogService catalogService)
{
    private CatalogService Catalog { get; } = catalogService;

    public void AdvanceMission(PenitentState state)
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

    public void ApplyMissionFailure(PenitentState state)
    {
        state.MissionsFailed++;
        state.CurrentOperation.MissionNumber = 1;
    }

    public bool IsFinalMission(PenitentState state)
    {
        return state.CurrentOperation.DifficultyIndex == Catalog.LastOperationIndex && state.CurrentOperation.IsLastMission;
    }
}
