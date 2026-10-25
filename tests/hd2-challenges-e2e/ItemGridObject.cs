using Hd2Challenges.Models;
using Microsoft.Playwright;
using Xunit;

namespace Hd2Challenges.EndToEnd.Tests;

internal sealed class ItemGridObject(ILocator root)
{
    public ILocator Root { get; } = root;
    public ILocator Cards => Root.Locator("[data-item-id]");
    public ILocator SelectableChoices => Root.Locator("button[data-item-id]");

    public async Task<string[]> GetItemIdsAsync()
    {
        ILocator[] cards = [.. await Cards.AllAsync()];
        string?[] ids = await Task.WhenAll(cards.Select(card => card.GetAttributeAsync("data-item-id")));
        return ids.Select(id => id ?? throw new InvalidOperationException("An item card is missing its data-item-id.")).ToArray();
    }

    public async Task AssertCountAsync(int expectedCount)
    {
        Assert.Equal(expectedCount, await Cards.CountAsync());
        Assert.Equal(expectedCount.ToString(), await Root.GetAttributeAsync("data-item-count"));
    }

    public async Task AssertContainsItemsAsync(params string[] expectedItemIds)
    {
        string[] actualItemIds = await GetItemIdsAsync();
        foreach (string expectedItemId in expectedItemIds)
        {
            Assert.True(actualItemIds.Contains(expectedItemId, StringComparer.OrdinalIgnoreCase),
                $"Expected item '{expectedItemId}' in grid. Actual item IDs: {string.Join(", ", actualItemIds)}");
        }
    }

    public async Task AssertMatchesItemsAsync(ItemSet expectedItems)
    {
        string[] actualItemIds = await GetItemIdsAsync();
        Assert.Equal(
            expectedItems.Select(item => item.Id).OrderBy(id => id, StringComparer.OrdinalIgnoreCase),
            actualItemIds.OrderBy(id => id, StringComparer.OrdinalIgnoreCase),
            StringComparer.OrdinalIgnoreCase);
        await AssertCountAsync(expectedItems.Count);
    }

    public async Task AssertDoesNotContainItemAsync(string itemId)
    {
        string[] actualItemIds = await GetItemIdsAsync();
        Assert.False(actualItemIds.Contains(itemId, StringComparer.OrdinalIgnoreCase),
            $"Did not expect item '{itemId}' in grid.");
    }
}