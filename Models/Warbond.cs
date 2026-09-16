namespace Hd2Challenges.Models;

public sealed record Warbond(
    string InternalName,
    string DisplayName,
    bool AlwaysEnabled = false
);
