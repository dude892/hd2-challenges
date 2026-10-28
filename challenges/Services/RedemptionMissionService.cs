using Hd2Challenges.Models;

namespace Hd2Challenges.Services;

public sealed class RedemptionMissionService(
    CatalogService catalog,
    RedemptionRewardService rewards,
    RedemptionSessionService session)
{
    private RedemptionState State => session.State;
    public bool IsFinalMission => State.CurrentOperation.DifficultyIndex == catalog.LastOperationIndex && State.CurrentOperation.IsLastMission;

    public MissionCompletionResult CompleteMission(int stars)
    {
        if (!session.CanCompleteMission)
        {
            return MissionCompletionResult.Unavailable;
        }

        if (TryCompleteFinalMission())
        {
            return MissionCompletionResult.ChallengeCompleted;
        }

        rewards.EnsurePendingRewards(stars);
        return State.PendingRewardItems.Count > 0 ? MissionCompletionResult.RewardsPending : MissionCompletionResult.NoRewardsAvailable;
    }

    public bool PrepareMissionFailure()
    {
        return session.CanFailMission && rewards.EnsurePendingPunishments();
    }

    public bool ClaimMissionReward(string itemId)
    {
        if (!session.CanCompleteMission || !rewards.ClaimReward(itemId))
        {
            return false;
        }

        AdvanceMission();
        return true;
    }

    public bool SkipAndBanMissionRewards()
    {
        if (!session.CanCompleteMission || !rewards.BanPendingRewards())
        {
            return false;
        }

        AdvanceMission();
        return true;
    }

    public bool ClaimMissionPunishment(string itemId)
    {
        if (!session.CanFailMission || !rewards.ClaimPunishment(itemId))
        {
            return false;
        }

        ApplyMissionFailure();
        return true;
    }

    public void AdvanceMission()
    {
        if (State.CurrentOperation.MissionNumber < State.CurrentOperation.MissionCount)
        {
            State.CurrentOperation.MissionNumber++;
        }
        else if (State.CurrentOperation.DifficultyIndex < catalog.LastOperationIndex)
        {
            State.CurrentOperation = catalog.GetOperation(State.CurrentOperation.DifficultyIndex + 1);
        }
    }

    public void ApplyMissionFailure()
    {
        State.MissionsFailed++;
        State.CurrentOperation.MissionNumber = 1;
    }

    public bool TryCompleteFinalMission()
    {
        if (!IsFinalMission)
        {
            return false;
        }

        session.SetRunStatus(RunStatus.Completed);
        return true;
    }
}
