namespace Hd2Challenges.Models;

public sealed class Warbond : IEquatable<Warbond>
{
    public string Id { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public bool AlwaysEnabled { get; init; } = false;

    public bool Equals(Warbond? other) =>
        other is not null && string.Equals(Id, other.Id, StringComparison.OrdinalIgnoreCase);

    public override bool Equals(object? obj) => Equals(obj as Warbond);

    public override int GetHashCode() => StringComparer.OrdinalIgnoreCase.GetHashCode(Id);
}
