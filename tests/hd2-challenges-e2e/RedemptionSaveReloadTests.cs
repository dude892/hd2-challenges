using System.Text.Json;
using Hd2Challenges.Models;
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
        RedemptionDifficulty difficulty = RedemptionCatalog.GetDifficulty("normal");
        Operation operation = difficulty.StartOperation;
        await Expect(CurrentOperationProgress).ToHaveAttributeAsync("data-operation-index", operation.DifficultyIndex.ToString());
        await Modals.MissionComplete.OpenAsync();
        await Modals.MissionComplete.ThreeStarsButton.ClickAsync();
        await Modals.RewardOptions.OpenAsync();
        await GetValidatedRewardItemsAsync();
        string claimedRewardId = (await Modals.RewardOptions.Choices.SelectableChoices.Nth(1).GetAttributeAsync("data-item-id"))!;
        await Modals.RewardOptions.Choices.SelectableChoices.Nth(1).ClickAsync();

        await Expect(CurrentMissionProgress).ToHaveAttributeAsync("data-mission-number", "2");
        await Expect(CurrentMissionProgress).ToHaveAttributeAsync("data-mission-count", operation.MissionCount.ToString());
        await ReloadAsync();
        await Expect(CurrentOperationProgress).ToHaveAttributeAsync("data-operation-index", operation.DifficultyIndex.ToString());
        await Expect(CurrentMissionProgress).ToHaveAttributeAsync("data-mission-number", "2");
        await Expect(CurrentMissionProgress).ToHaveAttributeAsync("data-mission-count", operation.MissionCount.ToString());
        await ItemGrids.AcquiredItems.AssertContainsItemsAsync(claimedRewardId);

        await Modals.SaveSlots.OpenAsync();
        await Expect(Modals.SaveSlots.Dialog).ToContainTextAsync($"{difficulty.DisplayName} |");
    }

    [Fact]
    public async Task AdvancingPastTheLastMissionUpdatesTheOperationIndex()
    {
        SaveObject save = SaveObject.OperationLastMission;
        RedemptionState state = save.ResolveState(RedemptionCatalog);
        Assert.True(state.CurrentOperation.IsLastMission);
        await ImportRunAsync(save);
        await Expect(CurrentOperationProgress).ToHaveAttributeAsync("data-operation-index", state.CurrentOperation.DifficultyIndex.ToString());

        await Modals.MissionComplete.OpenAsync();
        await Modals.MissionComplete.ThreeStarsButton.ClickAsync();
        await Modals.RewardOptions.OpenAsync();
        await GetValidatedRewardItemsAsync();
        await Modals.RewardOptions.Choices.SelectableChoices.First.ClickAsync();

        await Expect(CurrentOperationProgress).ToHaveAttributeAsync("data-operation-index", "4");
        await Expect(CurrentMissionProgress).ToHaveAttributeAsync("data-mission-number", "1");
        await ReloadAsync();
        await Expect(CurrentOperationProgress).ToHaveAttributeAsync("data-operation-index", "4");
        await Expect(CurrentMissionProgress).ToHaveAttributeAsync("data-mission-number", "1");
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

        await ReloadAsync();
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
        await GetValidatedRewardItemsAsync();
        await Modals.RewardOptions.Choices.SelectableChoices.Nth(1).ClickAsync();
        await Expect(CurrentMissionProgress).ToHaveAttributeAsync("data-mission-number", "2");
        await Expect(CurrentMissionProgress).ToHaveAttributeAsync("data-mission-count", DefaultStartOperation.MissionCount.ToString());

        await Modals.SaveSlots.OpenAsync();
        ILocator snapshot = Modals.SaveSlots.Slots.Filter(new() { HasText = "Snapshot" });
        await snapshot.GetByRole(AriaRole.Button, new() { Name = "Load" }).ClickAsync();

        await Expect(CurrentMissionProgress).ToHaveAttributeAsync("data-mission-number", "1");
        await Expect(CurrentMissionProgress).ToHaveAttributeAsync("data-mission-count", DefaultStartOperation.MissionCount.ToString());
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
        string downloadPath = await download.PathAsync();
        string json = await File.ReadAllTextAsync(downloadPath, TestContext.Current.CancellationToken);
        using JsonDocument export = JsonDocument.Parse(json);
        Assert.Equal("warpath-redemption", export.RootElement.GetProperty("mode").GetString());
        await Modals.SaveSlots.ImportSaveInput.SetInputFilesAsync(downloadPath);

        await Expect(ImportedSaveStatus).ToBeVisibleAsync();
        await Expect(Modals.SaveSlots.Slots).ToHaveCountAsync(2);
    }

    [Fact]
    public async Task SkippingRewardsBansThemAndPersistsTheBan()
    {
        await StartRunAsync();
        await CompleteFirstMissionAndOpenRewardsAsync();

        ItemSet rewards = await GetValidatedRewardItemsAsync();
        await Modals.RewardOptions.SkipAndBanAllButton.ClickAsync();
        await Expect(CurrentMissionProgress).ToHaveAttributeAsync("data-mission-number", "2");
        await Expect(CurrentMissionProgress).ToHaveAttributeAsync("data-mission-count", DefaultStartOperation.MissionCount.ToString());

        await Modals.BannedItems.OpenAsync();
        await Expect(Modals.BannedItems.Dialog).ToBeVisibleAsync();
        await Expect(Modals.BannedItems.Items.Root).ToBeVisibleAsync();
        await Modals.BannedItems.Items.AssertMatchesItemsAsync(rewards);

        await ReloadAsync();
        await Modals.BannedItems.OpenAsync();
        await Expect(Modals.BannedItems.Dialog).ToBeVisibleAsync();
        await Expect(Modals.BannedItems.Items.Root).ToBeVisibleAsync();
        await Modals.BannedItems.Items.AssertMatchesItemsAsync(rewards);
    }

    private async Task CompleteFirstMissionAndOpenRewardsAsync()
    {
        await Modals.MissionComplete.OpenAsync();
        await Modals.MissionComplete.ThreeStarsButton.ClickAsync();
        await Modals.RewardOptions.OpenAsync();
        await Expect(Modals.RewardOptions.Dialog).ToBeVisibleAsync();
    }
}
