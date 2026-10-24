using Hd2Challenges.Models;
using Hd2Challenges.Services;
using Microsoft.Playwright;
using Microsoft.Playwright.Xunit.v3;
using Xunit;

namespace Hd2Challenges.EndToEnd.Tests;

public abstract class RedemptionPageTest(WarpathWebAppFixture webApp) : PageTest
{
    private protected RedemptionModals Modals => new(Page);
    private protected RedemptionItemGrids ItemGrids => new(Page);
    protected CatalogService Catalog => webApp.Catalog;
    protected RedemptionCatalogService RedemptionCatalog => webApp.RedemptionCatalog;
    protected Operation DefaultStartOperation => RedemptionCatalog.GetDifficulty("normal").StartOperation;
    protected ILocator BeginRunButton => Page.GetByRole(AriaRole.Button, new() { Name = "Begin Run", Exact = true });
    protected ILocator CurrentOperationProgress => Page.GetByRole(AriaRole.Status, new() { Name = "Current operation", Exact = true });
    protected ILocator CurrentMissionProgress => Page.GetByRole(AriaRole.Status, new() { Name = "Current mission progress", Exact = true });
    protected ILocator MissionsFailedCount => Page.GetByRole(AriaRole.Status, new() { Name = "Missions failed", Exact = true });
    protected ILocator ImportedSaveStatus => Page.GetByRole(AriaRole.Status, new() { NameRegex = new System.Text.RegularExpressions.Regex("^Imported slot") });

    protected async Task OpenSetupAsync()
    {
        await Page.GotoAsync($"{webApp.BaseUrl}/warpath-redemption");
        await BeginRunButton.WaitForAsync();
    }

    protected async Task StartRunAsync()
    {
        await OpenSetupAsync();
        await BeginRunButton.ClickAsync();
    }

    protected async Task ReloadAsync()
    {
        await Page.ReloadAsync();
        await Modals.SaveSlots.OpenButton.WaitForAsync();
    }

    private protected async Task ImportRunAsync(SaveObject save)
    {
        await OpenSetupAsync();
        await Modals.SaveSlots.OpenAsync();

        await Modals.SaveSlots.ImportSaveInput.SetInputFilesAsync(save.FilePath);

        await Expect(ImportedSaveStatus).ToContainTextAsync($"Imported slot {save.Slot.Name}.");
        await Modals.SaveSlots.CloseAsync();
    }

    private protected async Task<ItemSet> GetValidatedRewardItemsAsync()
    {
        await Expect(Modals.RewardOptions.Choices.Cards.First).ToBeVisibleAsync();
        string[] ids = await Modals.RewardOptions.Choices.GetItemIdsAsync();
        Assert.NotEmpty(ids);
        Assert.Equal(ids.Length, ids.Distinct(StringComparer.OrdinalIgnoreCase).Count());
        ItemSet catalogItems = Catalog.GetAllItems();
        foreach (string id in ids)
        {
            Assert.Contains(catalogItems, item => string.Equals(item.Id, id, StringComparison.OrdinalIgnoreCase));
        }

        return new ItemSet(catalogItems.Where(item => ids.Contains(item.Id, StringComparer.OrdinalIgnoreCase)));
    }
}
