using System.Text;
using System.Diagnostics.CodeAnalysis;
using Hd2Challenges.Models;

namespace Hd2Challenges.Services;

/// <summary>
/// Owns the active runtime state, resolves saved definitions through the catalog,
/// and coordinates run lifecycle, mission progression, rewards, and persistence.
/// </summary>
public sealed class RedemptionSessionService(
    CatalogService baseCatalog,
    RedemptionCatalogService redemptionCatalog,
    RedemptionSaveLibraryService saves)
{
    private const int HardFinaleOperationIndex = 5;
    private const int MaximumPunishmentChoices = 3;
    private RedemptionState? _state;

    public RedemptionState State { get { EnsureLoaded(); return _state; } }

    public RunStatus CurrentRunStatus => CurrentSlot?.Status ?? RunStatus.InProgress;
    public bool CanCompleteMission => CurrentRunStatus == RunStatus.InProgress && State.PendingPunishmentItems.Count == 0;
    public bool CanFailMission => CurrentRunStatus == RunStatus.InProgress && State.PendingRewardItems.Count == 0;

    public SaveSlot<RedemptionStateDefinition>? CurrentSlot => saves.Library.CurrentSlot;
    public Guid? CurrentSlotId => CurrentSlot?.Id;
    public bool HasWorkingSlot => saves.Library.WorkingSlot is not null;
    public IReadOnlyList<SaveSlot<RedemptionStateDefinition>> Slots => saves.Library.Slots.AsReadOnly();

    public async Task LoadAsync()
    {
        await redemptionCatalog.EnsureLoadedAsync();
        await saves.LoadAsync();

        if (saves.Library.CurrentSlotId is null || saves.Library.CurrentSlot is null && saves.Library.Slots.Count == 0)
        {
            _state = redemptionCatalog.CreateRedemptionSetupState(redemptionCatalog.GetDifficulty("normal"), baseCatalog.GetAllWarbonds());
            saves.SetWorkingSlot(_state.ToDefinition(), _state.Difficulty.DisplayName);
            
            return;
        }

        SaveSlot<RedemptionStateDefinition> slot = saves.Library.CurrentSlot ?? saves.Library.Slots.First();
        if (saves.Library.CurrentSlotId != slot.Id)
        {
            slot = await saves.SelectSlotAsync(slot.Id);
        }

        _state = redemptionCatalog.CreateRedemptionState(slot.State);
    }

    public void ChangeDifficulty(string difficultyId) => State.ChangeDifficulty(redemptionCatalog.GetDifficulty(difficultyId));

    public bool SetWarbondSelected(Warbond warbond, bool selected) => State.SetWarbondSelected(warbond, selected);

    public async Task RestartAsync()
    {
        RedemptionState setup = redemptionCatalog.CreateRedemptionSetupState(State.Difficulty, State.SelectedWarbonds);
        await saves.ResetWorkingSlotAsync(setup.ToDefinition(), setup.Difficulty.DisplayName);
        _state = setup;
    }

    public async Task BeginRunAsync()
    {
        EnsureLoaded();
        if (saves.Library.WorkingSlot is null)
        {
            return;
        }

        State.BeginRun();
        await saves.CommitWorkingSlotAsync(State.ToDefinition(), State.Difficulty.DisplayName, CurrentRunStatus);
    }

    public void SetRunStatus(RunStatus status)
    {
        EnsureLoaded();
        saves.SetCurrentRunStatus(status);
    }

    public MissionCompletionResult CompleteMission(int stars)
    {
        if (!CanCompleteMission)
        {
            return MissionCompletionResult.Unavailable;
        }

        if (State.CurrentOperation.DifficultyIndex == baseCatalog.LastOperationIndex && State.CurrentOperation.IsLastMission)
        {
            SetRunStatus(RunStatus.Completed);
            return MissionCompletionResult.ChallengeCompleted;
        }

        if (State.PendingRewardItems.Count == 0)
        {
            State.PendingRewardItems = RollRewardItems(stars);
        }

        return State.PendingRewardItems.Count > 0 ? MissionCompletionResult.RewardsPending : MissionCompletionResult.NoRewardsAvailable;
    }

    public bool PrepareMissionFailure()
    {
        if (!CanFailMission || State.PendingPunishmentItems.Count != 0)
        {
            return false;
        }

        var punishmentPool = State.AcquiredItems
            .Where(item => !State.StarterItems.ContainsItem(item))
            .OrderBy(_ => Random.Shared.Next())
            .Take(MaximumPunishmentChoices);
        State.PendingPunishmentItems = new ItemSet(punishmentPool);
        return true;
    }

    public bool ClaimMissionReward(string itemId)
    {
        if (!CanCompleteMission)
        {
            return false;
        }

        GameItem? reward = State.PendingRewardItems.FirstOrDefault(item => item.Id == itemId);
        if (reward is null)
        {
            return false;
        }

        State.AcquiredItems.AddItem(reward);
        State.PendingRewardItems.Clear();
        AdvanceMission();
        return true;
    }

    public bool SkipAndBanMissionRewards()
    {
        if (!CanCompleteMission || State.PendingRewardItems.Count == 0)
        {
            return false;
        }

        State.BannedItems.AddItems(State.PendingRewardItems);
        State.PendingRewardItems.Clear();
        AdvanceMission();
        return true;
    }

    public bool ClaimMissionPunishment(string itemId)
    {
        if (!CanFailMission)
        {
            return false;
        }

        GameItem? punishment = State.PendingPunishmentItems.FirstOrDefault(item => item.Id == itemId);
        if (punishment is null)
        {
            return false;
        }

        State.AcquiredItems.RemoveItem(punishment);
        State.BannedItems.RemoveItem(punishment);
        State.PendingPunishmentItems.Clear();
        State.MissionsFailed++;
        State.CurrentOperation.MissionNumber = 1;
        return true;
    }

    private void AdvanceMission()
    {
        if (State.CurrentOperation.MissionNumber < State.CurrentOperation.MissionCount)
        {
            State.CurrentOperation.MissionNumber++;
        }
        else if (State.CurrentOperation.DifficultyIndex < baseCatalog.LastOperationIndex)
        {
            State.CurrentOperation = baseCatalog.GetOperation(State.CurrentOperation.DifficultyIndex + 1);
        }
    }

    private ItemSet RollRewardItems(int selectedStars)
    {
        int rewardQuantity = Math.Max(1, selectedStars - 1);
        if (State.Difficulty.IsSuper)
        {
            rewardQuantity--;
        }

        rewardQuantity = Math.Max(1, rewardQuantity);

        ItemSet rewardPool = baseCatalog.GetItemsByWarbonds(State.SelectedWarbonds)
            .RemoveItems([State.StarterItems, State.BannedItems, State.AcquiredItems]);
        if (State.CurrentOperation.DifficultyIndex == HardFinaleOperationIndex && State.CurrentOperation.IsLastMission)
        {
            rewardPool = new ItemSet(rewardPool.Where(item => item.Antitank));
        }

        return new ItemSet(
            rewardPool.AllItems
                .DistinctBy(item => item.Id)
                .OrderBy(_ => Random.Shared.Next())
                .Take(rewardQuantity));
    }

    public Task PersistAsync() => saves.PersistAsync(State.ToDefinition(), CurrentRunStatus);

    public Task CreateSnapshotAsync() => State.RunStarted
        ? saves.CreateSnapshotAsync(State.ToDefinition(), State.Difficulty.DisplayName, CurrentRunStatus)
        : Task.CompletedTask;

    public async Task SelectSlotAsync(Guid slotId)
    {
        EnsureLoaded();
        SaveSlot<RedemptionStateDefinition> slot = await saves.SelectSlotAsync(slotId);
        _state = redemptionCatalog.CreateRedemptionState(slot.State);
    }

    public async Task<bool> DeleteSlotAsync(Guid slotId)
    {
        RedemptionState current = State;
        RedemptionState? replacement = null;
        bool deleted = await saves.DeleteSlotAsync(slotId, () =>
        {
            replacement = redemptionCatalog.CreateRedemptionSetupState(current.Difficulty, current.SelectedWarbonds);
            return replacement.ToDefinition();
        }, current.Difficulty.DisplayName);
        if (replacement is not null)
        {
            _state = replacement;
        }

        return deleted;
    }

    public async Task<SaveSlot<RedemptionStateDefinition>?> ImportAsync(string json)
    {
        EnsureLoaded();
        
        var result = await saves.ImportAsync(json);

        if (result is null)
        {
            return null;
        }

        _state = redemptionCatalog.CreateRedemptionState(result.State);
        return result;
    }

    public SaveSlot<RedemptionStateDefinition>? GetSlot(Guid slotId) => saves.GetSlot(slotId);

    public Task<bool> RenameSlotAsync(Guid slotId, string? name) => saves.RenameSlotAsync(slotId, name);

    public SaveExport<RedemptionStateDefinition> CreateExport(Guid slotId) => saves.CreateExport(slotId);

    [MemberNotNull(nameof(_state))]
    private void EnsureLoaded()
    {
        if (_state is null)
        {
            throw new InvalidOperationException("The Redemption session has not been loaded.");
        }
    }

    public string GetCurrentRunSummary()
    {
        int score = State.Difficulty.ScoreModifier - (State.MissionsFailed * 50);
        StringBuilder builder = new();

        builder.AppendLine("Warpath: Redemption Summary");
        builder.AppendLine("===========================");
        builder.AppendLine();
        builder.AppendLine($"Difficulty: {State.Difficulty.DisplayName}");

        if (CurrentRunStatus == RunStatus.Completed)
        {
            builder.AppendLine("Run Completed");
            builder.AppendLine($"Missions Failed: {State.MissionsFailed}");
            builder.AppendLine($"Final Score: {score}");
        }
        else if (CurrentRunStatus == RunStatus.Failed)
        {
            builder.AppendLine("Run Failed");
            builder.AppendLine($"Missions Failed: {State.MissionsFailed}");
            builder.AppendLine($"Final Score: {score}");
        }
        else
        {
            builder.AppendLine($"Current Operation: {State.CurrentOperation.DifficultyLabel}, {State.CurrentOperation.MissionLabel}");
            builder.AppendLine($"Missions Failed: {State.MissionsFailed}");
            builder.AppendLine($"Current Score: {score}");
        }

        builder.AppendLine();
        builder.AppendLine("Selected Warbonds:");
        if (State.SelectedWarbonds.Count == 0)
        {
            builder.AppendLine("  - None");
        }
        else
        {
            foreach (var warbond in State.SelectedWarbonds.OrderBy(item => item.DisplayName, StringComparer.OrdinalIgnoreCase))
            {
                builder.AppendLine($"  - {warbond.DisplayName}");
            }
        }

        builder.AppendLine();
        AppendSummarySection(builder, "Banned Items:", State.BannedItems);
        AppendSummarySection(builder, "Pending Rewards:", State.PendingRewardItems);
        AppendSummarySection(builder, "Pending Punishments:", State.PendingPunishmentItems);

        builder.AppendLine("Acquired Items:");
        foreach (var category in State.AcquiredItems.Categories)
        {
            builder.AppendLine($"{category.Label}:");
            if (category.Count == 0)
            {
                builder.AppendLine("  - None");
            }
            else
            {
                foreach (var item in category)
                {
                    builder.AppendLine($"  - {item.DisplayName}");
                }
            }

            builder.AppendLine();
        }

        return builder.ToString();
    }

    private static void AppendSummarySection(StringBuilder builder, string heading, ItemSet items)
    {
        builder.AppendLine(heading);
        if (items.Count == 0)
        {
            builder.AppendLine("  - None");
            builder.AppendLine();
            return;
        }

        foreach (var item in items.AllItems.OrderBy(item => item.DisplayName, StringComparer.OrdinalIgnoreCase))
        {
            builder.AppendLine($"  - {item.DisplayName}");
        }

        builder.AppendLine();
    }
}
