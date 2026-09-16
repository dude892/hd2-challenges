namespace Hd2Challenges.Models;

public sealed class PenitentStarterItems
{
    public List<GameItem> Stratagems { get; set; } = [];
    public List<GameItem> Primaries { get; set; } = [];
    public List<GameItem> Secondaries { get; set; } = [];
    public List<GameItem> Throwables { get; set; } = [];
    public List<GameItem> ArmorPassives { get; set; } = [];
    public List<GameItem> Boosters { get; set; } = [];

    public IEnumerable<GameItem> AllItems => Stratagems.Concat(Primaries).Concat(Secondaries).Concat(Throwables).Concat(ArmorPassives).Concat(Boosters);

    public HashSet<string> AllInternalNames => AllItems.Select(item => item.InternalName).ToHashSet();
}
