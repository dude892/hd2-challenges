namespace Hd2Challenges.Models;

public sealed record Warbond(
    string Id,
    string DisplayName,
    bool AlwaysEnabled = false
);
