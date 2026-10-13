using Bunit;
using Hd2Challenges.Components;
using Hd2Challenges.Models;
using Hd2Challenges.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace Hd2Challenges.Tests;

public sealed class ComponentTests
{
    [Fact]
    public void WarbondCardReflectsSelectionAndInvokesClickCallback()
    {
        using BunitContext context = new();
        int clickCount = 0;
        Warbond warbond = new() { Id = "test-warbond", DisplayName = "Test Warbond" };

        IRenderedComponent<WarbondCard> component = context.Render<WarbondCard>(parameters => parameters
            .Add(card => card.Warbond, warbond)
            .Add(card => card.IsSelected, true)
            .Add(card => card.OnClick, EventCallback.Factory.Create(this, () => clickCount++)));

        Assert.Contains("warbond-card-selected", component.Find("button").ClassName);
        Assert.Contains("Test Warbond", component.Markup);

        component.Find("button").Click();

        Assert.Equal(1, clickCount);
    }

    [Fact]
    public void ToastComponentRendersAndDismissesServiceNotifications()
    {
        using BunitContext context = new();
        ToastService toasts = new();
        context.Services.AddSingleton(toasts);
        IRenderedComponent<Toast> component = context.Render<Toast>();

        toasts.Show("Save completed.");

        Assert.Contains("Save completed.", component.Markup);
        component.Find("button[aria-label='Dismiss notification']").Click();

        Assert.Empty(toasts.Messages);
        Assert.DoesNotContain("Save completed.", component.Markup);
    }

    [Fact]
    public async Task RedemptionSetupRendersSetupAndForwardsUserActions()
    {
        using CatalogTestContext catalogContext = await CatalogTestContext.CreateAsync();
        using BunitContext context = new();
        RedemptionState state = catalogContext.RedemptionCatalog.CreateRedemptionSetupState(
            catalogContext.RedemptionCatalog.GetDifficulty("normal"),
            catalogContext.Catalog.GetAllWarbonds());
        int beginCount = 0;
        Warbond? toggledWarbond = null;
        ChangeEventArgs? difficultyChange = null;

        IRenderedComponent<RedemptionSetup> component = context.Render<RedemptionSetup>(parameters => parameters
            .Add(setup => setup.State, state)
            .Add(setup => setup.Catalog, catalogContext.Catalog)
            .Add(setup => setup.RedemptionCatalog, catalogContext.RedemptionCatalog)
            .Add(setup => setup.OnBeginRun, EventCallback.Factory.Create(this, () => beginCount++))
            .Add(setup => setup.OnWarbondToggle, EventCallback.Factory.Create<Warbond>(this, warbond => toggledWarbond = warbond))
            .Add(setup => setup.OnDifficultyChanged, EventCallback.Factory.Create<ChangeEventArgs>(this, args => difficultyChange = args)));

        Assert.Contains("Starting Loadout Preview", component.Markup);
        component.Find("button.btn-success").Click();
        component.Find("button.warbond-card-clickable").Click();
        component.Find("select").Change("super");

        Assert.Equal(1, beginCount);
        Assert.NotNull(toggledWarbond);
        Assert.Equal("super", difficultyChange?.Value);
    }

    [Fact]
    public async Task RedemptionActiveRunAndRewardComponentExposeActionsWithoutOwningTransitions()
    {
        using CatalogTestContext catalogContext = await CatalogTestContext.CreateAsync();
        using BunitContext context = new();
        RedemptionState state = catalogContext.RedemptionCatalog.CreateRedemptionSetupState(
            catalogContext.RedemptionCatalog.GetDifficulty("normal"),
            catalogContext.Catalog.GetAllWarbonds());
        state.RunStarted = true;
        state.AcquiredItems.AddItems(state.StarterItems);
        GameItem reward = catalogContext.Catalog.GetAllItems().First(item => !state.StarterItems.ContainsItem(item));
        state.PendingRewardItems.AddItem(reward);
        int missionCompleteCount = 0;
        string? claimedRewardId = null;

        IRenderedComponent<RedemptionActiveRun> activeRun = context.Render<RedemptionActiveRun>(parameters => parameters
            .Add(run => run.State, state)
            .Add(run => run.Catalog, catalogContext.Catalog)
            .Add(run => run.OnMissionComplete, EventCallback.Factory.Create(this, () => missionCompleteCount++)));
        IRenderedComponent<RedemptionRewards> rewards = context.Render<RedemptionRewards>(parameters => parameters
            .Add(dialogs => dialogs.State, state)
            .Add(dialogs => dialogs.Catalog, catalogContext.Catalog)
            .Add(dialogs => dialogs.ShowRewardOptions, true)
            .Add(dialogs => dialogs.OnClaimReward, EventCallback.Factory.Create<string>(this, id => claimedRewardId = id)));

        Assert.Contains(state.CurrentOperation.MissionLabel, activeRun.Markup);
        activeRun.Find("button.btn-success").Click();
        rewards.Find("button.item-card-clickable").Click();

        Assert.Equal(1, missionCompleteCount);
        Assert.Equal(reward.Id, claimedRewardId);
    }
}
