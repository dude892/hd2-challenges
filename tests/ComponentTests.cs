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
}
