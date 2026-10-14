using System.Text;
using System.Text.Json;
using Microsoft.Playwright;
using Microsoft.Playwright.Xunit;

namespace Hd2Challenges.EndToEnd.Tests;

public abstract class RedemptionPageTest(WarpathWebAppFixture webApp) : PageTest
{
    protected async Task OpenSetupAsync()
    {
        await Page.GotoAsync($"{webApp.BaseUrl}/warpath-redemption");
        await Page.GetByRole(AriaRole.Button, new() { Name = "Begin Run" }).WaitForAsync();
    }

    protected async Task StartRunAsync()
    {
        await OpenSetupAsync();
        await Page.GetByRole(AriaRole.Button, new() { Name = "Begin Run" }).ClickAsync();
    }

    protected async Task ImportRunAsync(string[]? acquiredItemIds = null, string[]? pendingRewardIds = null)
    {
        await OpenSetupAsync();
        await Page.GetByRole(AriaRole.Button, new() { Name = "Save Slots" }).ClickAsync();

        object saveExport = new
        {
            Version = 1,
            Mode = "warpath-redemption",
            Slot = new
            {
                Name = "Test Run",
                State = new
                {
                    DifficultyId = "normal",
                    OperationIndex = 3,
                    MissionNumber = 1,
                    AcquiredItemIds = acquiredItemIds ?? [],
                    PendingRewardIds = pendingRewardIds ?? []
                }
            }
        };
        string json = JsonSerializer.Serialize(saveExport, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        await Page.Locator(".app-modal.save-slot-modal input[type=file]").SetInputFilesAsync(new FilePayload
        {
            Name = "test-run.json",
            MimeType = "application/json",
            Buffer = Encoding.UTF8.GetBytes(json)
        });

        await Expect(Page.Locator(".toast-body")).ToContainTextAsync("Imported slot Test Run.");
        await Page.Locator(".app-modal.save-slot-modal")
            .GetByRole(AriaRole.Button, new() { Name = "×" }).ClickAsync();
    }
}
