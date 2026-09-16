using System.Text.Json.Serialization;

namespace Hd2Challenges.Models;

public enum ItemKind
{
    Stratagem,
    Primary,
    Secondary,
    Throwable,
    Booster,
    ArmorPassive
}

public sealed class GameItem
{
    public string DisplayName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = [];
    public string WarbondCode { get; set; } = "none";
    public string InternalName { get; set; } = string.Empty;
    public string ImageURL { get; set; } = string.Empty;
    public bool Antitank { get; set; }

    [JsonIgnore]
    public ItemKind Kind { get; set; }

    [JsonIgnore]
    public string ImageDirectory =>
        Kind switch
        {
            ItemKind.Stratagem => "svgs",
            ItemKind.ArmorPassive => "armorpassives",
            _ => "equipment"
        };

    [JsonIgnore]
    public string KindLabel =>
        Kind switch
        {
            ItemKind.Stratagem => "Stratagem",
            ItemKind.Primary => "Primary",
            ItemKind.Secondary => "Secondary",
            ItemKind.Throwable => "Throwable",
            ItemKind.Booster => "Booster",
            ItemKind.ArmorPassive => "Armor Passive",
            _ => "Item"
        };
}
