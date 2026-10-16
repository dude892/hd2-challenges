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
        await Page.GetByRole(AriaRole.Button, new() { Name = "Record mission completion" }).ClickAsync();
        await Page.GetByRole(AriaRole.Button, new() { Name = "Set mission rating to 3 stars" }).ClickAsync();
        await Page.GetByRole(AriaRole.Button, new() { Name = "Continue to reward selection" }).ClickAsync();
        await Page.GetByRole(AriaRole.Dialog, new() { Name = "Reward Options" })
            .GetByRole(AriaRole.Group, new() { Name = "Reward choices" }).GetByRole(AriaRole.Button).First.ClickAsync();

        await Expect(Page.GetByRole(AriaRole.Status, new() { Name = "Current mission progress" })).ToContainTextAsync("Mission 2 of 2");
        await Page.ReloadAsync();
        await Expect(Page.GetByRole(AriaRole.Status, new() { Name = "Current mission progress" })).ToContainTextAsync("Mission 2 of 2");

        await Page.GetByRole(AriaRole.Button, new() { Name = "Open save slot manager" }).ClickAsync();
        await Expect(Page.GetByRole(AriaRole.Dialog, new() { Name = "Warpath: Redemption Save Slots" }))
            .ToContainTextAsync("Normal Warpath |");
    }

    [Fact]
    public async Task RenamingSaveSlotPersistsAfterReload()
    {
        await StartRunAsync();
        await Page.GetByRole(AriaRole.Button, new() { Name = "Open save slot manager" }).ClickAsync();

        ILocator saveManager = Page.GetByRole(AriaRole.Dialog, new() { Name = "Warpath: Redemption Save Slots" });
        await saveManager.GetByRole(AriaRole.Button, new() { Name = "Rename this save slot" }).ClickAsync();

        ILocator renameModal = Page.GetByRole(AriaRole.Dialog, new() { Name = "Rename Save Slot" });
        await renameModal.GetByLabel("Slot name").FillAsync("Campaign One");
        await renameModal.GetByRole(AriaRole.Button, new() { Name = "Save new save slot name" }).ClickAsync();
        await Expect(saveManager).ToContainTextAsync("Campaign One");

        await Page.ReloadAsync();
        await Page.GetByRole(AriaRole.Button, new() { Name = "Open save slot manager" }).ClickAsync();
        await Expect(Page.GetByRole(AriaRole.Dialog, new() { Name = "Warpath: Redemption Save Slots" }))
            .ToContainTextAsync("Campaign One");
    }

    [Fact]
    public async Task SnapshotCanBeLoadedAndRestoresItsSavedMission()
    {
        await StartRunAsync();
        await Page.GetByRole(AriaRole.Button, new() { Name = "Open save slot manager", Exact = true }).ClickAsync();
        ILocator saveManager = Page.GetByRole(AriaRole.Dialog, new() { Name = "Warpath: Redemption Save Slots" });
        await saveManager.GetByRole(AriaRole.Button, new() { Name = "Create save snapshot" }).ClickAsync();
        await saveManager.GetByRole(AriaRole.Button, new() { Name = "Close Warpath: Redemption Save Slots dialog" }).ClickAsync();

        await Page.GetByRole(AriaRole.Button, new() { Name = "Open save slot manager", Exact = true }).ClickAsync();
        saveManager = Page.GetByRole(AriaRole.Dialog, new() { Name = "Warpath: Redemption Save Slots" });
        await saveManager.GetByRole(AriaRole.Group).Last
            .GetByRole(AriaRole.Button, new() { Name = "Load this save slot" }).ClickAsync();
        await saveManager.GetByRole(AriaRole.Button, new() { Name = "Close Warpath: Redemption Save Slots dialog" }).ClickAsync();

        await Page.GetByRole(AriaRole.Button, new() { Name = "Record mission completion", Exact = true }).ClickAsync();
        await Page.GetByRole(AriaRole.Button, new() { Name = "Set mission rating to 3 stars", Exact = true }).ClickAsync();
        await Page.GetByRole(AriaRole.Button, new() { Name = "Continue to reward selection" }).ClickAsync();
        await Page.GetByRole(AriaRole.Dialog, new() { Name = "Reward Options" })
            .GetByRole(AriaRole.Group, new() { Name = "Reward choices" }).GetByRole(AriaRole.Button).First.ClickAsync();
        await Expect(Page.GetByRole(AriaRole.Status, new() { Name = "Current mission progress" })).ToContainTextAsync("Mission 2 of 2");

        await Page.GetByRole(AriaRole.Button, new() { Name = "Open save slot manager", Exact = true }).ClickAsync();
        saveManager = Page.GetByRole(AriaRole.Dialog, new() { Name = "Warpath: Redemption Save Slots" });
        ILocator snapshot = saveManager.GetByRole(AriaRole.Group).Filter(new() { HasText = "Snapshot" });
        await snapshot.GetByRole(AriaRole.Button, new() { Name = "Load this save slot" }).ClickAsync();

        await Expect(Page.GetByRole(AriaRole.Status, new() { Name = "Current mission progress" })).ToContainTextAsync("Mission 1 of 2");
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Level = 1 })).ToContainTextAsync("Snapshot");
    }

    [Fact]
    public async Task ExportedSaveCanBeImportedThroughTheSaveManager()
    {
        await StartRunAsync();
        await Page.GetByRole(AriaRole.Button, new() { Name = "Open save slot manager", Exact = true }).ClickAsync();
        ILocator saveManager = Page.GetByRole(AriaRole.Dialog, new() { Name = "Warpath: Redemption Save Slots" });
        Task<IDownload> downloadTask = Page.WaitForDownloadAsync();
        await saveManager.GetByRole(AriaRole.Button, new() { Name = "Export this save slot", Exact = true }).ClickAsync();

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

        await Expect(Page.GetByRole(AriaRole.Status, new() { Name = export.RootElement.GetProperty("slot").GetProperty("name").GetString() }))
            .ToBeVisibleAsync();
        await Expect(saveManager.GetByRole(AriaRole.Group)).ToHaveCountAsync(2);
    }

    [Fact]
    public async Task SkippingRewardsBansThemAndPersistsTheBan()
    {
        await StartRunAsync();
        await CompleteFirstMissionAndOpenRewardsAsync();

        await Page.GetByRole(AriaRole.Button, new() { Name = "Skip all rewards and ban them" }).ClickAsync();
        await Expect(Page.GetByRole(AriaRole.Status, new() { Name = "Current mission progress" })).ToContainTextAsync("Mission 2 of 2");

        await Page.GetByRole(AriaRole.Button, new() { Name = "View banned items" }).ClickAsync();
        ILocator bannedItems = Page.GetByRole(AriaRole.Dialog, new() { Name = "Banned Items" });
        await Expect(bannedItems).ToBeVisibleAsync();
        await Expect(bannedItems.GetByRole(AriaRole.Group, new() { Name = "Banned items" }))
            .ToBeVisibleAsync();

        await Page.ReloadAsync();
        await Page.GetByRole(AriaRole.Button, new() { Name = "View banned items" }).ClickAsync();
        bannedItems = Page.GetByRole(AriaRole.Dialog, new() { Name = "Banned Items" });
        await Expect(bannedItems).ToBeVisibleAsync();
        await Expect(bannedItems.GetByRole(AriaRole.Group, new() { Name = "Banned items" }))
            .ToBeVisibleAsync();
    }

    private async Task CompleteFirstMissionAndOpenRewardsAsync()
    {
        await Page.GetByRole(AriaRole.Button, new() { Name = "Record mission completion" }).ClickAsync();
        await Page.GetByRole(AriaRole.Button, new() { Name = "Set mission rating to 3 stars" }).ClickAsync();
        await Page.GetByRole(AriaRole.Button, new() { Name = "Continue to reward selection" }).ClickAsync();
        await Expect(Page.GetByRole(AriaRole.Dialog, new() { Name = "Reward Options" })).ToBeVisibleAsync();
    }
}
