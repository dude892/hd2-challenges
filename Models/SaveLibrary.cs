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

    public void RenameCurrentSlot(string? name)
    {
        if (CurrentSlot is null || string.IsNullOrWhiteSpace(name))
        {
            return;
        }

        CurrentSlot.Name = name;
        CurrentSlot.UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void DeleteSlot(Guid slotId, Func<SaveSlot<TState>> replacementFactory)
    {
        bool deletingCurrentSlot = CurrentSlotId == slotId;
        WorkingSlot = null;
        Slots.RemoveAll(slot => slot.Id == slotId);

        if (deletingCurrentSlot || Slots.Count == 0)
        {
            WorkingSlot = replacementFactory();
            CurrentSlotId = null;
        }
    }
}

public sealed class SaveExport<TState>
{
    public int Version { get; set; } = 1;
    public string Mode { get; set; } = string.Empty;
    public SaveSlot<TState> Slot { get; set; } = default!;
}
