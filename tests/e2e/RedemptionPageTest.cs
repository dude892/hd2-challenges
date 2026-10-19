using System.Text;
using System.Text.Json;
using Microsoft.Playwright;
using Microsoft.Playwright.Xunit;

namespace Hd2Challenges.EndToEnd.Tests;

public abstract class RedemptionPageTest(WarpathWebAppFixture webApp) : PageTest
{
    protected ILocator BeginRunButton => Page.GetByRole(AriaRole.Button, new() { Name = "Begin Run", Exact = true });
    protected ILocator AcquiredBoostersRegion => Page.GetByRole(AriaRole.Region, new() { Name = "Acquired boosters", IncludeHidden = true, Exact = true });
    private protected RedemptionModals Modals => new(Page);

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

    protected async Task ImportRunAsync(string[]? acquiredItemIds = null, string[]? pendingRewardIds = null)
    {
        await OpenSetupAsync();
        await Modals.SaveSlots.OpenAsync();

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
        await Modals.SaveSlots.ImportSaveInput.SetInputFilesAsync(new FilePayload
        {
            Name = "test-run.json",
            MimeType = "application/json",
            Buffer = Encoding.UTF8.GetBytes(json)
        });

        await Expect(Page.GetByRole(AriaRole.Status)).ToContainTextAsync("Imported slot Test Run.");
        await Modals.SaveSlots.CloseAsync();
    }
}
