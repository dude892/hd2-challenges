using System.Text;

namespace Hd2Challenges.Models;

public sealed class RedemptionState
{
    private readonly Func<RedemptionDifficulty, IEnumerable<Warbond>, ItemSet> _resolveStarterItems;
    private readonly HashSet<Warbond> _selectedWarbonds;

    public RedemptionState(
        RedemptionDifficulty difficulty,
        IEnumerable<Warbond> selectedWarbonds,
        Func<RedemptionDifficulty, IEnumerable<Warbond>, ItemSet> resolveStarterItems)
    {
        Difficulty = difficulty;
        _selectedWarbonds = [.. selectedWarbonds];
        CurrentOperation = difficulty.StartOperation.Clone();
        _resolveStarterItems = resolveStarterItems;
        StarterItems = _resolveStarterItems(Difficulty, _selectedWarbonds);
        RunStarted = false;
    }

    public RedemptionState(
        RedemptionStateDefinition definition,
        Func<string, RedemptionDifficulty> resolveDifficulty,
        Func<int, int, Operation> resolveOperation,
        Func<IEnumerable<string>, ItemSet> resolveItemSet,
        Func<RedemptionDifficulty, IEnumerable<Warbond>, ItemSet> resolveStarterItems,
        Func<IEnumerable<string>, HashSet<Warbond>> resolveWarbonds)
    {
        Difficulty = resolveDifficulty(definition.DifficultyId);
        _selectedWarbonds = resolveWarbonds(definition.SelectedWarbonds);
        BannedItems = resolveItemSet(definition.BannedItemIds);
        AcquiredItems = resolveItemSet(definition.AcquiredItemIds);
        PendingRewardItems = resolveItemSet(definition.PendingRewardIds);
        PendingPunishmentItems = resolveItemSet(definition.PendingPunishmentIds);
        CurrentOperation = resolveOperation(definition.OperationIndex, definition.MissionNumber);
        MissionsFailed = definition.MissionsFailed;
        RunCompleted = definition.RunCompleted;
        _resolveStarterItems = resolveStarterItems;
        StarterItems = _resolveStarterItems(Difficulty, _selectedWarbonds);
    }

    public bool RunStarted { get; set; } = true;
    public RedemptionDifficulty Difficulty { get; }
    public IReadOnlySet<Warbond> SelectedWarbonds => _selectedWarbonds;
    public ItemSet BannedItems { get; } = new();
    public ItemSet AcquiredItems { get; } = new();
    public ItemSet PendingRewardItems { get; set; } = new();
    public ItemSet PendingPunishmentItems { get; set; } = new();
    public Operation CurrentOperation { get; set; }
    public int MissionsFailed { get; set; }
    public bool RunCompleted { get; set; }

    public ItemSet StarterItems { get; private set; }
    public bool CanCompleteMission => !RunCompleted && PendingPunishmentItems.Count == 0;
    public bool CanFailMission => !RunCompleted && PendingRewardItems.Count == 0;

    public bool SetWarbondSelected(Warbond warbond, bool isSelected)
    {
        if (warbond.AlwaysEnabled)
        {
            return false;
        }

        bool changed = isSelected
            ? _selectedWarbonds.Add(warbond)
            : _selectedWarbonds.Remove(warbond);

        if (changed)
        {
            StarterItems = _resolveStarterItems(Difficulty, _selectedWarbonds);
        }

        return changed;
    }

    public string SummaryText
    {
        get
        {
            int score = Difficulty.ScoreModifier - (MissionsFailed * 50);
            StringBuilder builder = new();

            builder.AppendLine("Warpath: Redemption Summary");
            builder.AppendLine("===========================");
            builder.AppendLine();
            builder.AppendLine($"Difficulty: {Difficulty.DisplayName}");
            
            if (RunCompleted)
            {
                builder.AppendLine("Run Completed");
                builder.AppendLine($"Missions Failed: {MissionsFailed}");
                builder.AppendLine($"Final Score: {score}");
            }
            else
            {
                builder.AppendLine($"Current Operation: {CurrentOperation.DifficultyLabel}, {CurrentOperation.MissionLabel}");
                builder.AppendLine($"Missions Failed: {MissionsFailed}");
                builder.AppendLine($"Current Score: {score}");
            }

            builder.AppendLine();

            builder.AppendLine("Selected Warbonds:");
            if (SelectedWarbonds.Count == 0)
            {
                builder.AppendLine("  - None");
            }
            else
            {
                foreach (var warbond in SelectedWarbonds.OrderBy(item => item.DisplayName, StringComparer.OrdinalIgnoreCase))
                {
                    builder.AppendLine($"  - {warbond.DisplayName}");
                }
            }

            builder.AppendLine();
            builder.AppendLine("Banned Items:");
            if (BannedItems.Count == 0)
            {
                builder.AppendLine("  - None");
            }
            else
            {
                foreach (var item in BannedItems.AllItems.OrderBy(item => item.DisplayName, StringComparer.OrdinalIgnoreCase))
                {
                    builder.AppendLine($"  - {item.DisplayName}");
                }
            }

            builder.AppendLine();
            builder.AppendLine("Pending Rewards:");
            if (PendingRewardItems.Count == 0)
            {
                builder.AppendLine("  - None");
            }
            else
            {
                foreach (var item in PendingRewardItems.AllItems.OrderBy(item => item.DisplayName, StringComparer.OrdinalIgnoreCase))
                {
                    builder.AppendLine($"  - {item.DisplayName}");
                }
            }

            builder.AppendLine();
            builder.AppendLine("Pending Punishments:");
            if (PendingPunishmentItems.Count == 0)
            {
                builder.AppendLine("  - None");
            }
            else
            {
                foreach (var item in PendingPunishmentItems.AllItems.OrderBy(item => item.DisplayName, StringComparer.OrdinalIgnoreCase))
                {
                    builder.AppendLine($"  - {item.DisplayName}");
                }
            }

            builder.AppendLine();
            builder.AppendLine("Acquired Items:");
            foreach (var (label, items) in AcquiredItems.CategoryGroups)
            {
                builder.AppendLine($"{label}:");

                if (!items.Any())
                {
                    builder.AppendLine("  - None");
                }
                else
                {
                    foreach (var item in items)
                    {
                        builder.AppendLine($"  - {item.DisplayName}");
                    }
                }

                builder.AppendLine();
            }

            return builder.ToString();
        }
    }

    public RedemptionStateDefinition ToDefinition() => new()
    {
        DifficultyId = Difficulty.Id,
        OperationIndex = CurrentOperation.DifficultyIndex,
        MissionNumber = CurrentOperation.MissionNumber,
        MissionsFailed = MissionsFailed,
        RunCompleted = RunCompleted,
        SelectedWarbonds = [.. SelectedWarbonds.Select(item => item.Id)],
        AcquiredItemIds = [.. AcquiredItems.Select(item => item.Id)],
        BannedItemIds = [.. BannedItems.Select(item => item.Id)],
        PendingRewardIds = [.. PendingRewardItems.Select(item => item.Id)],
        PendingPunishmentIds = [.. PendingPunishmentItems.Select(item => item.Id)],
    };
}
