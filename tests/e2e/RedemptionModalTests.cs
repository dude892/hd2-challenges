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
        ILocator complete = Page.GetByRole(AriaRole.Button, new() { Name = "Mission Complete", Exact = true });
        ILocator failed = Page.GetByRole(AriaRole.Button, new() { Name = "Mission Failed", Exact = true });
        await complete.ClickAsync();
        await Page.GetByRole(AriaRole.Button, new() { Name = "3 stars", Exact = true }).ClickAsync();
        await Page.GetByRole(AriaRole.Button, new() { Name = "Continue to Rewards" }).ClickAsync();

        ILocator modal = Page.GetByRole(AriaRole.Dialog, new() { Name = "Reward Options" });
        await Expect(modal.GetByRole(AriaRole.Heading, new() { Name = "Reward Options" })).ToBeVisibleAsync();
        string[] rewards = [.. await modal.Locator(".item-card").AllTextContentsAsync()];
        await modal.GetByRole(AriaRole.Button, new() { Name = "Close dialog" }).ClickAsync();
        await Expect(failed).ToBeDisabledAsync();
        await Expect(complete).ToBeEnabledAsync();
        await Expect(Page.GetByText("Mission 1 of 2", new() { Exact = true })).ToBeVisibleAsync();

        await Page.ReloadAsync();
        await Expect(failed).ToBeDisabledAsync();
        await complete.ClickAsync();
        await Expect(modal.GetByRole(AriaRole.Heading, new() { Name = "Reward Options" })).ToBeVisibleAsync();
        await Expect(modal.Locator(".item-card")).ToHaveTextAsync(rewards);
        await Expect(Page.GetByRole(AriaRole.Button, new() { Name = "Continue to Rewards" })).ToHaveCountAsync(0);

        if (banRewards)
        {
            await modal.GetByRole(AriaRole.Button, new() { Name = "Skip and Ban All" }).ClickAsync();
        }
        else
        {
            await modal.Locator(".item-card").First.ClickAsync();
        }

        await Expect(modal).ToHaveCountAsync(0);
        await Expect(failed).ToBeEnabledAsync();
        await Expect(complete).ToBeEnabledAsync();
        await Expect(Page.GetByText("Mission 2 of 2", new() { Exact = true })).ToBeVisibleAsync();
    }

    [Fact]
    public async Task PendingPunishmentLocksMissionCompletionUntilClaimed()
    {
        await ImportRunAsync(acquiredItemIds: ["uavrecon"]);
        ILocator complete = Page.GetByRole(AriaRole.Button, new() { Name = "Mission Complete", Exact = true });
        ILocator failed = Page.GetByRole(AriaRole.Button, new() { Name = "Mission Failed", Exact = true });
        await failed.ClickAsync();

        ILocator modal = Page.GetByRole(AriaRole.Dialog, new() { Name = "Punishment Options" });
        await Expect(modal.GetByRole(AriaRole.Heading, new() { Name = "Punishment Options" })).ToBeVisibleAsync();
        await Expect(modal.Locator(".item-card")).ToHaveCountAsync(1);
        await Expect(modal.Locator(".item-card")).ToContainTextAsync("UAV Recon");
        await modal.GetByRole(AriaRole.Button, new() { Name = "Close dialog" }).ClickAsync();
        await Expect(complete).ToBeDisabledAsync();
        await Expect(failed).ToBeEnabledAsync();

        await Page.ReloadAsync();
        await Expect(complete).ToBeDisabledAsync();
        await failed.ClickAsync();
        await Expect(modal.GetByRole(AriaRole.Heading, new() { Name = "Punishment Options" })).ToBeVisibleAsync();
        await Expect(modal.Locator(".item-card")).ToHaveCountAsync(1);
        await Expect(modal.Locator(".item-card")).ToContainTextAsync("UAV Recon");
        await modal.GetByRole(AriaRole.Button, new() { Name = "UAV Recon", Exact = true }).ClickAsync();

        await Expect(modal).ToHaveCountAsync(0);
        await Expect(complete).ToBeEnabledAsync();
        await Expect(failed).ToBeEnabledAsync();
        await Expect(Page.Locator("#boosters-collapse .item-card")).ToHaveCountAsync(0);
    }

    [Theory]
    [InlineData("Save Slots", "Warpath: Redemption Save Slots")]
    [InlineData("Run Settings", "Run Settings")]
    [InlineData("Mission Complete", "Mission Complete")]
    public async Task CloseButtonDismissesModalWithoutChangingMission(string buttonName, string title)
    {
        await StartRunAsync();
        await Page.GetByRole(AriaRole.Button, new() { Name = buttonName, Exact = true }).ClickAsync();

        ILocator modal = Page.GetByRole(AriaRole.Dialog, new() { Name = title, Exact = true });
        await Expect(modal.GetByRole(AriaRole.Heading, new() { Name = title, Exact = true })).ToBeVisibleAsync();
        await modal.GetByRole(AriaRole.Button, new() { Name = "Close dialog" }).ClickAsync();

        await Expect(modal).ToHaveCountAsync(0);
        await Expect(Page.GetByText("Mission 1 of 2", new() { Exact = true })).ToBeVisibleAsync();
        await Expect(Page.GetByRole(AriaRole.Button, new() { Name = "Mission Complete", Exact = true })).ToBeEnabledAsync();
    }

    [Fact]
    public async Task RunSettingsStaysOpenForInsideClicksAndClosesOnBackdropClick()
    {
        await StartRunAsync();
        await Page.GetByRole(AriaRole.Button, new() { Name = "Run Settings", Exact = true }).ClickAsync();

        ILocator modal = Page.GetByRole(AriaRole.Dialog, new() { Name = "Run Settings", Exact = true });
        await modal.GetByRole(AriaRole.Heading, new() { Name = "Run Settings", Exact = true }).ClickAsync();
        await Expect(modal).ToBeVisibleAsync();

        await Page.Locator(".app-modal-backdrop").ClickAsync(new() { Position = new() { X = 0, Y = 0 } });
        await Expect(modal).ToHaveCountAsync(0);
    }

    [Fact]
    public async Task RenameModalValidatesNameAndCancelDiscardsChanges()
    {
        await StartRunAsync();
        await Page.GetByRole(AriaRole.Button, new() { Name = "Save Slots", Exact = true }).ClickAsync();
        ILocator saveManager = Page.GetByRole(AriaRole.Dialog, new() { Name = "Warpath: Redemption Save Slots" });
        await saveManager.GetByRole(AriaRole.Button, new() { Name = "Edit name" }).ClickAsync();

        ILocator renameModal = Page.GetByRole(AriaRole.Dialog, new() { Name = "Rename Save Slot" });
        ILocator nameInput = renameModal.GetByLabel("Slot name");
        string originalName = await nameInput.InputValueAsync();
        ILocator saveButton = renameModal.GetByRole(AriaRole.Button, new() { Name = "Save name" });
        await Expect(saveButton).ToBeDisabledAsync();
        await nameInput.FillAsync("   ");
        await Expect(saveButton).ToBeDisabledAsync();
        await nameInput.FillAsync("Unsaved modal test name");
        await Expect(saveButton).ToBeEnabledAsync();
        await renameModal.GetByRole(AriaRole.Button, new() { Name = "Cancel", Exact = true }).ClickAsync();

        await Expect(renameModal).ToHaveCountAsync(0);
        await Expect(saveManager).ToBeVisibleAsync();
        await Expect(saveManager).ToContainTextAsync(originalName);
        await Expect(saveManager).Not.ToContainTextAsync("Unsaved modal test name");
        await saveManager.GetByRole(AriaRole.Button, new() { Name = "Edit name" }).ClickAsync();
        await Expect(renameModal.GetByLabel("Slot name")).ToHaveValueAsync(originalName);
    }

    [Fact]
    public async Task DeleteConfirmationIgnoresBackdropAndCancelPreservesSave()
    {
        await StartRunAsync();
        await Page.GetByRole(AriaRole.Button, new() { Name = "Save Slots", Exact = true }).ClickAsync();
        ILocator saveManager = Page.GetByRole(AriaRole.Dialog, new() { Name = "Warpath: Redemption Save Slots" });
        await saveManager.GetByRole(AriaRole.Button, new() { Name = "Delete", Exact = true }).ClickAsync();

        ILocator confirmation = Page.GetByRole(AriaRole.Dialog, new() { Name = "Delete Current Save?", Exact = true });
        await Expect(confirmation).ToBeVisibleAsync();
        await Page.Locator(".app-modal-backdrop").Filter(new() { Has = confirmation })
            .ClickAsync(new() { Position = new() { X = 0, Y = 0 } });
        await Expect(confirmation).ToBeVisibleAsync();
        await confirmation.GetByRole(AriaRole.Button, new() { Name = "Cancel", Exact = true }).ClickAsync();

        await Expect(confirmation).ToHaveCountAsync(0);
        await Expect(saveManager.GetByRole(AriaRole.Button, new() { Name = "Delete", Exact = true })).ToBeVisibleAsync();
        await saveManager.GetByRole(AriaRole.Button, new() { Name = "Close dialog" }).ClickAsync();
        await Page.ReloadAsync();
        await Expect(Page.GetByText("Mission 1 of 2", new() { Exact = true })).ToBeVisibleAsync();
    }

    [Fact]
    public async Task ConfirmingDeleteRemovesSaveAndReturnsToSetup()
    {
        await StartRunAsync();
        await Page.GetByRole(AriaRole.Button, new() { Name = "Save Slots", Exact = true }).ClickAsync();
        ILocator saveManager = Page.GetByRole(AriaRole.Dialog, new() { Name = "Warpath: Redemption Save Slots" });
        await saveManager.GetByRole(AriaRole.Button, new() { Name = "Delete", Exact = true }).ClickAsync();
        await Page.GetByRole(AriaRole.Button, new() { Name = "Delete Save", Exact = true }).ClickAsync();

        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Delete Current Save?", Exact = true })).ToHaveCountAsync(0);
        await Expect(saveManager.GetByRole(AriaRole.Button, new() { Name = "Delete", Exact = true })).ToHaveCountAsync(0);
        await saveManager.GetByRole(AriaRole.Button, new() { Name = "Close dialog" }).ClickAsync();
        await Expect(Page.GetByRole(AriaRole.Button, new() { Name = "Begin Run", Exact = true })).ToBeVisibleAsync();

        await Page.ReloadAsync();
        await Expect(Page.GetByRole(AriaRole.Button, new() { Name = "Begin Run", Exact = true })).ToBeVisibleAsync();
    }
}
