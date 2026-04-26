namespace Stationfall.Core.ProcGen;

public record DungeonLayout(
    IReadOnlyList<RoomDescriptor> Rooms,
    string EntryRoomId
)
{
    private readonly Dictionary<string, RoomDescriptor> _byId = Rooms.ToDictionary(r => r.Id);

    public RoomDescriptor GetRoom(string id) => _byId[id];

    public bool TryGetRoom(string id, out RoomDescriptor room)
    {
        if (_byId.TryGetValue(id, out var found))
        {
            room = found;
            return true;
        }
        room = null!;
        return false;
    }

    public bool ContainsRoom(string id) => _byId.ContainsKey(id);
    public int RoomCount => Rooms.Count;
}
