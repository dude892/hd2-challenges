namespace Hd2Challenges.Models;

public sealed class PenitentSpecialist
{
    public string DisplayName { get; set; } = string.Empty;
    public List<string> StarterItems { get; set; } = [];
    public List<string> Stratagems { get; set; } = [];
    public List<string> Primaries { get; set; } = [];
    public List<string> Boosters { get; set; } = [];
    public List<string> Secondaries { get; set; } = [];
    public List<string> Throwables { get; set; } = [];
    public List<string> ArmorPassives { get; set; } = [];
    public List<string> Traits { get; set; } = [];
    public string ImageURL { get; set; } = string.Empty;
    public List<string> Warbonds { get; set; } = [];
}
