using Microsoft.Playwright;
using Xunit;

namespace Hd2Challenges.EndToEnd.Tests;

[Collection(WarpathWebAppCollection.Name)]
public sealed class RedemptionRunSetupTests(WarpathWebAppFixture webApp) : RedemptionPageTest(webApp)
{
    [Fact]
    public async Task SelectedDifficultyIsShownInRunSettings()
    {
        await OpenSetupAsync();
        string initialPreview = await GetLoadoutPreviewAsync();

        await Page.GetByLabel("Difficulty").SelectOptionAsync("quick");
        await Expect(Page.GetByText("Quick Warpath loadout preview updated.")).ToBeVisibleAsync();
        string updatedPreview = await GetLoadoutPreviewAsync();
        Assert.NotEqual(initialPreview, updatedPreview);

        await Page.GetByRole(AriaRole.Button, new() { Name = "Begin Run" }).ClickAsync();
        await Expect(Page.GetByText("Quick Warpath", new() { Exact = true })).ToBeVisibleAsync();

        await Page.GetByRole(AriaRole.Button, new() { Name = "Run Settings" }).ClickAsync();
        await Expect(Page.GetByRole(AriaRole.Dialog, new() { Name = "Run Settings" })).ToContainTextAsync("Quick Warpath");
    }

    private async Task<string> GetLoadoutPreviewAsync()
    {
        ILocator[] images = [.. await Page.GetByRole(AriaRole.Group, new() { Name = "Starting loadout preview" })
            .GetByRole(AriaRole.Img).AllAsync()];
        string?[] itemNames = await Task.WhenAll(images.Select(image => image.GetAttributeAsync("alt")));
        return string.Join("|", itemNames);
    }

    [Fact]
    public async Task StartingNewRunReturnsToTheSetupScreen()
    {
        await StartRunAsync();
        await Page.GetByRole(AriaRole.Button, new() { Name = "Start New Run" }).ClickAsync();

        await Expect(Page.GetByRole(AriaRole.Button, new() { Name = "Begin Run" })).ToBeVisibleAsync();
        await Expect(Page.GetByRole(AriaRole.Button, new() { Name = "Mission Complete" })).ToHaveCountAsync(0);
    }
}
