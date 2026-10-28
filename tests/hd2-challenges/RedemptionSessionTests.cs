using System.Text.Json;
using Hd2Challenges.Models;
using Hd2Challenges.Services;

namespace Hd2Challenges.Tests;

public sealed class RedemptionSessionTests
{
    private static RedemptionSessionService CreateSession(RedemptionSaveTestContext context) =>
        new(context.Catalog.RedemptionCatalog, context.SaveLibrary);

    private static string SerializeExport(RedemptionSaveTestContext context, Guid slotId) =>
        JsonSerializer.Serialize(context.SaveLibrary.CreateExport(slotId),
            new JsonSerializerOptions(JsonSerializerDefaults.Web));

    [Fact]
    public async Task StateAccessBeforeLoadingFailsExplicitly()
    {
        using RedemptionSaveTestContext context = await RedemptionSaveTestContext.CreateAsync();
        RedemptionSessionService session = CreateSession(context);

        Assert.Throws<InvalidOperationException>(() => session.State);
    }

    [Fact]
    public async Task MissionActionBeforeLoadingFailsExplicitly()
    {
        using RedemptionSaveTestContext context = await RedemptionSaveTestContext.CreateAsync();
        RedemptionSessionService session = CreateSession(context);

        Assert.Throws<InvalidOperationException>(() => session.CompleteMission(0));
    }

    [Fact]
    public async Task LoadingCreatesAnActiveSetupState()
    {
        using RedemptionSaveTestContext context = await RedemptionSaveTestContext.CreateAsync();
        RedemptionSessionService session = CreateSession(context);

        await session.LoadAsync();

        Assert.False(session.State.RunStarted);
        Assert.Equal(context.SaveLibrary.Library.WorkingSlot!.State.DifficultyId, session.State.Difficulty.Id);
    }

    [Fact]
    public async Task LoadingSelectsAllWarbondsExposedByRedemptionCatalog()
    {
        using RedemptionSaveTestContext context = await RedemptionSaveTestContext.CreateAsync();
        RedemptionSessionService session = CreateSession(context);

        await session.LoadAsync();

        Assert.True(context.Catalog.RedemptionCatalog.GetAllWarbonds()
            .SetEquals(session.State.SelectedWarbonds));
    }

    [Fact]
    public async Task ChangingDifficultyUpdatesSetupAndPreservesWarbondSelection()
    {
        using RedemptionSaveTestContext context = await RedemptionSaveTestContext.CreateAsync();
        RedemptionSessionService session = CreateSession(context);
        await session.LoadAsync();
        Warbond optional = context.Catalog.RedemptionCatalog.GetAllWarbonds()
            .First(warbond => !warbond.AlwaysEnabled);
        session.SetWarbondSelected(optional, false);
        RedemptionState previous = session.State;
        HashSet<Warbond> selectedWarbonds = previous.SelectedWarbonds.ToHashSet();
        RedemptionDifficulty difficulty = context.Catalog.RedemptionCatalog.GetDifficulty("super");

        session.ChangeDifficulty(difficulty.Id);

        Assert.Same(previous, session.State);
        Assert.Same(difficulty, session.State.Difficulty);
        Assert.Equal(selectedWarbonds, session.State.SelectedWarbonds.ToHashSet());
        Assert.Equal(context.Catalog.RedemptionCatalog.GetStarterItems(difficulty, session.State.SelectedWarbonds).ToHashSet(),
            session.State.StarterItems.ToHashSet());
    }

    [Fact]
    public async Task SelectingWarbondUpdatesActiveSetup()
    {
        using RedemptionSaveTestContext context = await RedemptionSaveTestContext.CreateAsync();
        RedemptionSessionService session = CreateSession(context);
        await session.LoadAsync();
        Warbond optional = context.Catalog.RedemptionCatalog.GetAllWarbonds()
            .First(warbond => !warbond.AlwaysEnabled);

        Assert.True(session.SetWarbondSelected(optional, false));
        Assert.DoesNotContain(optional, session.State.SelectedWarbonds);
        Assert.Equal(context.Catalog.RedemptionCatalog.GetStarterItems(
            session.State.Difficulty, session.State.SelectedWarbonds).ToHashSet(), session.State.StarterItems.ToHashSet());
    }

    [Fact]
    public async Task BeginningRunCommitsActiveSetup()
    {
        using RedemptionSaveTestContext context = await RedemptionSaveTestContext.CreateAsync();
        RedemptionSessionService session = CreateSession(context);
        await session.LoadAsync();
        RedemptionState setup = session.State;

        await session.BeginRunAsync();

        Assert.Same(setup, session.State);
        Assert.True(session.State.RunStarted);
        Assert.Null(context.SaveLibrary.Library.WorkingSlot);
        Assert.Equal(session.State.AcquiredItems.Select(item => item.Id).ToHashSet(),
            Assert.Single(context.SaveLibrary.Library.Slots).State.AcquiredItemIds.ToHashSet());
    }

    [Fact]
    public async Task PersistingSavesActiveRunForReload()
    {
        using RedemptionSaveTestContext context = await RedemptionSaveTestContext.CreateAsync();
        RedemptionSessionService session = CreateSession(context);
        await session.LoadAsync();
        await session.BeginRunAsync();
        session.State.MissionsFailed = 2;

        await session.PersistAsync();
        await session.LoadAsync();

        Assert.Equal(2, session.State.MissionsFailed);
    }

    [Fact]
    public async Task PersistingSavesRunStatusForReload()
    {
        using RedemptionSaveTestContext context = await RedemptionSaveTestContext.CreateAsync();
        RedemptionSessionService session = CreateSession(context);
        await session.LoadAsync();
        await session.BeginRunAsync();
        session.SetRunStatus(RunStatus.Failed);

        await session.PersistAsync();
        await session.LoadAsync();

        Assert.Equal(RunStatus.Failed, session.CurrentRunStatus);
    }

    [Fact]
    public async Task RestartingReplacesRunWithSetupUsingSameSettings()
    {
        using RedemptionSaveTestContext context = await RedemptionSaveTestContext.CreateAsync();
        RedemptionSessionService session = CreateSession(context);
        await session.LoadAsync();
        session.ChangeDifficulty("super");
        await session.BeginRunAsync();
        RedemptionState previous = session.State;
        previous.MissionsFailed = 2;

        await session.RestartAsync();

        Assert.NotSame(previous, session.State);
        Assert.False(session.State.RunStarted);
        Assert.Same(previous.Difficulty, session.State.Difficulty);
        Assert.Equal(previous.SelectedWarbonds.ToHashSet(), session.State.SelectedWarbonds.ToHashSet());
        Assert.Equal(0, session.State.MissionsFailed);
    }

    [Fact]
    public async Task SelectingSlotReplacesActiveRunWithSavedState()
    {
        using RedemptionSaveTestContext context = await RedemptionSaveTestContext.CreateAsync();
        RedemptionSessionService session = CreateSession(context);
        await session.LoadAsync();
        await session.BeginRunAsync();
        Guid slotId = context.SaveLibrary.Library.CurrentSlot!.Id;
        session.State.MissionsFailed = 2;
        await session.PersistAsync();
        await session.RestartAsync();

        await session.SelectSlotAsync(slotId);

        Assert.True(session.State.RunStarted);
        Assert.Equal(2, session.State.MissionsFailed);
        Assert.Equal(slotId, context.SaveLibrary.Library.CurrentSlotId);
    }

    [Fact]
    public async Task ImportingReplacesActiveRunWithImportedState()
    {
        using RedemptionSaveTestContext context = await RedemptionSaveTestContext.CreateAsync();
        RedemptionSessionService session = CreateSession(context);
        await session.LoadAsync();
        await session.BeginRunAsync();
        session.State.MissionsFailed = 2;
        await session.PersistAsync();
        string json = SerializeExport(context, context.SaveLibrary.Library.CurrentSlot!.Id);
        await session.RestartAsync();

        SaveSlot<RedemptionStateDefinition>? imported = await session.ImportAsync(json);

        Assert.NotNull(imported);
        Assert.Equal(imported.Id, context.SaveLibrary.Library.CurrentSlotId);
        Assert.Equal(2, session.State.MissionsFailed);
        Assert.True(session.State.RunStarted);
    }

    [Fact]
    public async Task RejectedImportPreservesActiveState()
    {
        using RedemptionSaveTestContext context = await RedemptionSaveTestContext.CreateAsync();
        RedemptionSessionService session = CreateSession(context);
        await session.LoadAsync();
        RedemptionState previous = session.State;

        Assert.Null(await session.ImportAsync("{}"));
        Assert.Same(previous, session.State);
    }

    [Fact]
    public async Task ImportingUnresolvableStatePreservesActiveStateAndSavedLibrary()
    {
        using RedemptionSaveTestContext context = await RedemptionSaveTestContext.CreateAsync();
        RedemptionSessionService session = CreateSession(context);
        await session.LoadAsync();
        await session.BeginRunAsync();
        RedemptionState previous = session.State;
        Guid previousSlotId = session.CurrentSlotId!.Value;
        string json = JsonSerializer.Serialize(
            context.GenericSaveLibrary.CreateExport("warpath-redemption", new SaveSlot<RedemptionStateDefinition>
            {
                State = new RedemptionStateDefinition { DifficultyId = "unknown" }
            }),
            new JsonSerializerOptions(JsonSerializerDefaults.Web));

        await Assert.ThrowsAsync<InvalidOperationException>(() => session.ImportAsync(json));

        Assert.Same(previous, session.State);
        Assert.Equal(previousSlotId, session.CurrentSlotId);
        Assert.Single(context.SaveLibrary.Library.Slots);
        SaveLibrary<RedemptionStateDefinition>? persisted =
            await context.GenericSaveLibrary.LoadAsync<RedemptionStateDefinition>("blazor-warpath-redemption");
        Assert.Equal(previousSlotId, persisted!.CurrentSlotId);
        Assert.Equal(previousSlotId, Assert.Single(persisted.Slots).Id);
    }

    [Fact]
    public async Task SelectingUnresolvableStatePreservesActiveStateAndSavedSelection()
    {
        using RedemptionSaveTestContext context = await RedemptionSaveTestContext.CreateAsync();
        RedemptionSessionService session = CreateSession(context);
        await session.LoadAsync();
        await session.BeginRunAsync();
        RedemptionState previous = session.State;
        Guid previousSlotId = session.CurrentSlotId!.Value;
        SaveSlot<RedemptionStateDefinition> invalidSlot = new()
        {
            State = new RedemptionStateDefinition { DifficultyId = "unknown" }
        };
        context.SaveLibrary.Library.Slots.Add(invalidSlot);
        await context.GenericSaveLibrary.PersistAsync("blazor-warpath-redemption", context.SaveLibrary.Library);

        await Assert.ThrowsAsync<InvalidOperationException>(() => session.SelectSlotAsync(invalidSlot.Id));

        Assert.Same(previous, session.State);
        Assert.Equal(previousSlotId, session.CurrentSlotId);
        SaveLibrary<RedemptionStateDefinition>? persisted =
            await context.GenericSaveLibrary.LoadAsync<RedemptionStateDefinition>("blazor-warpath-redemption");
        Assert.Equal(previousSlotId, persisted!.CurrentSlotId);
        Assert.Equal(2, persisted.Slots.Count);
    }

    [Fact]
    public async Task DeletingCurrentSlotReplacesActiveStateWithSetup()
    {
        using RedemptionSaveTestContext context = await RedemptionSaveTestContext.CreateAsync();
        RedemptionSessionService session = CreateSession(context);
        await session.LoadAsync();
        await session.BeginRunAsync();
        RedemptionState previous = session.State;
        Guid slotId = context.SaveLibrary.Library.CurrentSlot!.Id;

        Assert.True(await session.DeleteSlotAsync(slotId));
        Assert.NotSame(previous, session.State);
        Assert.False(session.State.RunStarted);
        Assert.NotNull(context.SaveLibrary.Library.WorkingSlot);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DeletingNonCurrentOrMissingSlotPreservesActiveState(bool existingSlot)
    {
        using RedemptionSaveTestContext context = await RedemptionSaveTestContext.CreateAsync();
        RedemptionSessionService session = CreateSession(context);
        await session.LoadAsync();
        await session.BeginRunAsync();
        Guid oldSlotId = context.SaveLibrary.Library.CurrentSlot!.Id;
        await session.RestartAsync();
        await session.BeginRunAsync();
        RedemptionState previous = session.State;

        bool deleted = await session.DeleteSlotAsync(existingSlot ? oldSlotId : Guid.NewGuid());

        Assert.Equal(existingSlot, deleted);
        Assert.Same(previous, session.State);
    }

    [Fact]
    public async Task SnapshotUsesActiveStateWithoutReplacingIt()
    {
        using RedemptionSaveTestContext context = await RedemptionSaveTestContext.CreateAsync();
        RedemptionSessionService session = CreateSession(context);
        await session.LoadAsync();
        await session.BeginRunAsync();
        RedemptionState previous = session.State;
        previous.MissionsFailed = 2;

        await session.CreateSnapshotAsync();

        Assert.Same(previous, session.State);
        Assert.Equal(2, context.SaveLibrary.Library.Slots.Count);
        Assert.Equal(2, context.SaveLibrary.Library.CurrentSlot!.State.MissionsFailed);
    }

    [Fact]
    public async Task SnapshotPreservesCurrentRunStatus()
    {
        using RedemptionSaveTestContext context = await RedemptionSaveTestContext.CreateAsync();
        RedemptionSessionService session = CreateSession(context);
        await session.LoadAsync();
        await session.BeginRunAsync();
        session.SetRunStatus(RunStatus.Failed);

        await session.CreateSnapshotAsync();

        Assert.Equal(RunStatus.Failed, context.SaveLibrary.Library.CurrentSlot!.Status);
    }

    [Theory]
    [InlineData(RunStatus.InProgress, "Current Score:", "Current Operation:")]
    [InlineData(RunStatus.Completed, "Final Score:", "Run Completed")]
    [InlineData(RunStatus.Failed, "Final Score:", "Run Failed")]
    public async Task SummaryReflectsCurrentRunStatus(
        RunStatus status,
        string scoreLabel,
        string statusLabel)
    {
        using RedemptionSaveTestContext context = await RedemptionSaveTestContext.CreateAsync();
        RedemptionSessionService session = CreateSession(context);
        await session.LoadAsync();
        await session.BeginRunAsync();
        session.State.MissionsFailed = 2;
        session.SetRunStatus(status);

        string summary = session.GetCurrentRunSummary();

        Assert.Contains(statusLabel, summary);
        Assert.Contains($"{scoreLabel} {session.State.Difficulty.ScoreModifier - 100}", summary);
    }

    [Fact]
    public async Task SummaryIncludesSelectionsAndRuntimeItemCollections()
    {
        using RedemptionSaveTestContext context = await RedemptionSaveTestContext.CreateAsync();
        RedemptionSessionService session = CreateSession(context);
        await session.LoadAsync();
        Warbond selected = session.State.SelectedWarbonds.First();
        GameItem banned = context.Catalog.Catalog.GetAllItems()
            .First(item => !session.State.StarterItems.ContainsItem(item));
        session.State.BannedItems.AddItem(banned);

        string summary = session.GetCurrentRunSummary();

        Assert.Contains(selected.DisplayName, summary);
        Assert.Contains(banned.DisplayName, summary);
        Assert.Contains("Pending Rewards:", summary);
        Assert.Contains("Pending Punishments:", summary);
        Assert.Contains("Acquired Items:", summary);
    }

    [Theory]
    [InlineData("difficulty")]
    [InlineData("restart")]
    [InlineData("select")]
    [InlineData("import")]
    [InlineData("delete")]
    [InlineData("reload")]
    public async Task MissionActionsUseCurrentStateAfterRunReplacement(string replacement)
    {
        using RedemptionSaveTestContext context = await RedemptionSaveTestContext.CreateAsync();
        RedemptionSessionService session = CreateSession(context);
        await session.LoadAsync();
        await session.BeginRunAsync();
        RedemptionState previous = session.State;
        Guid slotId = context.SaveLibrary.Library.CurrentSlot!.Id;

        switch (replacement)
        {
            case "difficulty":
                await session.RestartAsync();
                session.ChangeDifficulty("super");
                break;
            case "restart":
                await session.RestartAsync();
                break;
            case "select":
                await session.SelectSlotAsync(slotId);
                break;
            case "import":
                Assert.NotNull(await session.ImportAsync(SerializeExport(context, slotId)));
                break;
            case "delete":
                Assert.True(await session.DeleteSlotAsync(slotId));
                break;
            case "reload":
                await session.LoadAsync();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(replacement));
        }

        Assert.NotSame(previous, session.State);
        GameItem reward = context.Catalog.Catalog.GetAllItems()
            .First(item => !session.State.StarterItems.ContainsItem(item));
        session.State.PendingRewardItems.AddItem(reward);

        Assert.True(session.ClaimMissionReward(reward.Id));
        Assert.Contains(reward, session.State.AcquiredItems);
        Assert.DoesNotContain(reward, previous.AcquiredItems);
        Assert.Empty(session.State.PendingRewardItems);
    }
}
