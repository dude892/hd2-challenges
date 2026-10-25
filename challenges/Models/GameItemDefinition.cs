using System.Text.Json.Serialization;

namespace Hd2Challenges.Models;

/// <summary>The catalog JSON representation; warbond IDs are resolved before constructing runtime items.</summary>
public sealed class GameItemDefinition
{
    public string DisplayName { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = [];
    public string Id { get; set; } = string.Empty;
    public string ImageURL { get; set; } = string.Empty;
    public string Svg { get; set; } = string.Empty;
    public bool Antitank { get; set; }

    [JsonPropertyName("warbond")]
    public string WarbondId { get; set; } = "none";
}
