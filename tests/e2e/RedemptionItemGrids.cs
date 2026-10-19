using Microsoft.Playwright;

namespace Hd2Challenges.EndToEnd.Tests;

internal sealed class RedemptionItemGrids(IPage page)
{
    public ILocator BoosterAccordionToggle => page.GetByRole(AriaRole.Button, new() { Name = "Toggle acquired boosters", Exact = true });
    public ILocator AcquiredBoostersRegion => page.GetByRole(AriaRole.Region, new() { Name = "Acquired boosters", IncludeHidden = true, Exact = true });
    public ItemGridObject StartingLoadoutPreview => new(page.GetByRole(AriaRole.Group, new() { Name = "Starting loadout preview", Exact = true }));
    public ItemGridObject AcquiredBoosters => new(AcquiredBoostersRegion.GetByRole(AriaRole.Group, new() { Name = "Acquired booster items", Exact = true, IncludeHidden = true }));
    public ItemGridObject AcquiredItems => new(page.GetByRole(AriaRole.Group, new()
    {
        NameRegex = new System.Text.RegularExpressions.Regex("^Acquired (primary items|secondary items|throwable items|armor passives|stratagems|booster items)$"),
        IncludeHidden = true
    }));
}
