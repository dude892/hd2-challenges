using System.Text;
using System.Text.Json;
using Microsoft.Playwright;
using Xunit;

namespace Hd2Challenges.EndToEnd.Tests;

[Collection(WarpathWebAppCollection.Name)]
public sealed class RedemptionSaveReloadTests(WarpathWebAppFixture webApp) : RedemptionPageTest(webApp)
{
    [Fact]
    public async Task AdvancingMissionPersistsAndReloadsTheActiveRun()
    {
        await StartRunAsync();
        await Page.GetByRole(AriaRole.Button, new() { Name = "Mission Complete" }).ClickAsync();
        await Page.GetByRole(AriaRole.Button, new() { Name = "3 stars" }).ClickAsync();
        await Page.GetByRole(AriaRole.Button, new() { Name = "Continue to Rewards" }).ClickAsync();
        await Page.Locator(".app-modal .card-body button").First.ClickAsync();

        await Expect(Page.GetByText("Mission 2 of 2")).ToBeVisibleAsync();
        await Page.ReloadAsync();
        await Expect(Page.GetByText("Mission 2 of 2")).ToBeVisibleAsync();

        await Page.GetByRole(AriaRole.Button, new() { Name = "Save Slots" }).ClickAsync();
        await Expect(Page.Locator(".app-modal.save-slot-modal")).ToContainTextAsync("Normal Warpath |");
    }

    [Fact]
    public async Task RenamingSaveSlotPersistsAfterReload()
    {
        await StartRunAsync();
        await Page.GetByRole(AriaRole.Button, new() { Name = "Save Slots" }).ClickAsync();

        ILocator saveManager = Page.Locator(".app-modal.save-slot-modal");
        await saveManager.GetByRole(AriaRole.Button, new() { Name = "Edit name" }).ClickAsync();

        ILocator renameModal = Page.Locator(".app-modal.save-slot-rename-modal");
        await renameModal.GetByLabel("Slot name").FillAsync("Campaign One");
        await renameModal.GetByRole(AriaRole.Button, new() { Name = "Save name" }).ClickAsync();
        await Expect(saveManager).ToContainTextAsync("Campaign One");

        await Page.ReloadAsync();
        await Page.GetByRole(AriaRole.Button, new() { Name = "Save Slots" }).ClickAsync();
        await Expect(Page.Locator(".app-modal.save-slot-modal")).ToContainTextAsync("Campaign One");
    }

    [Fact]
    public async Task SnapshotCanBeLoadedAndRestoresItsSavedMission()
    {
        await StartRunAsync();
        await Page.GetByRole(AriaRole.Button, new() { Name = "Save Slots", Exact = true }).ClickAsync();
        ILocator saveManager = Page.Locator(".app-modal.save-slot-modal");
        await saveManager.GetByRole(AriaRole.Button, new() { Name = "Create Snapshot" }).ClickAsync();
        await saveManager.GetByRole(AriaRole.Button, new() { Name = "×" }).ClickAsync();

        await Page.GetByRole(AriaRole.Button, new() { Name = "Save Slots", Exact = true }).ClickAsync();
        saveManager = Page.Locator(".app-modal.save-slot-modal");
        await saveManager.Locator(".card-body > .d-grid > .card").Last
            .GetByRole(AriaRole.Button, new() { Name = "Load" }).ClickAsync();
        await saveManager.GetByRole(AriaRole.Button, new() { Name = "×" }).ClickAsync();

        await Page.GetByRole(AriaRole.Button, new() { Name = "Mission Complete", Exact = true }).ClickAsync();
        await Page.GetByRole(AriaRole.Button, new() { Name = "3 stars", Exact = true }).ClickAsync();
        await Page.GetByRole(AriaRole.Button, new() { Name = "Continue to Rewards" }).ClickAsync();
        await Page.Locator(".app-modal.item-modal .item-card").First.ClickAsync();
        await Expect(Page.GetByText("Mission 2 of 2", new() { Exact = true })).ToBeVisibleAsync();

        await Page.GetByRole(AriaRole.Button, new() { Name = "Save Slots", Exact = true }).ClickAsync();
        saveManager = Page.Locator(".app-modal.save-slot-modal");
        ILocator snapshot = saveManager.Locator(".card-body > .d-grid > .card").Filter(new() { HasText = "Snapshot" });
        await snapshot.GetByRole(AriaRole.Button, new() { Name = "Load" }).ClickAsync();

        await Expect(Page.GetByText("Mission 1 of 2", new() { Exact = true })).ToBeVisibleAsync();
        await Expect(Page.Locator("h1")).ToContainTextAsync("Snapshot");
    }

    [Fact]
    public async Task ExportedSaveCanBeImportedThroughTheSaveManager()
    {
        await StartRunAsync();
        await Page.GetByRole(AriaRole.Button, new() { Name = "Save Slots", Exact = true }).ClickAsync();
        ILocator saveManager = Page.Locator(".app-modal.save-slot-modal");
        Task<IDownload> downloadTask = Page.WaitForDownloadAsync();
        await saveManager.GetByRole(AriaRole.Button, new() { Name = "Export", Exact = true }).ClickAsync();

        IDownload download = await downloadTask;
        Assert.EndsWith(".json", download.SuggestedFilename);
        string json = await File.ReadAllTextAsync(await download.PathAsync());
        using JsonDocument export = JsonDocument.Parse(json);
        Assert.Equal("warpath-redemption", export.RootElement.GetProperty("mode").GetString());

        await saveManager.Locator("input[type=file]").SetInputFilesAsync(new FilePayload
        {
            Name = download.SuggestedFilename,
            MimeType = "application/json",
            Buffer = Encoding.UTF8.GetBytes(json)
        });

        await Expect(Page.Locator(".toast-body")).ToContainTextAsync("Imported slot");
        await Expect(saveManager.Locator(".card-body > .d-grid > .card")).ToHaveCountAsync(2);
    }

    [Fact]
    public async Task SkippingRewardsBansThemAndPersistsTheBan()
    {
        await StartRunAsync();
        await CompleteFirstMissionAndOpenRewardsAsync();

        await Page.GetByRole(AriaRole.Button, new() { Name = "Skip and Ban All" }).ClickAsync();
        await Expect(Page.GetByText("Mission 2 of 2")).ToBeVisibleAsync();

        await Page.GetByRole(AriaRole.Button, new() { Name = "Banned Items" }).ClickAsync();
        ILocator bannedItems = Page.Locator(".app-modal.item-modal");
        await Expect(bannedItems).ToBeVisibleAsync();
        await Expect(bannedItems.Locator(".item-card")).Not.ToHaveCountAsync(0);

        await Page.ReloadAsync();
        await Page.GetByRole(AriaRole.Button, new() { Name = "Banned Items" }).ClickAsync();
        bannedItems = Page.Locator(".app-modal.item-modal");
        await Expect(bannedItems).ToBeVisibleAsync();
        await Expect(bannedItems.Locator(".item-card")).Not.ToHaveCountAsync(0);
    }

    private async Task CompleteFirstMissionAndOpenRewardsAsync()
    {
        await Page.GetByRole(AriaRole.Button, new() { Name = "Mission Complete" }).ClickAsync();
        await Page.GetByRole(AriaRole.Button, new() { Name = "3 stars" }).ClickAsync();
        await Page.GetByRole(AriaRole.Button, new() { Name = "Continue to Rewards" }).ClickAsync();
        await Expect(Page.GetByText("Reward Options")).ToBeVisibleAsync();
    }
}
