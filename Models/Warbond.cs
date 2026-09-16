namespace Hd2Challenges.Models;

public sealed record Warbond(
    string WarbondCode,
    string DisplayName,
    bool AlwaysEnabled = false
);

