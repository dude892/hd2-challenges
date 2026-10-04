using System.Text.Json.Serialization;

namespace Hd2Challenges.Models;

public enum ItemKind
{
    Stratagem,
    Primary,
    Secondary,
    Throwable,
    Booster,
    Passive
}

public sealed class GameItem : IEquatable<GameItem>
{
    public string DisplayName { get; init; } = string.Empty;
    public List<string> Tags { get; init; } = [];
    public string Id { get; init; } = string.Empty;
    public string ImageURL { get; init; } = string.Empty;
    public string Svg { get; init; } = string.Empty;
    public bool Antitank { get; init; } = false;

    [JsonIgnore]
    public ItemKind Kind { get; private set; }

    [JsonIgnore]
    public string? ImagePath
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(Svg))
            {
                return (string?)$"svgs/{Kind.ToString().ToLowerInvariant()}/{Svg}";
            }
            else if (!string.IsNullOrWhiteSpace(ImageURL))
            {
                string subfolder = Kind.ToString().ToLowerInvariant();

                if (Kind is ItemKind.Primary or ItemKind.Secondary or ItemKind.Throwable)
                {
                    subfolder = "equipment";
                }

                return $"images/{subfolder}/{ImageURL}";
            }
            else
            {
                return null;
            }
        }
    }

    [JsonIgnore]
    public Warbond? Warbond { get; private set; }

    [JsonInclude]
    [JsonPropertyName("warbond")]
    private string _warbond { get; set; } = "none";

    public void PostInitSetup(ItemKind kind, IReadOnlyList<Warbond> warbonds)
    {
        Kind = kind;
        Warbond = warbonds.FirstOrDefault(w => string.Equals(w.Id, _warbond, StringComparison.OrdinalIgnoreCase));
    }

    public bool Equals(GameItem? other) => other is GameItem gi && string.Equals(Id, gi.Id, StringComparison.OrdinalIgnoreCase);

    public override bool Equals(object? obj) => Equals(obj as GameItem);

    public override int GetHashCode() => StringComparer.OrdinalIgnoreCase.GetHashCode(Id);
}
