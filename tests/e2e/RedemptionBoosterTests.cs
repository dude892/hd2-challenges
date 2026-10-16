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

        ILocator toggle = Page.GetByRole(AriaRole.Button, new() { Name = "Toggle acquired boosters" });
        ILocator content = Page.Locator("#boosters-collapse");
        await Expect(toggle).ToHaveAttributeAsync("aria-expanded", "false");
        await Expect(content).ToHaveAttributeAsync("aria-hidden", "true");
        await Expect(content).ToBeHiddenAsync();

        await toggle.ClickAsync();
        await Expect(toggle).ToHaveAttributeAsync("aria-expanded", "true");
        await Expect(content).ToHaveAttributeAsync("aria-hidden", "false");
        await Expect(content).ToBeVisibleAsync();
        await Expect(content).ToContainTextAsync("No boosters acquired.");

        await toggle.ClickAsync();
        await Expect(toggle).ToHaveAttributeAsync("aria-expanded", "false");
        await Expect(content).ToBeHiddenAsync();
    }

    [Fact]
    public async Task ReloadingRunWithoutBoostersKeepsAccordionCollapsed()
    {
        await StartRunAsync();
        await Page.ReloadAsync();

        await Expect(Page.GetByRole(AriaRole.Button, new() { Name = "Toggle acquired boosters" }))
            .ToHaveAttributeAsync("aria-expanded", "false");
        await Expect(Page.Locator("#boosters-collapse")).ToBeHiddenAsync();
    }

    [Fact]
    public async Task ReloadingRunWithAcquiredBoostersKeepsAccordionExpanded()
    {
        await ImportRunAsync(acquiredItemIds: ["uavrecon"]);

        await Page.ReloadAsync();
        await Expect(Page.GetByRole(AriaRole.Button, new() { Name = "Toggle acquired boosters" }))
            .ToHaveAttributeAsync("aria-expanded", "true");
        await Expect(Page.Locator("#boosters-collapse")).ToContainTextAsync("UAV Recon");
    }

    [Fact]
    public async Task ClaimingBoosterRewardAutomaticallyExpandsAccordion()
    {
        await ImportRunAsync(pendingRewardIds: ["uavrecon"]);
        await Page.GetByRole(AriaRole.Button, new() { Name = "Mission Complete" }).ClickAsync();
        await Expect(Page.GetByText("Reward Options")).ToBeVisibleAsync();
        await Page.GetByRole(AriaRole.Dialog, new() { Name = "Reward Options" })
            .GetByRole(AriaRole.Button, new() { Name = "UAV Recon", Exact = true }).ClickAsync();

        ILocator toggle = Page.GetByRole(AriaRole.Button, new() { Name = "Toggle acquired boosters" });
        await Expect(toggle).ToHaveAttributeAsync("aria-expanded", "true");
        await Expect(Page.Locator("#boosters-collapse")).ToContainTextAsync("UAV Recon");
    }

}
