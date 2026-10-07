namespace Hd2Challenges.Models;

public sealed class RedemptionDifficulty(RedemptionDifficultyDefinition definition, Func<IEnumerable<string>, ItemSet> resolveItemSet, Func<int, Operation> resolveOperation)
{
    public string Id { get; } = definition.Id;
    public string DisplayName { get; } = definition.DisplayName;
    public Operation StartOperation { get; } = resolveOperation(definition.StartOperationIndex);
    public int ScoreModifier { get; } = definition.ScoreModifier;
    public bool IsSuper { get; } = definition.IsSuper;
    public ItemSet LoadoutOverrides { get; } = resolveItemSet(GetItemNames(definition.LoadoutOverrides));
    public ItemSet LoadoutAdditions { get; } = resolveItemSet(GetItemNames(definition.LoadoutAdditions));

    private static IEnumerable<string> GetItemNames(RedemptionStarterLoadoutDefinition? definition) =>
        definition is null
            ? []
            : definition.Stratagems
                .Concat(definition.Primaries)
                .Concat(definition.Secondaries)
                .Concat(definition.Throwables)
                .Concat(definition.ArmorPassives)
                .Concat(definition.Boosters);
}