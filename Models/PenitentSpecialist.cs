namespace Hd2Challenges.Models;

public sealed class PenitentSpecialist
{
    public string Id { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public List<string> StarterItems { get; set; } = [];
    public PenitentStarterLoadout? StarterLoadout { get; set; }
    public List<string> Traits { get; set; } = [];
    public string ImageURL { get; set; } = string.Empty;
    public List<string> Warbonds { get; set; } = [];
}
