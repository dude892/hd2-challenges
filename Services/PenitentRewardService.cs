using Hd2Challenges.Models;

namespace Hd2Challenges.Services;

public sealed class PenitentRewardService(CatalogService catalogService)
{
    private CatalogService CatalogService { get; } = catalogService;

    public ItemSet GetRewardPool(PenitentState state)
    {
        return CatalogService.FilterAllItems([state.StarterItems, state.BannedItems, state.AcquiredItems]);
    }
    private static bool IsHardFinale(PenitentState state) => state.CurrentOperation.DifficultyIndex == 5 && state.CurrentOperation.IsLastMission;

    public bool EnsurePendingRewards(PenitentState state, int selectedStars)
    {
        if (state.PendingRewardItems.Count != 0)
        {
            return false;
        }

        state.PendingRewardItems = RollRewardItems(state, selectedStars);
        return true;
    }

    public bool ClaimReward(PenitentState state, string id)
    {
        GameItem? reward = state.PendingRewardItems.FirstOrDefault(item => item.Id == id);
        if (reward is null)
        {
            return false;
        }

        state.AcquiredItems.AddItem(reward);
        state.PendingRewardItems.Clear();
        return true;
    }

    public bool BanPendingRewards(PenitentState state)
    {
        if (state.PendingRewardItems.Count == 0)
        {
            return false;
        }

        state.BannedItems.AddItems(state.PendingRewardItems);
        state.PendingRewardItems.Clear();
        return true;
    }

    public bool EnsurePendingPunishments(PenitentState state)
    {
        if (state.PendingPunishmentItems.Count != 0)
        {
            return false;
        }

        var punishmentPool = state.AcquiredItems
            .OrderBy(_ => Random.Shared.Next())
            .Take(Math.Min(3, state.AcquiredItems.Count));
        state.PendingPunishmentItems = new ItemSet(punishmentPool);
        return true;
    }

    public bool ClaimPunishment(PenitentState state, string id)
    {
        GameItem? punishment = state.PendingPunishmentItems.FirstOrDefault(item => item.Id == id);
        if (punishment is null)
        {
            return false;
        }

        state.AcquiredItems.RemoveItem(punishment);
        state.BannedItems.RemoveItem(punishment);
        state.PendingPunishmentItems.Clear();
        return true;
    }

    public ItemSet RollRewardItems(PenitentState state, int selectedStars)
    {
        int rewardQuantity = Math.Max(1, selectedStars - 1);
        if (state.Difficulty.IsSuper)
        {
            rewardQuantity--;
        }

        rewardQuantity = Math.Max(1, rewardQuantity);

        var rewardPool = GetRewardPool(state);

        rewardPool = IsHardFinale(state) ? new ItemSet(rewardPool.Where(item => item.Antitank)) : rewardPool;
    
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
