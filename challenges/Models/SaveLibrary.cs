using System.Text.Json.Serialization;

namespace Hd2Challenges.Models;

public sealed class SaveSlot<TState>
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public TState State { get; set; } = default!;
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
        var slot = Slots.FirstOrDefault(entry => entry.Id == slotId);
        if (slot is null || string.IsNullOrWhiteSpace(name))
        {
            return false;
        }

        string normalizedName = name.Trim();
        if (slot.Name == normalizedName)
        {
            return false;
        }

        slot.Name = normalizedName;
        slot.UpdatedAt = DateTimeOffset.UtcNow;
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

public sealed class SaveExport<TState>
{
    public int Version { get; set; } = 1;
    public string Mode { get; set; } = string.Empty;
    public SaveSlot<TState> Slot { get; set; } = default!;
}
