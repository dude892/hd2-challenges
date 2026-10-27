using System.Text.Json;
using Hd2Challenges.Models;
using Hd2Challenges.Services;

namespace Hd2Challenges.Tests;

public sealed class RedemptionSessionTests
{
    private static RedemptionSessionService CreateSession(RedemptionSaveTestContext context) =>
        new(context.Catalog.Catalog, context.Catalog.RedemptionCatalog, context.SaveLibrary);

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
        RedemptionMissionService missions = new(context.Catalog.Catalog, new(context.Catalog.Catalog, session), session);

        Assert.Throws<InvalidOperationException>(() => missions.CompleteMission(0));
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
    public async Task ChangingDifficultyUpdatesSetupAndPreservesWarbondSelection()
    {
        using RedemptionSaveTestContext context = await RedemptionSaveTestContext.CreateAsync();
        RedemptionSessionService session = CreateSession(context);
        await session.LoadAsync();
        Warbond optional = context.Catalog.Catalog.GetAllWarbonds().First(warbond => !warbond.AlwaysEnabled);
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
        Warbond optional = context.Catalog.Catalog.GetAllWarbonds().First(warbond => !warbond.AlwaysEnabled);

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
        RedemptionMissionService missions = new(context.Catalog.Catalog, new(context.Catalog.Catalog, session), session);
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

        Assert.True(missions.ClaimMissionReward(reward.Id));
        Assert.Contains(reward, session.State.AcquiredItems);
        Assert.DoesNotContain(reward, previous.AcquiredItems);
        Assert.Empty(session.State.PendingRewardItems);
    }
}
