using Stationfall.Core.Runs;

namespace Stationfall.Godot.Persistence;

// Implemented by Godot nodes whose state should survive room teardowns
// (pressure plates, breakable crates, switches, etc.). DungeonRoot collects
// these on room exit, snapshots their state into RunState.Dungeon, and pushes
// it back on re-entry.
//
// EntityId is room-scoped — only needs to be unique within its room scene.
// Designers set it via [Export] on the node.
public interface IPersistentEntity
{
    string EntityId { get; }

    // Returns the current state to persist, or null if this entity has nothing
    // worth saving (e.g., temporarily). Most implementations will always return
    // a record.
    EntityState? CaptureState();

    // Applies the saved state. Called once on room enter, after AddChild but
    // before OnPlayerEntered. The entity is responsible for any side effects
    // implied by the state (e.g., a destroyed crate calling QueueFree).
    void RestoreState(EntityState state);
}
