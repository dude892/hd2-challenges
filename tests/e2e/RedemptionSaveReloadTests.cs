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
        await Page.GetByRole(AriaRole.Dialog, new() { Name = "Reward Options" })
            .GetByRole(AriaRole.Button).Nth(1).ClickAsync();

        await Expect(Page.GetByText("Mission 2 of 2")).ToBeVisibleAsync();
        await Page.ReloadAsync();
        await Expect(Page.GetByText("Mission 2 of 2")).ToBeVisibleAsync();

        await Page.GetByRole(AriaRole.Button, new() { Name = "Save Slots" }).ClickAsync();
        await Expect(Page.GetByRole(AriaRole.Dialog, new() { Name = "Warpath: Redemption Save Slots" }))
            .ToContainTextAsync("Normal Warpath |");
    }

    [Fact]
    public async Task RenamingSaveSlotPersistsAfterReload()
    {
        await StartRunAsync();
        await Page.GetByRole(AriaRole.Button, new() { Name = "Save Slots" }).ClickAsync();

        ILocator saveManager = Page.GetByRole(AriaRole.Dialog, new() { Name = "Warpath: Redemption Save Slots" });
        await saveManager.GetByRole(AriaRole.Button, new() { Name = "Edit name" }).ClickAsync();

        ILocator renameModal = Page.GetByRole(AriaRole.Dialog, new() { Name = "Rename Save Slot" });
        await renameModal.GetByLabel("Slot name").FillAsync("Campaign One");
        await renameModal.GetByRole(AriaRole.Button, new() { Name = "Save name" }).ClickAsync();
        await Expect(saveManager).ToContainTextAsync("Campaign One");

        await Page.ReloadAsync();
        await Page.GetByRole(AriaRole.Button, new() { Name = "Save Slots" }).ClickAsync();
        await Expect(Page.GetByRole(AriaRole.Dialog, new() { Name = "Warpath: Redemption Save Slots" }))
            .ToContainTextAsync("Campaign One");
    }

    [Fact]
    public async Task SnapshotCanBeLoadedAndRestoresItsSavedMission()
    {
        await StartRunAsync();
        await Page.GetByRole(AriaRole.Button, new() { Name = "Save Slots", Exact = true }).ClickAsync();
        ILocator saveManager = Page.GetByRole(AriaRole.Dialog, new() { Name = "Warpath: Redemption Save Slots" });
        await saveManager.GetByRole(AriaRole.Button, new() { Name = "Create Snapshot" }).ClickAsync();
        await saveManager.GetByRole(AriaRole.Button, new() { Name = "Close dialog" }).ClickAsync();

        await Page.GetByRole(AriaRole.Button, new() { Name = "Save Slots", Exact = true }).ClickAsync();
        saveManager = Page.GetByRole(AriaRole.Dialog, new() { Name = "Warpath: Redemption Save Slots" });
        await saveManager.GetByRole(AriaRole.Group).Last
            .GetByRole(AriaRole.Button, new() { Name = "Load" }).ClickAsync();
        await saveManager.GetByRole(AriaRole.Button, new() { Name = "Close dialog" }).ClickAsync();

        await Page.GetByRole(AriaRole.Button, new() { Name = "Mission Complete", Exact = true }).ClickAsync();
        await Page.GetByRole(AriaRole.Button, new() { Name = "3 stars", Exact = true }).ClickAsync();
        await Page.GetByRole(AriaRole.Button, new() { Name = "Continue to Rewards" }).ClickAsync();
        await Page.GetByRole(AriaRole.Dialog, new() { Name = "Reward Options" })
            .GetByRole(AriaRole.Button).Nth(1).ClickAsync();
        await Expect(Page.GetByText("Mission 2 of 2", new() { Exact = true })).ToBeVisibleAsync();

        await Page.GetByRole(AriaRole.Button, new() { Name = "Save Slots", Exact = true }).ClickAsync();
        saveManager = Page.GetByRole(AriaRole.Dialog, new() { Name = "Warpath: Redemption Save Slots" });
        ILocator snapshot = saveManager.GetByRole(AriaRole.Group).Filter(new() { HasText = "Snapshot" });
        await snapshot.GetByRole(AriaRole.Button, new() { Name = "Load" }).ClickAsync();

        await Expect(Page.GetByText("Mission 1 of 2", new() { Exact = true })).ToBeVisibleAsync();
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Level = 1 })).ToContainTextAsync("Snapshot");
    }

    [Fact]
    public async Task ExportedSaveCanBeImportedThroughTheSaveManager()
    {
        await StartRunAsync();
        await Page.GetByRole(AriaRole.Button, new() { Name = "Save Slots", Exact = true }).ClickAsync();
        ILocator saveManager = Page.GetByRole(AriaRole.Dialog, new() { Name = "Warpath: Redemption Save Slots" });
        Task<IDownload> downloadTask = Page.WaitForDownloadAsync();
        await saveManager.GetByRole(AriaRole.Button, new() { Name = "Export", Exact = true }).ClickAsync();

        IDownload download = await downloadTask;
        Assert.EndsWith(".json", download.SuggestedFilename);
        string json = await File.ReadAllTextAsync(await download.PathAsync());
        using JsonDocument export = JsonDocument.Parse(json);
        Assert.Equal("warpath-redemption", export.RootElement.GetProperty("mode").GetString());

        await saveManager.GetByLabel("Import Save").SetInputFilesAsync(new FilePayload
        {
            Name = download.SuggestedFilename,
            MimeType = "application/json",
            Buffer = Encoding.UTF8.GetBytes(json)
        });

        await Expect(Page.GetByRole(AriaRole.Status)).ToContainTextAsync("Imported slot");
        await Expect(saveManager.GetByRole(AriaRole.Group)).ToHaveCountAsync(2);
    }

    [Fact]
    public async Task SkippingRewardsBansThemAndPersistsTheBan()
    {
        await StartRunAsync();
        await CompleteFirstMissionAndOpenRewardsAsync();

        await Page.GetByRole(AriaRole.Button, new() { Name = "Skip and Ban All" }).ClickAsync();
        await Expect(Page.GetByText("Mission 2 of 2")).ToBeVisibleAsync();

        await Page.GetByRole(AriaRole.Button, new() { Name = "Banned Items" }).ClickAsync();
        ILocator bannedItems = Page.GetByRole(AriaRole.Dialog, new() { Name = "Banned Items" });
        await Expect(bannedItems).ToBeVisibleAsync();
        await Expect(bannedItems.GetByRole(AriaRole.Group, new() { Name = "Banned items" }))
            .ToBeVisibleAsync();

        await Page.ReloadAsync();
        await Page.GetByRole(AriaRole.Button, new() { Name = "Banned Items" }).ClickAsync();
        bannedItems = Page.GetByRole(AriaRole.Dialog, new() { Name = "Banned Items" });
        await Expect(bannedItems).ToBeVisibleAsync();
        await Expect(bannedItems.GetByRole(AriaRole.Group, new() { Name = "Banned items" }))
            .ToBeVisibleAsync();
    }

    private async Task CompleteFirstMissionAndOpenRewardsAsync()
    {
        await Page.GetByRole(AriaRole.Button, new() { Name = "Mission Complete" }).ClickAsync();
        await Page.GetByRole(AriaRole.Button, new() { Name = "3 stars" }).ClickAsync();
        await Page.GetByRole(AriaRole.Button, new() { Name = "Continue to Rewards" }).ClickAsync();
        await Expect(Page.GetByText("Reward Options")).ToBeVisibleAsync();
    }
}
