namespace Stationfall.Core.Runs;

// Snapshot of one room's runtime state at the moment the player left it.
// IsCleared mirrors RoomController.IsCleared on capture; Entities holds
// per-entity records keyed by their author-assigned EntityId (room-scoped).
public class RoomState
{
    public bool IsCleared { get; set; }
    public Dictionary<string, EntityState> Entities { get; } = new();
}
