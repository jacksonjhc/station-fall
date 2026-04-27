using Godot;
using Stationfall.Core.Runs;
using Stationfall.Godot.Dungeon;
using Stationfall.Godot.Persistence;

namespace Stationfall.Godot.Enemies;

// Authoring node for "this room contains N enemies." Sits as a child of the
// room scene; spawns the configured EnemyResource at each child Marker2D when
// the room transitions to Active (first entry, RequiresClear=true).
//
// Persistence: per-marker death tracking via IPersistentEntity. When the
// player partially clears a room and leaves, the markers whose enemy died
// are captured; on re-entry the spawner skips those markers and only
// re-spawns the survivors at full HP. Matches the user-requested model
// where partial-kills heal up but full-kills stay dead — and prevents
// infinite re-rolls of an unkilled patient when the player exits via an
// unbarred door.
//
// Doesn't spawn when the room is restored Cleared on re-entry — DungeonRoot
// pre-marks the room Cleared *before* OnPlayerEntered, so the Active
// transition never fires and this handler stays dormant.
public partial class EnemySpawner : Node2D, IPersistentEntity
{
    [Export] public string EntityId { get; set; } = "spawner";
    [Export] public EnemyResource? Definition { get; set; }
    [Export] public NodePath RoomControllerPath { get; set; } = "..";
    // If left empty, the spawner spawns one enemy at its own position. With
    // child Marker2Ds set, spawns one per marker at that marker's global pos.
    [Export] public NodePath SpawnMarkersParentPath { get; set; } = ".";

    private RoomController? _room;
    private int _aliveCount;
    private bool _hasSpawned;
    // Markers whose enemy was killed. Persists across room exits via
    // EnemySpawnerState; on re-entry RestoreState seeds this set so Spawn()
    // skips them.
    private readonly System.Collections.Generic.HashSet<string> _deadMarkers = new();
    // Fallback id for the no-markers case where the spawner uses its own
    // position. Synthetic so a no-markers spawner still partakes in
    // per-spawner persistence.
    private const string SelfMarkerId = "_self";

    public override void _Ready()
    {
        _room = GetNodeOrNull<RoomController>(RoomControllerPath);
        if (_room == null)
        {
            GD.PushError($"EnemySpawner '{Name}' could not resolve RoomController at '{RoomControllerPath}'");
            return;
        }
        _room.StateChanged += OnRoomStateChanged;
    }

    private void OnRoomStateChanged(int next)
    {
        if (_hasSpawned) return;
        if ((RoomLifecycle)next != RoomLifecycle.Active) return;
        Spawn();
    }

    private void Spawn()
    {
        if (Definition == null || Definition.Scene == null)
        {
            GD.PushError($"EnemySpawner '{Name}': Definition or Definition.Scene is null");
            return;
        }
        if (_room == null) return;

        _hasSpawned = true;
        var entries = CollectMarkers();
        if (entries.Count == 0)
            entries.Add((SelfMarkerId, GlobalPosition));

        foreach (var (markerId, pos) in entries)
        {
            if (_deadMarkers.Contains(markerId)) continue;

            var enemy = Definition.Scene.Instantiate<EnemyController>();
            enemy.Definition = Definition;
            enemy.GlobalPosition = pos;
            // Closure capture so OnEnemyDied knows which marker just emptied.
            // Without this the spawner couldn't tell partial-clear from
            // total-clear at the per-marker level.
            string capturedId = markerId;
            enemy.Died += () => OnEnemyDied(capturedId);
            // Parent under the room so it tears down with the room scene if the
            // player exits mid-fight. Live enemies are not snapshotted — the
            // dead-markers set drives re-spawn decisions instead.
            _room.AddChild(enemy);
            _aliveCount++;
        }

        if (_aliveCount == 0)
        {
            // Either no markers/positions resolved, or every marker is already
            // dead from a prior visit. Either way, nothing to fight — clear
            // the room so doors unlock and the player isn't soft-locked.
            _room.Clear();
        }
    }

    private System.Collections.Generic.List<(string Id, Vector2 Position)> CollectMarkers()
    {
        var list = new System.Collections.Generic.List<(string, Vector2)>();
        var parent = string.IsNullOrEmpty(SpawnMarkersParentPath)
            ? null
            : GetNodeOrNull<Node>(SpawnMarkersParentPath);
        if (parent == null) return list;

        foreach (var child in parent.GetChildren())
        {
            // Marker2D is the canonical spawn-point node; accept any Node2D too so
            // designers can drop a plain Position-bearing node and have it work.
            // Marker name is the per-marker id — stable across .tscn re-instantiation
            // because Godot scene nodes round-trip their authored Name.
            if (child is Marker2D marker)
                list.Add((marker.Name, marker.GlobalPosition));
        }
        return list;
    }

    private void OnEnemyDied(string markerId)
    {
        _aliveCount--;
        _deadMarkers.Add(markerId);
        if (_aliveCount <= 0 && _room != null && !_room.IsCleared())
            _room.Clear();
    }

    public EntityState? CaptureState() =>
        new EnemySpawnerState(_deadMarkers.ToArray());

    public void RestoreState(EntityState state)
    {
        if (state is not EnemySpawnerState s) return;
        _deadMarkers.Clear();
        foreach (var id in s.DeadMarkerIds) _deadMarkers.Add(id);
    }
}
