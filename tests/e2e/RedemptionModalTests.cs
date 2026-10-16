using Microsoft.Playwright;
using Xunit;

namespace Hd2Challenges.EndToEnd.Tests;

[Collection(WarpathWebAppCollection.Name)]
public sealed class RedemptionModalTests(WarpathWebAppFixture webApp) : RedemptionPageTest(webApp)
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PendingRewardsLockMissionFailureUntilClaimedOrBanned(bool banRewards)
    {
        await StartRunAsync();
        ILocator complete = Page.GetByRole(AriaRole.Button, new() { Name = "Record mission completion", Exact = true });
        ILocator failed = Page.GetByRole(AriaRole.Button, new() { Name = "Record mission failure", Exact = true });
        await complete.ClickAsync();
        await Page.GetByRole(AriaRole.Button, new() { Name = "Set mission rating to 3 stars", Exact = true }).ClickAsync();
        await Page.GetByRole(AriaRole.Button, new() { Name = "Continue to reward selection" }).ClickAsync();

        ILocator modal = Page.GetByRole(AriaRole.Dialog, new() { Name = "Reward Options" });
        await Expect(modal.GetByRole(AriaRole.Heading, new() { Name = "Reward Options" })).ToBeVisibleAsync();
        ILocator rewardChoices = modal.GetByRole(AriaRole.Group, new() { Name = "Reward choices" });
        string[] rewards = [.. await rewardChoices.GetByRole(AriaRole.Button).AllTextContentsAsync()];
        await modal.GetByRole(AriaRole.Button, new() { Name = "Close Reward Options dialog" }).ClickAsync();
        await Expect(failed).ToBeDisabledAsync();
        await Expect(complete).ToBeEnabledAsync();
        await Expect(Page.GetByRole(AriaRole.Status, new() { Name = "Current mission progress" })).ToContainTextAsync("Mission 1 of 2");

        await Page.ReloadAsync();
        await Expect(failed).ToBeDisabledAsync();
        await complete.ClickAsync();
        await Expect(modal.GetByRole(AriaRole.Heading, new() { Name = "Reward Options" })).ToBeVisibleAsync();
        await Expect(rewardChoices.GetByRole(AriaRole.Button)).ToHaveTextAsync(rewards);
        await Expect(Page.GetByRole(AriaRole.Button, new() { Name = "Continue to reward selection" })).ToHaveCountAsync(0);

        if (banRewards)
        {
            await modal.GetByRole(AriaRole.Button, new() { Name = "Skip all rewards and ban them" }).ClickAsync();
        }
        else
        {
            await rewardChoices.GetByRole(AriaRole.Button).First.ClickAsync();
        }

        await Expect(modal).ToHaveCountAsync(0);
        await Expect(failed).ToBeEnabledAsync();
        await Expect(complete).ToBeEnabledAsync();
        await Expect(Page.GetByRole(AriaRole.Status, new() { Name = "Current mission progress" })).ToContainTextAsync("Mission 2 of 2");
    }

    [Fact]
    public async Task PendingPunishmentLocksMissionCompletionUntilClaimed()
    {
        await ImportRunAsync(acquiredItemIds: ["uavrecon"]);
        ILocator complete = Page.GetByRole(AriaRole.Button, new() { Name = "Record mission completion", Exact = true });
        ILocator failed = Page.GetByRole(AriaRole.Button, new() { Name = "Record mission failure", Exact = true });
        await failed.ClickAsync();

        ILocator modal = Page.GetByRole(AriaRole.Dialog, new() { Name = "Punishment Options" });
        await Expect(modal.GetByRole(AriaRole.Heading, new() { Name = "Punishment Options" })).ToBeVisibleAsync();
        ILocator punishmentChoices = modal.GetByRole(AriaRole.Group, new() { Name = "Punishment choices" });
        await Expect(punishmentChoices.GetByRole(AriaRole.Button)).ToHaveCountAsync(1);
        await Expect(punishmentChoices).ToContainTextAsync("UAV Recon");
        await modal.GetByRole(AriaRole.Button, new() { Name = "Close Punishment Options dialog" }).ClickAsync();
        await Expect(complete).ToBeDisabledAsync();
        await Expect(failed).ToBeEnabledAsync();

        await Page.ReloadAsync();
        await Expect(complete).ToBeDisabledAsync();
        await failed.ClickAsync();
        await Expect(modal.GetByRole(AriaRole.Heading, new() { Name = "Punishment Options" })).ToBeVisibleAsync();
        await Expect(punishmentChoices.GetByRole(AriaRole.Button)).ToHaveCountAsync(1);
        await Expect(punishmentChoices).ToContainTextAsync("UAV Recon");
        await modal.GetByRole(AriaRole.Button, new() { Name = "UAV Recon", Exact = true }).ClickAsync();

        await Expect(modal).ToHaveCountAsync(0);
        await Expect(complete).ToBeEnabledAsync();
        await Expect(failed).ToBeEnabledAsync();
        await Expect(Page.GetByRole(AriaRole.Region, new() { Name = "Acquired boosters", IncludeHidden = true })
            .GetByRole(AriaRole.Img, new() { Name = "UAV Recon", Exact = true })).ToHaveCountAsync(0);
    }

    [Theory]
    [InlineData("Open save slot manager", "Warpath: Redemption Save Slots")]
    [InlineData("Open current run settings", "Run Settings")]
    [InlineData("Record mission completion", "Mission Complete")]
    public async Task CloseButtonDismissesModalWithoutChangingMission(string accessibleName, string title)
    {
        await StartRunAsync();
        await Page.GetByRole(AriaRole.Button, new() { Name = accessibleName, Exact = true }).ClickAsync();

        ILocator modal = Page.GetByRole(AriaRole.Dialog, new() { Name = title, Exact = true });
        await Expect(modal.GetByRole(AriaRole.Heading, new() { Name = title, Exact = true })).ToBeVisibleAsync();
        await modal.GetByRole(AriaRole.Button, new() { Name = $"Close {title} dialog" }).ClickAsync();

        await Expect(modal).ToHaveCountAsync(0);
        await Expect(Page.GetByRole(AriaRole.Status, new() { Name = "Current mission progress" })).ToContainTextAsync("Mission 1 of 2");
        await Expect(Page.GetByRole(AriaRole.Button, new() { Name = "Record mission completion", Exact = true })).ToBeEnabledAsync();
    }

    [Fact]
    public async Task RunSettingsStaysOpenForInsideClicksAndClosesOnBackdropClick()
    {
        await StartRunAsync();
        await Page.GetByRole(AriaRole.Button, new() { Name = "Open current run settings", Exact = true }).ClickAsync();

        ILocator modal = Page.GetByRole(AriaRole.Dialog, new() { Name = "Run Settings", Exact = true });
        await modal.GetByRole(AriaRole.Heading, new() { Name = "Run Settings", Exact = true }).ClickAsync();
        await Expect(modal).ToBeVisibleAsync();

        await modal.Locator("xpath=..").ClickAsync(new() { Position = new() { X = 0, Y = 0 } });
        await Expect(modal).ToHaveCountAsync(0);
    }

    [Fact]
    public async Task RenameModalValidatesNameAndCancelDiscardsChanges()
    {
        await StartRunAsync();
        await Page.GetByRole(AriaRole.Button, new() { Name = "Open save slot manager", Exact = true }).ClickAsync();
        ILocator saveManager = Page.GetByRole(AriaRole.Dialog, new() { Name = "Warpath: Redemption Save Slots" });
        await saveManager.GetByRole(AriaRole.Button, new() { Name = "Rename this save slot" }).ClickAsync();

        ILocator renameModal = Page.GetByRole(AriaRole.Dialog, new() { Name = "Rename Save Slot" });
        ILocator nameInput = renameModal.GetByLabel("New save slot name");
        string originalName = await nameInput.InputValueAsync();
        ILocator saveButton = renameModal.GetByRole(AriaRole.Button, new() { Name = "Save new save slot name" });
        await Expect(saveButton).ToBeDisabledAsync();
        await nameInput.FillAsync("   ");
        await Expect(saveButton).ToBeDisabledAsync();
        await nameInput.FillAsync("Unsaved modal test name");
        await Expect(saveButton).ToBeEnabledAsync();
        await renameModal.GetByRole(AriaRole.Button, new() { Name = "Cancel save slot rename", Exact = true }).ClickAsync();

        await Expect(renameModal).ToHaveCountAsync(0);
        await Expect(saveManager).ToBeVisibleAsync();
        await Expect(saveManager).ToContainTextAsync(originalName);
        await Expect(saveManager).Not.ToContainTextAsync("Unsaved modal test name");
        await saveManager.GetByRole(AriaRole.Button, new() { Name = "Rename this save slot" }).ClickAsync();
        await Expect(renameModal.GetByLabel("New save slot name")).ToHaveValueAsync(originalName);
    }

    [Fact]
    public async Task DeleteConfirmationIgnoresBackdropAndCancelPreservesSave()
    {
        await StartRunAsync();
        await Page.GetByRole(AriaRole.Button, new() { Name = "Open save slot manager", Exact = true }).ClickAsync();
        ILocator saveManager = Page.GetByRole(AriaRole.Dialog, new() { Name = "Warpath: Redemption Save Slots" });
        await saveManager.GetByRole(AriaRole.Button, new() { Name = "Delete this save slot", Exact = true }).ClickAsync();

        ILocator confirmation = Page.GetByRole(AriaRole.Dialog, new() { Name = "Delete Current Save?", Exact = true });
        await Expect(confirmation).ToBeVisibleAsync();
        await confirmation.Locator("xpath=..").ClickAsync(new() { Position = new() { X = 0, Y = 0 } });
        await Expect(confirmation).ToBeVisibleAsync();
        await confirmation.GetByRole(AriaRole.Button, new() { Name = "Cancel save deletion", Exact = true }).ClickAsync();

        await Expect(confirmation).ToHaveCountAsync(0);
        await Expect(saveManager.GetByRole(AriaRole.Button, new() { Name = "Delete this save slot", Exact = true })).ToBeVisibleAsync();
        await saveManager.GetByRole(AriaRole.Button, new() { Name = "Close Warpath: Redemption Save Slots dialog" }).ClickAsync();
        await Page.ReloadAsync();
        await Expect(Page.GetByRole(AriaRole.Status, new() { Name = "Current mission progress" })).ToContainTextAsync("Mission 1 of 2");
    }

    [Fact]
    public async Task ConfirmingDeleteRemovesSaveAndReturnsToSetup()
    {
        await StartRunAsync();
        await Page.GetByRole(AriaRole.Button, new() { Name = "Open save slot manager", Exact = true }).ClickAsync();
        ILocator saveManager = Page.GetByRole(AriaRole.Dialog, new() { Name = "Warpath: Redemption Save Slots" });
        await saveManager.GetByRole(AriaRole.Button, new() { Name = "Delete this save slot", Exact = true }).ClickAsync();
        await Page.GetByRole(AriaRole.Button, new() { Name = "Confirm deleting this save", Exact = true }).ClickAsync();

        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Delete Current Save?", Exact = true })).ToHaveCountAsync(0);
        await Expect(saveManager.GetByRole(AriaRole.Button, new() { Name = "Delete this save slot", Exact = true })).ToHaveCountAsync(0);
        await saveManager.GetByRole(AriaRole.Button, new() { Name = "Close Warpath: Redemption Save Slots dialog" }).ClickAsync();
        await Expect(Page.GetByRole(AriaRole.Button, new() { Name = "Begin Warpath: Redemption run", Exact = true })).ToBeVisibleAsync();

        await Page.ReloadAsync();
        await Expect(Page.GetByRole(AriaRole.Button, new() { Name = "Begin Warpath: Redemption run", Exact = true })).ToBeVisibleAsync();
    }
}
