using Hd2Challenges.Models;
using Microsoft.Playwright;
using Xunit;

namespace Hd2Challenges.EndToEnd.Tests;

[Collection(WarpathWebAppCollection.Name)]
public sealed class RedemptionRunSetupTests(WarpathWebAppFixture webApp) : RedemptionPageTest(webApp)
{
    [Theory]
    [InlineData("normal")]
    [InlineData("solo")]
    [InlineData("quick")]
    [InlineData("super")]
    [InlineData("supersolo")]
    public async Task SelectedDifficultyIsShownInRunSettings(string difficultyId)
    {
        await OpenSetupAsync();
        RedemptionDifficulty difficulty = RedemptionCatalog.GetDifficulty(difficultyId);
        ItemSet expectedItems = RedemptionCatalog.GetStarterItems(difficulty, Catalog.GetAllWarbonds());
        string initialDifficulty = await Page.GetByLabel("Select campaign difficulty").InputValueAsync();
        string[] initialItemIds = await ItemGrids.StartingLoadoutPreview.GetItemIdsAsync();

        await Page.GetByLabel("Select campaign difficulty").SelectOptionAsync(difficulty.Id);
        string[] updatedItemIds = await ItemGrids.StartingLoadoutPreview.GetItemIdsAsync();
        if(difficultyId != initialDifficulty)
        {
            await Expect(Page.GetByRole(AriaRole.Status, new() { Name = $"{difficulty.DisplayName} loadout preview updated." })).ToBeVisibleAsync();
            Assert.False(initialItemIds.SequenceEqual(updatedItemIds, StringComparer.OrdinalIgnoreCase));
        }
        else
        {
            Assert.True(initialItemIds.SequenceEqual(updatedItemIds, StringComparer.OrdinalIgnoreCase));
        }
        await ItemGrids.StartingLoadoutPreview.AssertMatchesItemsAsync(expectedItems);

        await BeginRunButton.ClickAsync();
        await Expect(Page.GetByText(difficulty.DisplayName, new() { Exact = true })).ToBeVisibleAsync();

        await Modals.RunSettings.OpenAsync();
        await Expect(Modals.RunSettings.Dialog).ToContainTextAsync(difficulty.DisplayName);
    }

    [Fact]
    public async Task StartingNewRunReturnsToTheSetupScreen()
    {
        await StartRunAsync();
        await Page.GetByRole(AriaRole.Button, new() { Name = "Start a new Warpath: Redemption run" }).ClickAsync();

        await Expect(BeginRunButton).ToBeVisibleAsync();
        await Expect(Modals.MissionComplete.OpenButton).ToHaveCountAsync(0);
    }
}
