namespace Hd2Challenges.Models;

public sealed class RedemptionDifficulty(RedemptionDifficultyDefinition definition, Func<IEnumerable<string>, ItemSet> resolveItemSet, Func<int, Operation> resolveOperation)
{
    public string Id { get; } = definition.Id;
    public string DisplayName { get; } = definition.DisplayName;
    public Operation StartOperation { get; } = resolveOperation(definition.StartOperationIndex);
    public int ScoreModifier { get; } = definition.ScoreModifier;
    public bool IsSuper { get; } = definition.IsSuper;
    public ItemSet LoadoutOverrides { get; } = resolveItemSet(definition.LoadoutOverrides?.GetItemNames() ?? []);
    public ItemSet LoadoutAdditions { get; } = resolveItemSet(definition.LoadoutAdditions?.GetItemNames() ?? []);
}