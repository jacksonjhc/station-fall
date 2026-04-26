namespace Stationfall.Core.Runs;

// Per-run state for the dungeon: which rooms have been visited, which are
// cleared, and the captured per-entity state of each room. DungeonRoot reads
// from and writes to this on every room transition.
//
// Sector-spanning state (toggles, switches that affect multiple rooms) is
// deferred until we actually have a use case — when it lands, add a separate
// SectorEntities bucket here rather than overloading per-room scoping.
public class DungeonState
{
    public Dictionary<string, RoomState> Rooms { get; } = new();
    public HashSet<string> VisitedRoomIds { get; } = new();
    public string ActiveRoomId { get; set; } = "";

    public RoomState GetOrCreateRoom(string roomId)
    {
        if (!Rooms.TryGetValue(roomId, out var state))
        {
            state = new RoomState();
            Rooms[roomId] = state;
        }
        return state;
    }

    public bool IsRoomCleared(string roomId) =>
        Rooms.TryGetValue(roomId, out var state) && state.IsCleared;
}
