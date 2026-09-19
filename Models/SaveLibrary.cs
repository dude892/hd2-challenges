using System.Text.Json.Serialization;

namespace Hd2Challenges.Models;

public sealed class SaveSlot<TState>
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public bool IsCompleted { get; set; }
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
}

public sealed class SaveExport<TState>
{
    public int Version { get; set; } = 1;
    public string Mode { get; set; } = string.Empty;
    public SaveSlot<TState> Slot { get; set; } = default!;
}
