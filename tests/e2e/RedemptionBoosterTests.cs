using Microsoft.Playwright;
using Xunit;

namespace Hd2Challenges.EndToEnd.Tests;

[Collection(WarpathWebAppCollection.Name)]
public sealed class RedemptionBoosterTests(WarpathWebAppFixture webApp) : RedemptionPageTest(webApp)
{
    private ILocator BoosterAccordionToggle => Page.GetByRole(AriaRole.Button, new() { Name = "Toggle acquired boosters", Exact = true });

    [Fact]
    public async Task BoosterAccordionCanBeExpandedAndCollapsedWithoutBoosters()
    {
        await StartRunAsync();

        await Expect(BoosterAccordionToggle).ToHaveAttributeAsync("aria-expanded", "false");
        await Expect(AcquiredBoostersRegion).ToHaveAttributeAsync("aria-hidden", "true");
        await Expect(AcquiredBoostersRegion).ToBeHiddenAsync();

        await BoosterAccordionToggle.ClickAsync();
        await Expect(BoosterAccordionToggle).ToHaveAttributeAsync("aria-expanded", "true");
        await Expect(AcquiredBoostersRegion).ToHaveAttributeAsync("aria-hidden", "false");
        await Expect(AcquiredBoostersRegion).ToBeVisibleAsync();
        await Expect(AcquiredBoostersRegion).ToContainTextAsync("No boosters acquired.");

        await BoosterAccordionToggle.ClickAsync();
        await Expect(BoosterAccordionToggle).ToHaveAttributeAsync("aria-expanded", "false");
        await Expect(AcquiredBoostersRegion).ToHaveAttributeAsync("aria-hidden", "true");
        await Expect(AcquiredBoostersRegion).ToBeHiddenAsync();
    }

    [Fact]
    public async Task ReloadingRunWithoutBoostersKeepsAccordionCollapsed()
    {
        await StartRunAsync();
        await Page.ReloadAsync();

        await Expect(BoosterAccordionToggle).ToHaveAttributeAsync("aria-expanded", "false");
        await Expect(AcquiredBoostersRegion).ToHaveAttributeAsync("aria-hidden", "true");
        await Expect(AcquiredBoostersRegion).ToBeHiddenAsync();
    }

    [Fact]
    public async Task ReloadingRunWithAcquiredBoostersKeepsAccordionExpanded()
    {
        await ImportRunAsync(acquiredItemIds: ["uavrecon"]);

        await Page.ReloadAsync();
        await Expect(BoosterAccordionToggle).ToHaveAttributeAsync("aria-expanded", "true");
        await Expect(AcquiredBoostersRegion).ToContainTextAsync("UAV Recon");
    }

    [Fact]
    public async Task ClaimingBoosterRewardAutomaticallyExpandsAccordion()
    {
        await ImportRunAsync(pendingRewardIds: ["uavrecon"]);
        await Modals.MissionComplete.OpenAsync();
        await Expect(Modals.RewardOptions.Dialog).ToBeVisibleAsync();
        await Modals.RewardOptions.Choices
            .GetByRole(AriaRole.Button, new() { Name = "UAV Recon", Exact = true }).ClickAsync();

        await Expect(BoosterAccordionToggle).ToHaveAttributeAsync("aria-expanded", "true");
        await Expect(AcquiredBoostersRegion).ToContainTextAsync("UAV Recon");
    }

}
