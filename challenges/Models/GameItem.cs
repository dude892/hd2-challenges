namespace Hd2Challenges.Models;

public enum ItemKind
{
    Stratagem,
    Primary,
    Secondary,
    Throwable,
    Passive,
    Booster
}

/// <summary>An immutable catalog item with resolved kind and warbond references.</summary>
public sealed class GameItem : IEquatable<GameItem>
{
    public GameItem(GameItemDefinition definition, ItemKind kind, Warbond? warbond)
    {
        ArgumentNullException.ThrowIfNull(definition);
        if (!Enum.IsDefined(kind))
        {
            throw new ArgumentOutOfRangeException(nameof(kind), kind, null);
        }

        DisplayName = definition.DisplayName;
        Tags = Array.AsReadOnly(definition.Tags.ToArray());
        Id = definition.Id;
        ImageURL = definition.ImageURL;
        Svg = definition.Svg;
        Antitank = definition.Antitank;
        Kind = kind;
        Warbond = warbond;
    }

    public string DisplayName { get; }
    public IReadOnlyList<string> Tags { get; }
    public string Id { get; }
    public string ImageURL { get; }
    public string Svg { get; }
    public bool Antitank { get; }
    public ItemKind Kind { get; }
    public Warbond? Warbond { get; }

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

    public bool Equals(GameItem? other) => other is GameItem gi && string.Equals(Id, gi.Id, StringComparison.OrdinalIgnoreCase);

    public override bool Equals(object? obj) => Equals(obj as GameItem);

    public override int GetHashCode() => StringComparer.OrdinalIgnoreCase.GetHashCode(Id);
}
