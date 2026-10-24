using System.Text.Json;
using Hd2Challenges.Models;
using Hd2Challenges.Services;

namespace Hd2Challenges.EndToEnd.Tests;

public sealed class SaveObject
{
    public string FilePath { get; }
    public SaveSlot<RedemptionStateDefinition> Slot { get; }

    public RedemptionState ResolveState(RedemptionCatalogService catalog) => catalog.CreateRedemptionState(Slot.State);

    public SaveObject(string fileName)
    {
        FilePath = Path.Combine(AppContext.BaseDirectory, "Saves", fileName);
        SaveExport<RedemptionStateDefinition>? save = JsonSerializer.Deserialize<SaveExport<RedemptionStateDefinition>>(
            File.ReadAllText(FilePath), new JsonSerializerOptions(JsonSerializerDefaults.Web));
        if (save?.Slot?.State is null)
        {
            throw new InvalidDataException($"Save fixture '{FilePath}' must contain a slot with a state.");
        }

        Slot = save.Slot;
    }
}
