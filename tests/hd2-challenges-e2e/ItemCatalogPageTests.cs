using Hd2Challenges.Models;
using Microsoft.Playwright;
using Microsoft.Playwright.Xunit.v3;
using Xunit;

namespace Hd2Challenges.EndToEnd.Tests;

[Collection(WarpathWebAppCollection.Name)]
public sealed class ItemCatalogPageTests(WarpathWebAppFixture webApp) : PageTest
{

    private async Task OpenCatalogPageAsync()
    {
        await Page.GotoAsync($"{webApp.BaseUrl}/item-catalog-test");
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Item Catalog Test", Exact = true })).ToBeVisibleAsync();
        await Expect(Page.Locator("#item-catalog-accordion .accordion-item"))
            .ToHaveCountAsync(webApp.Catalog.GetAllItems().Categories.Count());
    }
    
    [Fact]
    public async Task CatalogPageLoadsEveryItem()
    {
        await OpenCatalogPageAsync();

        ItemSet expectedItems = webApp.Catalog.GetAllItems();

        ILocator cards = Page.Locator("#item-catalog-accordion [data-item-id]");
        await Expect(cards).ToHaveCountAsync(expectedItems.Count);

        ILocator[] renderedCards = [.. await cards.AllAsync()];
        string?[] renderedItemIds = await Task.WhenAll(renderedCards.Select(card => card.GetAttributeAsync("data-item-id")));
        Assert.Equal(
            expectedItems.Select(item => item.Id).OrderBy(id => id, StringComparer.OrdinalIgnoreCase),
            renderedItemIds.Select(id => id ?? throw new InvalidOperationException("An item card is missing its data-item-id."))
                .OrderBy(id => id, StringComparer.OrdinalIgnoreCase),
            StringComparer.OrdinalIgnoreCase);
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
    public async Task CatalogContainsEveryCategoryFromTheCatalog()
    {
        await OpenCatalogPageAsync();

        ItemCategory[] expectedGroups = [.. webApp.Catalog.GetAllItems().Categories];
        ILocator[] renderedGroups = [.. await Page.Locator("#item-catalog-accordion .accordion-item").AllAsync()];

        Assert.Equal(expectedGroups.Length, renderedGroups.Length);
        for (int index = 0; index < expectedGroups.Length; index++)
        {
            await Expect(renderedGroups[index].Locator(".accordion-button")).ToContainTextAsync(expectedGroups[index].Label);
        }
    }

    [Theory]
    [InlineData(ItemKind.Stratagem)]
    [InlineData(ItemKind.Primary)]
    [InlineData(ItemKind.Secondary)]
    [InlineData(ItemKind.Throwable)]
    [InlineData(ItemKind.Passive)]
    [InlineData(ItemKind.Booster)]
    public async Task CatalogItemsAppearInTheirCorrectCategories(ItemKind categoryKind)
    {
        await OpenCatalogPageAsync();

        ItemSet catalogItems = webApp.Catalog.GetAllItems();
        ItemCategory category = catalogItems.GetCategory(categoryKind);
        ILocator group = Page.Locator("#item-catalog-accordion .accordion-item")
            .Filter(new() { Has = Page.GetByRole(AriaRole.Button, new() { Name = category.Label }) });
        await Expect(group).ToHaveCountAsync(1);

        ILocator[] categoryCards = [.. await group.Locator("[data-item-id]").AllAsync()];
        string?[] categoryItemIds = await Task.WhenAll(categoryCards.Select(card => card.GetAttributeAsync("data-item-id")));
        Assert.Equal(
            category.Select(item => item.Id).OrderBy(id => id, StringComparer.OrdinalIgnoreCase),
            categoryItemIds.Select(id => id ?? throw new InvalidOperationException("An item card is missing its data-item-id."))
                .OrderBy(id => id, StringComparer.OrdinalIgnoreCase),
            StringComparer.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(ItemKind.Stratagem)]
    [InlineData(ItemKind.Primary)]
    [InlineData(ItemKind.Secondary)]
    [InlineData(ItemKind.Throwable)]
    [InlineData(ItemKind.Passive)]
    [InlineData(ItemKind.Booster)]
    public async Task CatalogCategoryAccordionsCanBeExpandedAndCollapsed(ItemKind categoryKind)
    {
        await OpenCatalogPageAsync();

        ItemSet catalogItems = webApp.Catalog.GetAllItems();
        ItemCategory category = catalogItems.GetCategory(categoryKind);
        ILocator group = Page.Locator("#item-catalog-accordion .accordion-item")
            .Filter(new() { Has = Page.GetByRole(AriaRole.Button, new() { Name = category.Label }) });
        await Expect(group).ToHaveCountAsync(1);
        ILocator toggle = group.Locator(".accordion-button");
        ILocator panel = group.Locator(".accordion-collapse");
        string collapseId = $"item-category-{category.Label.Replace(" ", "-").ToLowerInvariant()}";
        await Expect(toggle).ToHaveAttributeAsync("aria-controls", collapseId);
        await Expect(toggle).ToHaveAttributeAsync("data-bs-target", $"#{collapseId}");
        await Expect(panel).ToHaveAttributeAsync("id", collapseId);

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