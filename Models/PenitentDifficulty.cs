namespace Hd2Challenges.Models;

public sealed class PenitentDifficulty(PenitentDifficultyDefinition definition, Func<IEnumerable<string>, ItemSet> resolveItemSet, Func<string, Operation> resolveOperation)
{
    public string Id { get; } = definition.Id;
    public string DisplayName { get; } = definition.DisplayName;
    public Operation StartOperation { get; } = resolveOperation(definition.StartOperation);
    public int ScoreModifier { get; } = definition.ScoreModifier;
    public bool IsSuper { get; } = definition.IsSuper;
    public ItemSet LoadoutOverrides { get; } = resolveItemSet(GetItemNames(definition.LoadoutOverrides));
    public ItemSet LoadoutAdditions { get; } = resolveItemSet(GetItemNames(definition.LoadoutAdditions));

    private static IEnumerable<string> GetItemNames(PenitentStarterLoadoutDefinition? definition) =>
        definition is null
            ? []
            : definition.Stratagems
                .Concat(definition.Primaries)
                .Concat(definition.Secondaries)
                .Concat(definition.Throwables)
                .Concat(definition.ArmorPassives)
                .Concat(definition.Boosters);
}