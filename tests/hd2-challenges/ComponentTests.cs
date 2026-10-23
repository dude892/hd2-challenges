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
            new() { Id = "item-one", DisplayName = "Item One" },
            new() { Id = "item-two", DisplayName = "Item Two" }
        ];
        IRenderedComponent<ItemGrid> component = context.Render<ItemGrid>(parameters => parameters
            .Add(grid => grid.Items, initialItems)
            .Add(grid => grid.AccessibleLabel, "Test items")
            .Add(grid => grid.UseItemColumns, true));

        IElement root = component.Find("[role='group']");
        Assert.Equal("2", root.GetAttribute("data-item-count"));
        Assert.Equal("--item-columns: 2", root.GetAttribute("style"));
        Assert.Equal(["item-one", "item-two"], component.FindAll("[data-item-id]").Select(card => card.GetAttribute("data-item-id")));

        GameItem[] updatedItems = [new() { Id = "item-three", DisplayName = "Item Three" }];
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
