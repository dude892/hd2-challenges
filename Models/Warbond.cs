namespace Hd2Challenges.Models;

public sealed record Warbond(
    string Code,
    string Name,
    bool AlwaysEnabled = false
);

