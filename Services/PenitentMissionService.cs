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
        else if (Catalog.GetNextOperationDefinition(state.CurrentOperation.Id) is OperationDefinition nextOperationDefinition)
        {
            state.CurrentOperation = new Operation(nextOperationDefinition);
        }
    }

    public void ApplyMissionFailure(PenitentState state)
    {
        state.MissionsFailed++;
        state.CurrentOperation.MissionNumber = 1;
    }

    public void SetMissionProgress(PenitentState state, string operationId, int missionNumber)
    {
        var operation = Catalog.Operations.FirstOrDefault(item => item.Id == operationId) ?? Catalog.Operations[0];
        state.CurrentOperation = new Operation(operation, missionNumber);
    }

    public bool IsFinalMission(PenitentState state)
    {
        return state.CurrentOperation.Id == Catalog.Operations[^1].Id && state.CurrentOperation.IsLastMission;
    }
}
