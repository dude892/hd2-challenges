using System.Text.Json.Serialization;
using Hd2Challenges.Services;

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
    public List<string> Tags { get; set; } = [];
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
    public string KindLabel => Kind.CompareTo(ItemKind.ArmorPassive) == 0 ? "Armor Passive" : Kind.ToString();

    [JsonIgnore]
    public Warbond? Warbond { get; set; }

    [JsonInclude]
    [JsonPropertyName("warbond")]
    private string _warbond { get; set; } = "none";

    public void ResolveWarbond(IReadOnlyList<Warbond> warbonds)
    {
        Warbond = warbonds.FirstOrDefault(w => string.Equals(w.InternalName, _warbond, StringComparison.OrdinalIgnoreCase));
    }
}
