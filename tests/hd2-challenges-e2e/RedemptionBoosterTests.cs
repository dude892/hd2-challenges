using Hd2Challenges.Models;
using Microsoft.Playwright;
using Xunit;

namespace Hd2Challenges.EndToEnd.Tests;

[Collection(WarpathWebAppCollection.Name)]
public sealed class RedemptionBoosterTests(WarpathWebAppFixture webApp) : RedemptionPageTest(webApp)
{
    [Fact]
    public async Task BoosterAccordionCanBeExpandedAndCollapsedWithoutBoosters()
    {
        await StartRunAsync();

        await Expect(ItemGrids.BoosterAccordionToggle).ToHaveAttributeAsync("aria-expanded", "false");
        await Expect(ItemGrids.AcquiredBoostersRegion).ToHaveAttributeAsync("aria-hidden", "true");
        await Expect(ItemGrids.AcquiredBoostersRegion).ToBeHiddenAsync();

        await ItemGrids.BoosterAccordionToggle.ClickAsync();
        await Expect(ItemGrids.BoosterAccordionToggle).ToHaveAttributeAsync("aria-expanded", "true");
        await Expect(ItemGrids.AcquiredBoostersRegion).ToHaveAttributeAsync("aria-hidden", "false");
        await Expect(ItemGrids.AcquiredBoostersRegion).ToBeVisibleAsync();
        await Expect(ItemGrids.AcquiredBoosters.Root).ToContainTextAsync("No boosters acquired.");

        await ItemGrids.BoosterAccordionToggle.ClickAsync();
        await Expect(ItemGrids.BoosterAccordionToggle).ToHaveAttributeAsync("aria-expanded", "false");
        await Expect(ItemGrids.AcquiredBoostersRegion).ToHaveAttributeAsync("aria-hidden", "true");
        await Expect(ItemGrids.AcquiredBoostersRegion).ToBeHiddenAsync();
        await ItemGrids.AcquiredBoosters.AssertCountAsync(0);
    }

    [Fact]
    public async Task ReloadingRunWithoutBoostersKeepsAccordionCollapsed()
    {
        await StartRunAsync();
        await ReloadAsync();

        await Expect(ItemGrids.BoosterAccordionToggle).ToHaveAttributeAsync("aria-expanded", "false");
        await Expect(ItemGrids.AcquiredBoostersRegion).ToHaveAttributeAsync("aria-hidden", "true");
        await ItemGrids.AcquiredBoosters.AssertCountAsync(0);
        await Expect(ItemGrids.AcquiredBoostersRegion).ToBeHiddenAsync();
    }

    [Fact]
    public async Task ReloadingRunWithAcquiredBoostersKeepsAccordionExpanded()
    {
        SaveObject save = RedemptionSaves.AcquiredBooster;
        RedemptionState state = save.ResolveState(RedemptionCatalog);
        ItemSet expectedBoosters = new(state.AcquiredItems.Boosters);
        Assert.NotEmpty(expectedBoosters);
        await ImportRunAsync(save);

        await ReloadAsync();
        await Expect(ItemGrids.BoosterAccordionToggle).ToHaveAttributeAsync("aria-expanded", "true");
        await ItemGrids.AcquiredBoosters.AssertMatchesItemsAsync(expectedBoosters);
    }

    [Fact]
    public async Task ClaimingBoosterRewardAutomaticallyExpandsAccordion()
    {
        SaveObject save = RedemptionSaves.PendingBoosterReward;
        RedemptionState state = save.ResolveState(RedemptionCatalog);
        Assert.NotEmpty(state.PendingRewardItems.Boosters);
        await ImportRunAsync(save);
        await Modals.MissionComplete.OpenAsync();
        await Expect(Modals.RewardOptions.Dialog).ToBeVisibleAsync();
        await Modals.RewardOptions.Choices.AssertMatchesItemsAsync(state.PendingRewardItems);
        await GetValidatedRewardItemsAsync();
        string? claimedRewardId = await Modals.RewardOptions.Choices.SelectableChoices.First.GetAttributeAsync("data-item-id");
        Assert.Contains(state.PendingRewardItems.Boosters,
            item => string.Equals(item.Id, claimedRewardId, StringComparison.OrdinalIgnoreCase));
        await Modals.RewardOptions.Choices.SelectableChoices.First.ClickAsync();

        await Expect(ItemGrids.BoosterAccordionToggle).ToHaveAttributeAsync("aria-expanded", "true");
        ItemSet expectedBoosters = new(state.AcquiredItems.Boosters.Concat(
            state.PendingRewardItems.Boosters.Where(
                item => string.Equals(item.Id, claimedRewardId, StringComparison.OrdinalIgnoreCase))));
        await ItemGrids.AcquiredBoosters.AssertMatchesItemsAsync(expectedBoosters);
    }

}
