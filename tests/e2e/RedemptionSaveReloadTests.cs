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
        await Modals.MissionComplete.OpenAsync();
        await Modals.MissionComplete.ThreeStarsButton.ClickAsync();
        await Modals.RewardOptions.OpenAsync();
        await Modals.RewardOptions.Choices.GetByRole(AriaRole.Button).Nth(1).ClickAsync();

        await Expect(CurrentMissionProgress).ToHaveAttributeAsync("data-mission-number", "2");
        await Expect(CurrentMissionProgress).ToHaveAttributeAsync("data-mission-count", "2");
        await Page.ReloadAsync();
        await Expect(CurrentMissionProgress).ToHaveAttributeAsync("data-mission-number", "2");
        await Expect(CurrentMissionProgress).ToHaveAttributeAsync("data-mission-count", "2");

        await Modals.SaveSlots.OpenAsync();
        await Expect(Modals.SaveSlots.Dialog).ToContainTextAsync("Normal Warpath |");
    }

    [Fact]
    public async Task RenamingSaveSlotPersistsAfterReload()
    {
        await StartRunAsync();
        await Modals.SaveSlots.OpenAsync();
        await Modals.RenameSlot.OpenAsync();

        await Modals.RenameSlot.SlotNameInput.FillAsync("Campaign One");
        await Modals.RenameSlot.SaveNameButton.ClickAsync();
        await Expect(Modals.SaveSlots.Dialog).ToContainTextAsync("Campaign One");

        await Page.ReloadAsync();
        await Modals.SaveSlots.OpenAsync();
        await Expect(Modals.SaveSlots.Dialog).ToContainTextAsync("Campaign One");
    }

    [Fact]
    public async Task SnapshotCanBeLoadedAndRestoresItsSavedMission()
    {
        await StartRunAsync();
        await Modals.SaveSlots.OpenAsync();
        await Modals.SaveSlots.CreateSnapshotButton.ClickAsync();
        await Modals.SaveSlots.CloseAsync();

        await Modals.SaveSlots.OpenAsync();
        await Modals.SaveSlots.Slots.Last
            .GetByRole(AriaRole.Button, new() { Name = "Load" }).ClickAsync();
        await Modals.SaveSlots.CloseAsync();

        await Modals.MissionComplete.OpenAsync();
        await Modals.MissionComplete.ThreeStarsButton.ClickAsync();
        await Modals.RewardOptions.OpenAsync();
        await Modals.RewardOptions.Choices.GetByRole(AriaRole.Button).Nth(1).ClickAsync();
        await Expect(CurrentMissionProgress).ToHaveAttributeAsync("data-mission-number", "2");
        await Expect(CurrentMissionProgress).ToHaveAttributeAsync("data-mission-count", "2");

        await Modals.SaveSlots.OpenAsync();
        ILocator snapshot = Modals.SaveSlots.Slots.Filter(new() { HasText = "Snapshot" });
        await snapshot.GetByRole(AriaRole.Button, new() { Name = "Load" }).ClickAsync();

        await Expect(CurrentMissionProgress).ToHaveAttributeAsync("data-mission-number", "1");
        await Expect(CurrentMissionProgress).ToHaveAttributeAsync("data-mission-count", "2");
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Level = 1 })).ToContainTextAsync("Snapshot");
    }

    [Fact]
    public async Task ExportedSaveCanBeImportedThroughTheSaveManager()
    {
        await StartRunAsync();
        await Modals.SaveSlots.OpenAsync();
        Task<IDownload> downloadTask = Page.WaitForDownloadAsync();
        await Modals.SaveSlots.ExportButton.ClickAsync();

        IDownload download = await downloadTask;
        Assert.EndsWith(".json", download.SuggestedFilename);
        string json = await File.ReadAllTextAsync(await download.PathAsync());
        using JsonDocument export = JsonDocument.Parse(json);
        Assert.Equal("warpath-redemption", export.RootElement.GetProperty("mode").GetString());
        string importedSlotName = export.RootElement.GetProperty("slot").GetProperty("name").GetString()!;

        await Modals.SaveSlots.ImportSaveInput.SetInputFilesAsync(new FilePayload
        {
            Name = download.SuggestedFilename,
            MimeType = "application/json",
            Buffer = Encoding.UTF8.GetBytes(json)
        });

        await Expect(ImportedSaveStatus).ToBeVisibleAsync();
        await Expect(Modals.SaveSlots.Slots).ToHaveCountAsync(2);
    }

    [Fact]
    public async Task SkippingRewardsBansThemAndPersistsTheBan()
    {
        await StartRunAsync();
        await CompleteFirstMissionAndOpenRewardsAsync();

        await Modals.RewardOptions.SkipAndBanAllButton.ClickAsync();
        await Expect(CurrentMissionProgress).ToHaveAttributeAsync("data-mission-number", "2");
        await Expect(CurrentMissionProgress).ToHaveAttributeAsync("data-mission-count", "2");

        await Modals.BannedItems.OpenAsync();
        await Expect(Modals.BannedItems.Dialog).ToBeVisibleAsync();
        await Expect(Modals.BannedItems.Items).ToBeVisibleAsync();

        await Page.ReloadAsync();
        await Modals.BannedItems.OpenAsync();
        await Expect(Modals.BannedItems.Dialog).ToBeVisibleAsync();
        await Expect(Modals.BannedItems.Items).ToBeVisibleAsync();
    }

    private async Task CompleteFirstMissionAndOpenRewardsAsync()
    {
        await Modals.MissionComplete.OpenAsync();
        await Modals.MissionComplete.ThreeStarsButton.ClickAsync();
        await Modals.RewardOptions.OpenAsync();
        await Expect(Modals.RewardOptions.Dialog).ToBeVisibleAsync();
    }
}
