using System.Text.Json;
using Hd2Challenges.Models;

namespace Hd2Challenges.Tests;

public sealed class GameItemTests
{
    [Fact]
    public void RuntimeItemSnapshotsDefinitionAndRetainsResolvedTypes()
    {
        GameItemDefinition definition = new()
        {
            Id = "item",
            DisplayName = "Item",
            Tags = ["AssaultRifle"],
            ImageURL = "item.webp",
            Svg = "item.svg",
            Antitank = true,
            WarbondId = "warbond"
        };
        Warbond warbond = new() { Id = "warbond", DisplayName = "Warbond", AlwaysEnabled = true };
        GameItem item = new(definition, ItemKind.Primary, warbond);
        ItemSet items = new([item]);

        definition.Id = "changed";
        definition.DisplayName = "Changed";
        definition.Tags.Add("Changed");
        definition.Tags = [];
        definition.ImageURL = "changed.webp";
        definition.Svg = "changed.svg";
        definition.Antitank = false;
        definition.WarbondId = "changed";

        Assert.Equal("item", item.Id);
        Assert.Equal("Item", item.DisplayName);
        Assert.Equal(["AssaultRifle"], item.Tags);
        Assert.Equal("item.webp", item.ImageURL);
        Assert.Equal("item.svg", item.Svg);
        Assert.True(item.Antitank);
        Assert.Equal(ItemKind.Primary, item.Kind);
        Assert.Same(warbond, item.Warbond);
        Assert.Equal("svgs/primary/item.svg", item.ImagePath);
        Assert.Contains(item, items.Primaries);
    }

    [Fact]
    public void TagsCannotBeModifiedThroughCollectionInterfaces()
    {
        GameItem item = new(new GameItemDefinition { Tags = ["Original"] }, ItemKind.Booster, null);
        IList<string> tags = Assert.IsType<IList<string>>(item.Tags, exactMatch: false);

        Assert.True(tags.IsReadOnly);
        Assert.Throws<NotSupportedException>(() => tags.Add("Changed"));
        Assert.Throws<NotSupportedException>(() => tags[0] = "Changed");
        Assert.Throws<NotSupportedException>(() => tags.Clear());
        Assert.Equal(["Original"], item.Tags);
    }

    [Fact]
    public void RuntimeItemExposesNoPropertySetters()
    {
        Assert.All(typeof(GameItem).GetProperties(), property => Assert.Null(property.SetMethod));
    }

    [Fact]
    public void DefinitionPreservesTheCatalogJsonContract()
    {
        const string json = """
            {"id":"item","displayName":"Item","tags":["Test"],"warbond":"testWarbond",
             "imageURL":"item.webp","svg":"item.svg","antitank":true}
            """;
        JsonSerializerOptions options = new(JsonSerializerDefaults.Web);
        GameItemDefinition definition = Assert.IsType<GameItemDefinition>(
            JsonSerializer.Deserialize<GameItemDefinition>(json, options));

        Assert.Equal("item", definition.Id);
        Assert.Equal("Item", definition.DisplayName);
        Assert.Equal(["Test"], definition.Tags);
        Assert.Equal("testWarbond", definition.WarbondId);
        Assert.Equal("item.webp", definition.ImageURL);
        Assert.Equal("item.svg", definition.Svg);
        Assert.True(definition.Antitank);

        using JsonDocument serialized = JsonDocument.Parse(JsonSerializer.Serialize(definition, options));
        Assert.Equal("testWarbond", serialized.RootElement.GetProperty("warbond").GetString());
        Assert.False(serialized.RootElement.TryGetProperty("warbondId", out _));
        Assert.False(serialized.RootElement.TryGetProperty("kind", out _));
    }

    [Theory]
    [InlineData(ItemKind.Primary, "images/equipment/item.webp")]
    [InlineData(ItemKind.Secondary, "images/equipment/item.webp")]
    [InlineData(ItemKind.Throwable, "images/equipment/item.webp")]
    [InlineData(ItemKind.Stratagem, "images/stratagem/item.webp")]
    [InlineData(ItemKind.Passive, "images/passive/item.webp")]
    [InlineData(ItemKind.Booster, "images/booster/item.webp")]
    public void ImagePathsUseTheConstructorSuppliedKind(ItemKind kind, string expectedPath)
    {
        GameItem item = new(new GameItemDefinition { ImageURL = "item.webp" }, kind, null);

        Assert.Equal(expectedPath, item.ImagePath);
        Assert.Null(item.Warbond);
    }

    [Fact]
    public void DefaultsHaveNoImageAndInvalidConstructionIsRejected()
    {
        GameItem item = new(new GameItemDefinition(), ItemKind.Primary, null);

        Assert.Empty(item.Tags);
        Assert.Null(item.ImagePath);
        Assert.Throws<ArgumentNullException>(() => new GameItem(null!, ItemKind.Primary, null));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new GameItem(new GameItemDefinition(), (ItemKind)int.MaxValue, null));
    }

    [Theory]
    [InlineData("stratagems.json", ItemKind.Stratagem)]
    [InlineData("primaries.json", ItemKind.Primary)]
    [InlineData("secondaries.json", ItemKind.Secondary)]
    [InlineData("throwables.json", ItemKind.Throwable)]
    [InlineData("passives.json", ItemKind.Passive)]
    [InlineData("boosters.json", ItemKind.Booster)]
    public async Task CatalogResolvesDefinitionsAndPreservesFileOrder(string fileName, ItemKind kind)
    {
        using CatalogTestContext context = await CatalogTestContext.CreateAsync();
        string json = await File.ReadAllTextAsync(
            Path.Combine(AppContext.BaseDirectory, "data", fileName), TestContext.Current.CancellationToken);
        GameItemDefinition[] definitions = Assert.IsType<GameItemDefinition[]>(
            JsonSerializer.Deserialize<GameItemDefinition[]>(json, new JsonSerializerOptions(JsonSerializerDefaults.Web)));
        IReadOnlyList<GameItem> orderedItems = context.Catalog.GetItemsInCatalogOrder(
            context.Catalog.GetAllItems().GetCategory(kind).Reverse());
        Warbond[] warbonds = [.. context.Catalog.GetAllWarbonds()];

        Assert.Equal(definitions.Select(definition => definition.Id), orderedItems.Select(item => item.Id));
        foreach ((GameItemDefinition definition, GameItem item) in definitions.Zip(orderedItems))
        {
            Assert.Equal(kind, item.Kind);
            Assert.Equal(definition.DisplayName, item.DisplayName);
            Assert.Equal(definition.Tags, item.Tags);
            Assert.Equal(definition.ImageURL, item.ImageURL);
            Assert.Equal(definition.Svg, item.Svg);
            Assert.Equal(definition.Antitank, item.Antitank);
            Warbond? expectedWarbond = warbonds.FirstOrDefault(warbond =>
                string.Equals(warbond.Id, definition.WarbondId, StringComparison.OrdinalIgnoreCase));
            Assert.Same(expectedWarbond, item.Warbond);
        }
    }
}
