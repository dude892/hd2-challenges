using System.Text;
using System.Text.Json;
using Microsoft.Playwright;
using Microsoft.Playwright.Xunit;

namespace Hd2Challenges.EndToEnd.Tests;

public abstract class RedemptionPageTest(WarpathWebAppFixture webApp) : PageTest
{
    private protected RedemptionModals Modals => new(Page);
    protected ILocator BeginRunButton => Page.GetByRole(AriaRole.Button, new() { Name = "Begin Warpath: Redemption run", Exact = true });
    protected ILocator CurrentMissionProgress => Page.GetByRole(AriaRole.Status, new() { Name = "Current mission progress", Exact = true });
    protected ILocator ImportedSaveStatus => Page.GetByRole(AriaRole.Status, new() { NameRegex = new System.Text.RegularExpressions.Regex("^Imported slot") });
    protected ILocator BoosterAccordionToggle => Page.GetByRole(AriaRole.Button, new() { Name = "Toggle acquired boosters", Exact = true });
    protected ILocator AcquiredBoostersRegion => Page.GetByRole(AriaRole.Region, new() { Name = "Acquired boosters", IncludeHidden = true, Exact = true });

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

        await Expect(ImportedSaveStatus).ToContainTextAsync("Imported slot Test Run.");
        await Modals.SaveSlots.CloseAsync();
    }
}
