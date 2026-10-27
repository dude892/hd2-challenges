using AngleSharp.Dom;
using Bunit;
using Hd2Challenges.Components;
using Hd2Challenges.Models;
using Hd2Challenges.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace Hd2Challenges.Tests;

public sealed class ComponentTests
{
    private static RenderFragment CreateStatCards(params (string Label, string Value)[] cards) => builder =>
    {
        foreach (var card in cards)
        {
            builder.OpenComponent<StatCard>(0);
            builder.AddAttribute(1, nameof(StatCard.Label), card.Label);
            builder.AddAttribute(2, nameof(StatCard.ChildContent), (RenderFragment)(content => content.AddContent(0, card.Value)));
            builder.CloseComponent();
        }
    };

    [Fact]
    public void StatCardRowRendersCardsAndCustomContentInTheSameRow()
    {
        using BunitContext context = new();
        IRenderedComponent<StatCardRow> component = context.Render<StatCardRow>(parameters => parameters
            .Add(row => row.CssClass, "mb-3 align-items-center")
            .Add(row => row.ColumnClass, "col-md-2")
            .Add(row => row.Cards, CreateStatCards(("Successes", "3"), ("Failures", "1")))
            .Add(row => row.AdditionalContent, builder => builder.AddMarkupContent(0, "<div class='col-md-4'><button>Continue</button></div>")));

        Assert.Equal(2, component.FindAll(".row.g-3.mb-3.align-items-center > .col-md-2 > .card").Count);
        Assert.Equal(["Successes", "Failures"], component.FindAll(".card-body > .text-secondary").Select(label => label.TextContent));
        Assert.Equal("Continue", component.Find(".row > .col-md-4 > button").TextContent);
    }

    [Fact]
    public void StatCardRowUpdatesFromTheSuppliedCardsWithoutCustomContent()
    {
        using BunitContext context = new();
        IRenderedComponent<StatCardRow> component = context.Render<StatCardRow>(parameters => parameters
            .Add(row => row.Cards, CreateStatCards(("Successes", "3"))));

        component.Render(parameters => parameters
            .Add(row => row.Cards, CreateStatCards(("Stars Earned", "5"))));

        Assert.Single(component.FindAll(".row > .col > .card"));
        Assert.Equal("Stars Earned", component.Find(".card-body > .text-secondary").TextContent);
        Assert.EndsWith("5", component.Find(".card-body").TextContent.Trim());
    }

    [Fact]
    public void StatCardRowUpdatesTheColumnClassOfItsCards()
    {
        using BunitContext context = new();
        IRenderedComponent<StatCardRow> component = context.Render<StatCardRow>(parameters => parameters
            .Add(row => row.ColumnClass, "col-md-2")
            .Add(row => row.Cards, CreateStatCards(("Successes", "3"), ("Failures", "1"))));

        component.Render(parameters => parameters.Add(row => row.ColumnClass, "col-sm-4"));

        Assert.Equal(2, component.FindAll(".row > .col-sm-4 > .card").Count);
        Assert.Empty(component.FindAll(".col-md-2"));
    }

    [Fact]
    public void StatCardRendersItsLabel()
    {
        using BunitContext context = new();
        IRenderedComponent<StatCard> component = context.Render<StatCard>(parameters => parameters
            .Add(card => card.Label, "Successes")
            .AddChildContent("<div>3</div>"));

        Assert.Equal("Successes", component.Find(".card-body > .text-secondary").TextContent);
    }

    [Fact]
    public void StatCardAppliesSharedStylingToItsContentButNotItsLabel()
    {
        using BunitContext context = new();
        IRenderedComponent<StatCard> component = context.Render<StatCard>(parameters => parameters
            .Add(card => card.Label, "Successes")
            .AddChildContent("3"));

        Assert.Equal("3", component.Find(".card-body > .fs-5.text-light").TextContent.Trim());
        IElement label = component.Find(".card-body > .text-secondary");
        Assert.DoesNotContain("fs-5", label.ClassList);
        Assert.DoesNotContain("text-light", label.ClassList);
    }

    [Fact]
    public void StatCardPreservesCustomContentInsideItsStyledShell()
    {
        using BunitContext context = new();
        IRenderedComponent<StatCard> component = context.Render<StatCard>(parameters => parameters
            .Add(card => card.Label, "Operation")
            .AddChildContent("<div><img src='operation.svg' alt='' /><span role='status' aria-label='Current operation' data-operation-index='2'>Operation label</span></div>"));

        IElement status = component.Find(".card.bg-dark.border-secondary.h-100 > .card-body [role='status']");
        Assert.Equal("Operation label", status.TextContent);
        Assert.Equal("Current operation", status.GetAttribute("aria-label"));
        Assert.Equal("2", status.GetAttribute("data-operation-index"));
        Assert.Equal("operation.svg", component.Find(".card-body img").GetAttribute("src"));
    }

    [Fact]
    public void WarbondCardReflectsSelection()
    {
        using BunitContext context = new();
        Warbond warbond = new() { Id = "test-warbond", DisplayName = "Test Warbond" };

        IRenderedComponent<WarbondCard> component = context.Render<WarbondCard>(parameters => parameters
            .Add(card => card.Warbond, warbond)
            .Add(card => card.IsSelected, true)
            .Add(card => card.OnClick, EventCallback.Factory.Create(this, () => { })));

        var card = component.Find("[aria-label='Warbond: Test Warbond']");
        Assert.Equal("true", card.GetAttribute("aria-pressed"));
    }

    [Fact]
    public void WarbondCardInvokesClickCallback()
    {
        using BunitContext context = new();
        int clickCount = 0;
        Warbond warbond = new() { Id = "test-warbond", DisplayName = "Test Warbond" };

        IRenderedComponent<WarbondCard> component = context.Render<WarbondCard>(parameters => parameters
            .Add(card => card.Warbond, warbond)
            .Add(card => card.OnClick, EventCallback.Factory.Create(this, () => clickCount++)));

        component.Find("[aria-label='Warbond: Test Warbond']").Click();

        Assert.Equal(1, clickCount);
    }

    [Fact]
    public void ItemGridCountAndCardsUpdateFromTheSuppliedItems()
    {
        using BunitContext context = new();
        GameItem[] initialItems =
        [
            new(new GameItemDefinition { Id = "item-one", DisplayName = "Item One" }, ItemKind.Primary, null),
            new(new GameItemDefinition { Id = "item-two", DisplayName = "Item Two" }, ItemKind.Primary, null)
        ];
        IRenderedComponent<ItemGrid> component = context.Render<ItemGrid>(parameters => parameters
            .Add(grid => grid.Items, initialItems)
            .Add(grid => grid.AccessibleLabel, "Test items")
            .Add(grid => grid.UseItemColumns, true));

        IElement root = component.Find("[role='group']");
        Assert.Equal("2", root.GetAttribute("data-item-count"));
        Assert.Equal("--item-columns: 2", root.GetAttribute("style"));
        Assert.Equal(["item-one", "item-two"], component.FindAll("[data-item-id]").Select(card => card.GetAttribute("data-item-id")));

        GameItem[] updatedItems = [new(new GameItemDefinition { Id = "item-three", DisplayName = "Item Three" }, ItemKind.Primary, null)];
        component.Render(parameters => parameters.Add(grid => grid.Items, updatedItems));

        Assert.Equal("1", root.GetAttribute("data-item-count"));
        Assert.Equal("--item-columns: 1", root.GetAttribute("style"));
        Assert.Equal(["item-three"], component.FindAll("[data-item-id]").Select(card => card.GetAttribute("data-item-id")));
    }

    [Fact]
    public void ItemGridRendersAnEmptyStateAndZeroCount()
    {
        using BunitContext context = new();
        IRenderedComponent<ItemGrid> component = context.Render<ItemGrid>(parameters => parameters
            .Add(grid => grid.Items, Array.Empty<GameItem>())
            .Add(grid => grid.AccessibleLabel, "Empty test items")
            .Add(grid => grid.EmptyMessage, "No test items."));

        Assert.Equal("0", component.Find("[role='group']").GetAttribute("data-item-count"));
        Assert.Empty(component.FindAll("[data-item-id]"));
        Assert.Equal("No test items.", component.Find("[role='group']").TextContent.Trim());
    }

    [Fact]
    public void ItemGridRendersOptionalContainer()
    {
        using BunitContext context = new();
        IRenderedComponent<ItemGrid> component = context.Render<ItemGrid>(parameters => parameters
            .Add(grid => grid.Items, Array.Empty<GameItem>())
            .Add(grid => grid.AccessibleLabel, "Test items")
            .Add(grid => grid.ContainerClass, "section-card"));

        Assert.Equal("section-card", component.Find(".section-card").GetAttribute("class"));
    }

    [Fact]
    public void ItemGridRendersOptionalHeading()
    {
        using BunitContext context = new();
        IRenderedComponent<ItemGrid> component = context.Render<ItemGrid>(parameters => parameters
            .Add(grid => grid.Items, Array.Empty<GameItem>())
            .Add(grid => grid.AccessibleLabel, "Test items")
            .Add(grid => grid.HeaderContent, builder => builder.AddMarkupContent(0, "<h5 class='text-light'>Test heading</h5>")));

        Assert.Equal("Test heading", component.Find("h5").TextContent);
    }

    [Fact]
    public void ItemGridUsesProvidedAccessibleLabel()
    {
        using BunitContext context = new();
        IRenderedComponent<ItemGrid> component = context.Render<ItemGrid>(parameters => parameters
            .Add(grid => grid.Items, Array.Empty<GameItem>())
            .Add(grid => grid.AccessibleLabel, "Test items"));

        Assert.Equal("Test items", component.Find("[role='group']").GetAttribute("aria-label"));
    }

    [Fact]
    public void ToastComponentRendersServiceNotifications()
    {
        using BunitContext context = new();
        ToastService toasts = new();
        context.Services.AddSingleton(toasts);
        IRenderedComponent<Toast> component = context.Render<Toast>();

        toasts.Show("Save completed.");

        Assert.Contains("Save completed.", component.Markup);
    }

    [Theory]
    [InlineData(true, false, false, "Reward", "text-success", "Mission reward")]
    [InlineData(false, true, false, "Punishment", "text-danger", "Mission punishment")]
    [InlineData(false, false, true, "Banned", "text-primary", "Mission banned items")]
    [InlineData(false, false, false, "No Item Outcome", "text-warning", null)]
    [InlineData(true, true, true, "Reward", "text-success", "Mission reward")]
    [InlineData(false, true, true, "Punishment", "text-danger", "Mission punishment")]
    public async Task MissionHistoryTableRendersTheMatchingOutcomeAndAffectedItems(
        bool hasReward,
        bool hasPunishment,
        bool hasBannedItems,
        string expectedOutcome,
        string expectedCssClass,
        string? expectedAccessibleLabel)
    {
        using CatalogTestContext catalogContext = await CatalogTestContext.CreateAsync();
        using BunitContext context = new();
        context.Services.AddSingleton(catalogContext.Catalog);
        GameItem[] items = catalogContext.Catalog.GetAllItems().Take(4).ToArray();
        MissionProgressEntry entry = new(
            operationIndex: 1,
            missionNumber: 1,
            succeeded: true,
            stars: 1,
            rewardItemId: hasReward ? items[0].Id : null,
            punishmentItemId: hasPunishment ? items[1].Id : null,
            bannedItemIds: hasBannedItems ? [items[2].Id, items[3].Id] : []);

        IRenderedComponent<MissionHistoryTable> component = context.Render<MissionHistoryTable>(parameters => parameters
            .Add(table => table.Entries, [entry]));

        IElement outcomeCell = component.FindAll("tbody tr td")[4];
        Assert.Equal(expectedOutcome, outcomeCell.TextContent);
        Assert.Contains(expectedCssClass, outcomeCell.ClassList);

        string[] expectedItemIds = hasReward ? [items[0].Id]
            : hasPunishment ? [items[1].Id]
            : hasBannedItems ? [items[2].Id, items[3].Id]
            : [];
        Assert.Equal(expectedItemIds, component.FindAll("[data-item-id]").Select(card => card.GetAttribute("data-item-id")));
        if (expectedAccessibleLabel is not null)
        {
            Assert.Equal(expectedAccessibleLabel, component.Find("[role='group']").GetAttribute("aria-label"));
        }
        else
        {
            Assert.Equal("No Items Affected", component.FindAll("tbody tr td")[5].TextContent);
            Assert.Empty(component.FindComponents<ItemGrid>());
        }
    }

    [Fact]
    public void ToastComponentDismissButtonRemovesServiceNotification()
    {
        using BunitContext context = new();
        ToastService toasts = new();
        context.Services.AddSingleton(toasts);
        IRenderedComponent<Toast> component = context.Render<Toast>();
        toasts.Show("Save completed.", autoCloseMilliseconds: null);

        component.Find("button[aria-label='Dismiss notification']").Click();

        Assert.Empty(toasts.Messages);
        Assert.DoesNotContain("Save completed.", component.Markup);
    }
}
