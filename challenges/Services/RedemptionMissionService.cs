using Hd2Challenges.Models;

namespace Hd2Challenges.Services;

public sealed class RedemptionMissionService(CatalogService catalogService, RedemptionRewardService rewards)
{
    private CatalogService Catalog { get; } = catalogService;

    public MissionCompletionResult CompleteMission(RedemptionState state, int stars)
    {
        if (!state.CanCompleteMission)
        {
            return MissionCompletionResult.Unavailable;
        }

        if (TryCompleteFinalMission(state))
        {
            return MissionCompletionResult.ChallengeCompleted;
        }

        rewards.EnsurePendingRewards(state, stars);
        return state.PendingRewardItems.Count > 0
            ? MissionCompletionResult.RewardsPending
            : MissionCompletionResult.NoRewardsAvailable;
    }

    public bool PrepareMissionFailure(RedemptionState state) =>
        state.CanFailMission && rewards.EnsurePendingPunishments(state);

    public bool ClaimMissionReward(RedemptionState state, string itemId)
    {
        if (!state.CanCompleteMission || !rewards.ClaimReward(state, itemId))
        {
            return false;
        }

        AdvanceMission(state);
        return true;
    }

    public bool SkipAndBanMissionRewards(RedemptionState state)
    {
        if (!state.CanCompleteMission || !rewards.BanPendingRewards(state))
        {
            return false;
        }

        AdvanceMission(state);
        return true;
    }

    public bool ClaimMissionPunishment(RedemptionState state, string itemId)
    {
        if (!state.CanFailMission || !rewards.ClaimPunishment(state, itemId))
        {
            return false;
        }

        ApplyMissionFailure(state);
        return true;
    }

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
