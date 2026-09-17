namespace Hd2Challenges.Models;

public sealed class PenitentStarterLoadoutDefinition
{
    public List<string> Stratagems { get; set; } = [];
    public List<string> Primaries { get; set; } = [];
    public List<string> Secondaries { get; set; } = [];
    public List<string> Throwables { get; set; } = [];
    public List<string> ArmorPassives { get; set; } = [];
    public List<string> Boosters { get; set; } = [];
}
