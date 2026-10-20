using Hd2Challenges.Models;
using Microsoft.Playwright;
using Microsoft.Playwright.Xunit.v3;
using Xunit;

namespace Hd2Challenges.EndToEnd.Tests;

[Collection(WarpathWebAppCollection.Name)]
public sealed class ItemCatalogPageTests(WarpathWebAppFixture webApp) : PageTest
{
    [Fact]
    public async Task CatalogPageLoadsEveryItemWithAnImage()
    {
        await OpenCatalogPageAsync();

        ItemSet expectedItems = webApp.Catalog.GetAllItems();
        Assert.All(expectedItems, item => Assert.False(string.IsNullOrWhiteSpace(item.ImagePath),
            $"Catalog item '{item.Id}' does not have an image path."));

        ILocator cards = Page.Locator("#item-catalog-accordion [data-item-id]");
        await Expect(cards).ToHaveCountAsync(expectedItems.Count);

        ILocator[] renderedCards = [.. await cards.AllAsync()];
        string?[] renderedItemIds = await Task.WhenAll(renderedCards.Select(card => card.GetAttributeAsync("data-item-id")));
        Assert.Equal(
            expectedItems.Select(item => item.Id).OrderBy(id => id, StringComparer.OrdinalIgnoreCase),
            renderedItemIds.Select(id => id ?? throw new InvalidOperationException("An item card is missing its data-item-id."))
                .OrderBy(id => id, StringComparer.OrdinalIgnoreCase),
            StringComparer.OrdinalIgnoreCase);

        ILocator images = cards.Locator("img");
        await Expect(images).ToHaveCountAsync(expectedItems.Count);
        await Page.WaitForFunctionAsync(
            "() => [...document.querySelectorAll('#item-catalog-accordion img')].every(image => image.complete)");

        string[] brokenImageSources = await images.EvaluateAllAsync<string[]>(
            "images => images.filter(image => image.naturalWidth === 0).map(image => image.src)");
        Assert.Empty(brokenImageSources);
    }

    [Fact]
    public async Task CatalogItemsAppearInTheirCorrectCategories()
    {
        await OpenCatalogPageAsync();

        (string Label, IEnumerable<GameItem> Items)[] expectedGroups = [.. webApp.Catalog.GetAllItems().CategoryGroups];
        ILocator[] renderedGroups = [.. await Page.Locator("#item-catalog-accordion .accordion-item").AllAsync()];
        Assert.Equal(expectedGroups.Length, renderedGroups.Length);
        for (int index = 0; index < expectedGroups.Length; index++)
        {
            (string label, IEnumerable<GameItem> items) = expectedGroups[index];
            ILocator group = renderedGroups[index];
            await Expect(group.Locator(".accordion-button")).ToContainTextAsync(label);

            ILocator[] categoryCards = [.. await group.Locator("[data-item-id]").AllAsync()];
            string?[] categoryItemIds = await Task.WhenAll(categoryCards.Select(card => card.GetAttributeAsync("data-item-id")));
            Assert.Equal(
                items.Select(item => item.Id).OrderBy(id => id, StringComparer.OrdinalIgnoreCase),
                categoryItemIds.Select(id => id ?? throw new InvalidOperationException("An item card is missing its data-item-id."))
                    .OrderBy(id => id, StringComparer.OrdinalIgnoreCase),
                StringComparer.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public async Task CatalogItemsShowTheirCatalogImages()
    {
        await OpenCatalogPageAsync();

        ItemSet expectedItems = webApp.Catalog.GetAllItems();
        Dictionary<string, GameItem> expectedItemsById = expectedItems.ToDictionary(item => item.Id, StringComparer.OrdinalIgnoreCase);
        ILocator cards = Page.Locator("#item-catalog-accordion [data-item-id]");
        await Expect(cards).ToHaveCountAsync(expectedItems.Count);
        ILocator[] renderedCards = [.. await cards.AllAsync()];
        foreach (ILocator card in renderedCards)
        {
            string itemId = await card.GetAttributeAsync("data-item-id")
                ?? throw new InvalidOperationException("An item card is missing its data-item-id.");
            Assert.Contains(itemId, expectedItemsById.Keys, StringComparer.OrdinalIgnoreCase);

            GameItem expectedItem = expectedItemsById[itemId];
            ILocator image = card.Locator("img");
            Assert.Equal(expectedItem.ImagePath, await image.GetAttributeAsync("src"));
            Assert.Equal(expectedItem.DisplayName, await image.GetAttributeAsync("alt"));
        }
    }

    [Fact]
    public async Task CatalogCategoryAccordionsCanBeExpandedAndCollapsed()
    {
        await OpenCatalogPageAsync();

        IEnumerable<(string Label, IEnumerable<GameItem> Items)> expectedGroups = webApp.Catalog.GetAllItems().CategoryGroups;
        ILocator renderedGroups = Page.Locator("#item-catalog-accordion .accordion-item");
        await Expect(renderedGroups).ToHaveCountAsync(expectedGroups.Count());
        ILocator[] groups = [.. await renderedGroups.AllAsync()];
        foreach (ILocator group in groups)
        {
            ILocator toggle = group.Locator(".accordion-button");
            ILocator panel = group.Locator(".accordion-collapse");

            string? initialExpanded = await toggle.GetAttributeAsync("aria-expanded");
            Assert.True(initialExpanded is "true" or "false", "Accordion toggle should expose its expanded state.");
            if (initialExpanded == "true")
            {
                await toggle.ClickAsync();
                await Expect(toggle).ToHaveAttributeAsync("aria-expanded", "false");
                await Expect(panel).ToHaveClassAsync("accordion-collapse collapse");
                await Expect(panel).ToBeHiddenAsync();
            }

            await toggle.ClickAsync();
            await Expect(toggle).ToHaveAttributeAsync("aria-expanded", "true");
            await Expect(panel).ToHaveClassAsync("accordion-collapse collapse show");
            await Expect(panel).ToBeVisibleAsync();

            await toggle.ClickAsync();
            await Expect(toggle).ToHaveAttributeAsync("aria-expanded", "false");
            await Expect(panel).ToHaveClassAsync("accordion-collapse collapse");
            await Expect(panel).ToBeHiddenAsync();
        }
    }

    private async Task OpenCatalogPageAsync()
    {
        await Page.GotoAsync($"{webApp.BaseUrl}/item-catalog-test");
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Item Catalog Test", Exact = true })).ToBeVisibleAsync();
    }
}