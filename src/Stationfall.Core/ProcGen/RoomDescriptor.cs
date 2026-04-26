namespace Stationfall.Core.ProcGen;

public record RoomDescriptor(
    string Id,
    RoomType Type,
    string TemplateName,
    IReadOnlyDictionary<CardinalDirection, DoorDescriptor> Doors
)
{
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
