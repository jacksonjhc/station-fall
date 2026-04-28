namespace Stationfall.Core.ProcGen;

public record RoomDescriptor(
    string Id,
    RoomType Type,
    string TemplateName,
    IReadOnlyDictionary<CardinalDirection, DoorDescriptor> Doors
)
{
    // Tier is the run's current difficulty tier broadcast onto each room.
    // Per-room overrides (e.g. boss arenas getting a tier bump) can layer on
    // later via `with` without breaking existing call sites.
    public ContentTier ContentTier { get; init; } = ContentTier.Onboarding;

    // Set by the generator on a room that should host the run's key pickup.
    // Godot-side instantiation reads this in M5-4 to spawn the pickup.
    public bool ContainsKey { get; init; }

    public bool TryGetDoor(CardinalDirection direction, out DoorDescriptor door)
    {
        if (Doors.TryGetValue(direction, out var found))
        {
            door = found;
            return true;
        }
        door = null!;
        return false;
    }

    public IEnumerable<KeyValuePair<CardinalDirection, DoorDescriptor>> AllDoors => Doors;
}
