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

    // Key-consumed doors stay open from both sides for the rest of the run
    // (W7 sticky-open rule). The descriptor on each side still says
    // KeyLocked, so the unlock state has to live somewhere outside the
    // descriptor — here, keyed by canonical edge id (sorted room-pair).
    // Both DoorNodes (FarRoom east + VaultRoom west) hash to the same entry.
    public HashSet<string> UnlockedDoorEdges { get; } = new();

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

    public static string EdgeId(string a, string b) =>
        string.CompareOrdinal(a, b) <= 0 ? $"{a}|{b}" : $"{b}|{a}";

    public bool IsDoorUnlocked(string roomA, string roomB) =>
        UnlockedDoorEdges.Contains(EdgeId(roomA, roomB));

    public void MarkDoorUnlocked(string roomA, string roomB) =>
        UnlockedDoorEdges.Add(EdgeId(roomA, roomB));
}
