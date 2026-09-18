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

public sealed class GameItem : IEquatable<GameItem>
{
    public string DisplayName { get; init; } = string.Empty;
    public List<string> Tags { get; init; } = [];
    public string Id { get; init; } = string.Empty;
    public string ImageURL { get; init; } = string.Empty;
    public bool Antitank { get; init; } = false;

    [JsonIgnore]
    public ItemKind Kind { get; private set; }

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
    public Warbond? Warbond { get; private set; }

    [JsonInclude]
    [JsonPropertyName("warbond")]
    private string _warbond { get; set; } = "none";

    private void ResolveWarbond(IReadOnlyList<Warbond> warbonds)
    {
        Warbond = warbonds.FirstOrDefault(w => string.Equals(w.Id, _warbond, StringComparison.OrdinalIgnoreCase));
    }

    public void PostInitSetup(ItemKind kind, IReadOnlyList<Warbond> warbonds)
    {
        Kind = kind;
        ResolveWarbond(warbonds);
    }

    public bool Equals(GameItem? other) => other is GameItem gi && string.Equals(Id, gi.Id, StringComparison.OrdinalIgnoreCase);

    public override bool Equals(object? obj) => Equals(obj as GameItem);

    public override int GetHashCode() => StringComparer.OrdinalIgnoreCase.GetHashCode(Id);
}
