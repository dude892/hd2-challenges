using Hd2Challenges.Models;

namespace Hd2Challenges.Services;

public sealed class PenitentRewardService(CatalogService catalogService)
{
    private CatalogService Catalog { get; } = catalogService;

    public ItemSet GetRewardPool(PenitentState state) => Catalog.FilterAllItems([state.StarterItems, state.BannedItems, state.AcquiredItems]);

    public ItemSet RollRewardItems(PenitentState state, int selectedStars, int currentMissionNumber)
    {
        int rewardQuantity = Math.Max(1, selectedStars - 1);
        if (state.Difficulty.IsSuper)
        {
            rewardQuantity--;
        }

        rewardQuantity = Math.Max(1, rewardQuantity);

        var rewardPool = GetRewardPool(state);
        if (rewardPool.Count == 0)
        {
            return new ItemSet();
        }

        if (currentMissionNumber == 7)
        {
            var antiTankPool = new ItemSet(rewardPool.Where(item => item.Antitank));
            if (antiTankPool.Count > 0)
            {
                return new ItemSet(antiTankPool.OrderBy(_ => Random.Shared.Next()).Take(Math.Min(rewardQuantity, antiTankPool.Count)));
            }
        }

        return new ItemSet(
            rewardPool.AllItems
                .OrderBy(_ => Random.Shared.Next())
                .Take(rewardQuantity)
                .Select(item => RollItemFromPool(new List<GameItem> { item }, currentMissionNumber))
                .OfType<GameItem>()
                .DistinctBy(item => item.InternalName)
                .ToList());
    }

    public GameItem? RollItemFromPool(List<GameItem> pool, int currentMissionNumber)
    {
        if (pool.Count == 0)
        {
            return null;
        }

        if (currentMissionNumber == 7)
        {
            var antiTankPool = pool.Where(item => item.Antitank).ToList();
            if (antiTankPool.Count > 0)
            {
                pool = antiTankPool;
            }
        }

        return pool[Random.Shared.Next(pool.Count)];
    }
}