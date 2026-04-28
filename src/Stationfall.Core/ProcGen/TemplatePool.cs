namespace Stationfall.Core.ProcGen;

// Per-RoomType list of available template names. Generator picks one name
// per room from the pool using its seeded RNG, keeping layouts deterministic.
//
// Godot side resolves the chosen name to a PackedScene via DungeonInstantiator.
// Names are content-authored strings — the generator never reads them, only
// chooses among them.
public sealed record TemplatePool(IReadOnlyDictionary<RoomType, IReadOnlyList<string>> ByRoomType)
{
    public static TemplatePool Default { get; } = new(new Dictionary<RoomType, IReadOnlyList<string>>
    {
        [RoomType.Entry] = new[] { "EntryRoom" },
        [RoomType.Combat] = new[] { "Combat_Generic" },
        [RoomType.Item] = new[] { "Item_Generic" },
        [RoomType.Vendor] = new[] { "Vendor_Generic" },
        [RoomType.Boss] = new[] { "Boss_Generic" },
    });

    public IReadOnlyList<string> For(RoomType type)
    {
        if (!ByRoomType.TryGetValue(type, out var names) || names.Count == 0)
            throw new InvalidOperationException(
                $"TemplatePool has no templates for RoomType.{type}. Provide at least one name in TemplatePool.ByRoomType.");
        return names;
    }
}
