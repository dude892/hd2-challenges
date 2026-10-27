using Hd2Challenges.Models;

namespace Hd2Challenges.Services;

public sealed class RedemptionRewardService(CatalogService catalogService, RedemptionSessionService session)
{
    private RedemptionState State => session.State;
    private bool IsHardFinale => State.CurrentOperation.DifficultyIndex == 5 && State.CurrentOperation.IsLastMission;

    public ItemSet GetRewardPool() => 
        catalogService.GetItemsByWarbonds(State.SelectedWarbonds)
            .RemoveItems([State.StarterItems, State.BannedItems, State.AcquiredItems]);

    public bool EnsurePendingRewards(int selectedStars)
    {
        if (State.PendingRewardItems.Count != 0)
        {
            return false;
        }

        State.PendingRewardItems = RollRewardItems(selectedStars);
        return true;
    }

    public bool ClaimReward(string id)
    {
        GameItem? reward = State.PendingRewardItems.FirstOrDefault(item => item.Id == id);
        if (reward is null)
        {
            return false;
        }

        State.AcquiredItems.AddItem(reward);
        State.PendingRewardItems.Clear();
        return true;
    }

    public bool BanPendingRewards()
    {
        if (State.PendingRewardItems.Count == 0)
        {
            return false;
        }

        State.BannedItems.AddItems(State.PendingRewardItems);
        State.PendingRewardItems.Clear();
        return true;
    }

    public bool EnsurePendingPunishments()
    {
        if (State.PendingPunishmentItems.Count != 0)
        {
            return false;
        }

        var punishmentPool = State.AcquiredItems
            .Where(item => !State.StarterItems.ContainsItem(item))
            .OrderBy(_ => Random.Shared.Next())
            .Take(3);
        State.PendingPunishmentItems = new ItemSet(punishmentPool);
        return true;
    }

    public bool ClaimPunishment(string id)
    {
        GameItem? punishment = State.PendingPunishmentItems.FirstOrDefault(item => item.Id == id);
        if (punishment is null)
        {
            return false;
        }

        State.AcquiredItems.RemoveItem(punishment);
        State.BannedItems.RemoveItem(punishment);
        State.PendingPunishmentItems.Clear();
        return true;
    }

    public ItemSet RollRewardItems(int selectedStars)
    {
        int rewardQuantity = Math.Max(1, selectedStars - 1);
        if (State.Difficulty.IsSuper)
        {
            rewardQuantity--;
        }

        rewardQuantity = Math.Max(1, rewardQuantity);

        var rewardPool = GetRewardPool();

        rewardPool = IsHardFinale ? new ItemSet(rewardPool.Where(item => item.Antitank)) : rewardPool;
    
        if (rewardPool.Count == 0)
        {
            return new ItemSet();
        }

        return new ItemSet(
            rewardPool.AllItems
                .OrderBy(_ => Random.Shared.Next())
                .Take(Math.Min(rewardQuantity, rewardPool.Count))
                .DistinctBy(item => item.Id)
                .ToList());
    }
}
