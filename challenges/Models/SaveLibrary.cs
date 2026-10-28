using System.Text.Json.Serialization;

namespace Hd2Challenges.Models;

[JsonConverter(typeof(JsonStringEnumConverter<RunStatus>))]
public enum RunStatus
{
    InProgress,
    Completed,
    Failed
}

public sealed record SaveSlot<TState>
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string Name { get; init; } = string.Empty;
    public DateTimeOffset UpdatedAt { get; init; } = DateTimeOffset.UtcNow;
    public RunStatus Status { get; init; }
    public TState State { get; init; } = default!;
}

public sealed class SaveLibrary<TState>
{
    public Guid? CurrentSlotId { get; set; }
    public List<SaveSlot<TState>> Slots { get; set; } = [];

    [JsonIgnore]
    public SaveSlot<TState>? WorkingSlot { get; set; }

    [JsonIgnore]
    public SaveSlot<TState>? CurrentSlot => WorkingSlot ?? Slots.FirstOrDefault(slot => slot.Id == CurrentSlotId);

    public void CommitWorkingSlot()
    {
        if (WorkingSlot is null)
        {
            return;
        }

        Slots.Add(WorkingSlot);
        CurrentSlotId = WorkingSlot.Id;
        WorkingSlot = null;
    }

    public void AddSlot(SaveSlot<TState> slot)
    {
        WorkingSlot = null;
        Slots.Add(slot);
        CurrentSlotId = slot.Id;
    }

    public SaveSlot<TState>? SelectSlot(Guid slotId)
    {
        WorkingSlot = null;
        CurrentSlotId = slotId;
        return CurrentSlot;
    }

    public bool RenameSlot(Guid slotId, string? name)
    {
        int slotIndex = Slots.FindIndex(entry => entry.Id == slotId);
        if (slotIndex == -1 || string.IsNullOrWhiteSpace(name))
        {
            return false;
        }

        SaveSlot<TState> slot = Slots[slotIndex];
        string normalizedName = name.Trim();
        if (slot.Name == normalizedName)
        {
            return false;
        }

        Slots[slotIndex] = slot with { Name = normalizedName, UpdatedAt = DateTimeOffset.UtcNow };
        return true;
    }

    public bool ReplaceSlot(SaveSlot<TState> slot)
    {
        if (WorkingSlot?.Id == slot.Id)
        {
            WorkingSlot = slot;
            return true;
        }

        int slotIndex = Slots.FindIndex(entry => entry.Id == slot.Id);
        if (slotIndex == -1)
        {
            return false;
        }

        Slots[slotIndex] = slot;
        return true;
    }

    public (bool Deleted, bool CreatedReplacement) DeleteSlot(Guid slotId, Func<SaveSlot<TState>> replacementFactory)
    {
        bool deleted = false;
        bool creatingReplacement = false;

        if (Slots.RemoveAll(slot => slot.Id == slotId) > 0)
        {
            deleted = true;
        }
        else
        {
            return (deleted, creatingReplacement);
        }

        bool deletingCurrentSlot = CurrentSlotId == slotId;
        creatingReplacement = deletingCurrentSlot || (WorkingSlot is null && Slots.Count == 0);
        if (creatingReplacement)
        {
            WorkingSlot = replacementFactory();
            CurrentSlotId = null;
        }

        return (deleted, creatingReplacement);
    }

    public SaveSlot<TState>? GetSlot(Guid slotId)
    {
        return Slots.FirstOrDefault(entry => entry.Id == slotId);
    }
}

public sealed record SaveExport<TState>
{
    public int Version { get; init; } = 1;
    public string Mode { get; init; } = string.Empty;
    public SaveSlot<TState> Slot { get; init; } = default!;
}
