using System.Text;

namespace Hd2Challenges.Models;

public sealed class PenitentState(
    PenitentStateDefinition definition,
    Func<string, PenitentDifficulty> resolveDifficulty,
    Func<int, int, Operation> resolveOperation,
    Func<IEnumerable<string>, ItemSet> resolveItemSet,
    Func<string, IEnumerable<Warbond>, ItemSet> resolveStarterItems,
    Func<IEnumerable<string>, HashSet<Warbond>> resolveWarbonds
)
{
    public bool RunStarted { get; set; } = true;
    public PenitentDifficulty Difficulty { get; } = resolveDifficulty(definition.DifficultyId);
    public HashSet<Warbond> SelectedWarbonds { get; set; } = resolveWarbonds(definition.SelectedWarbonds);
    public ItemSet BannedItems { get; set; } = resolveItemSet(definition.BannedItemIds);
    public ItemSet AcquiredItems { get; set; } = resolveItemSet(definition.AcquiredItemIds);
    public ItemSet PendingRewardItems { get; set; } = resolveItemSet(definition.PendingRewardIds);
    public ItemSet PendingPunishmentItems { get; set; } = resolveItemSet(definition.PendingPunishmentIds);
    public Operation CurrentOperation { get; set; } = resolveOperation(definition.OperationIndex, definition.MissionNumber);
    public int MissionsFailed { get; set; } = definition.MissionsFailed;
    public bool RunCompleted { get; set; } = definition.RunCompleted;

    public ItemSet StarterItems => new(resolveStarterItems(Difficulty.Id, SelectedWarbonds));
    public bool CanCompleteMission => !RunCompleted && PendingPunishmentItems.Count == 0;
    public bool CanFailMission => !RunCompleted && PendingRewardItems.Count == 0;

    public bool SetWarbondSelected(Warbond warbond, bool isSelected)
    {
        if (warbond.AlwaysEnabled)
        {
            return false;
        }

        if (isSelected)
        {
            SelectedWarbonds.Add(warbond);
        }
        else
        {
            SelectedWarbonds.Remove(warbond);
        }

        return true;
    }

    public string SummaryText
    {
        get
        {
            int score = Difficulty.ScoreModifier - (MissionsFailed * 50);
            StringBuilder builder = new();

            builder.AppendLine("Penitent Crusade Summary");
            builder.AppendLine("========================");
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

    public PenitentStateDefinition ToDefinition() => new()
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
