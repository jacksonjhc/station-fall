using Godot;
using Stationfall.Godot.Dungeon;

namespace Stationfall.Godot.Enemies;

// Authoring node for "this room contains N enemies." Sits as a child of the
// room scene; spawns the configured EnemyResource at each child Marker2D when
// the room transitions to Active (first entry, RequiresClear=true).
//
// Doesn't spawn when the room is restored Cleared on re-entry — DungeonRoot
// pre-marks the room Cleared *before* OnPlayerEntered, so the Active
// transition never fires and this handler stays dormant.
public partial class EnemySpawner : Node2D
{
    [Export] public EnemyResource? Definition { get; set; }
    [Export] public NodePath RoomControllerPath { get; set; } = "..";
    // If left empty, the spawner spawns one enemy at its own position. With
    // child Marker2Ds set, spawns one per marker at that marker's global pos.
    [Export] public NodePath SpawnMarkersParentPath { get; set; } = ".";

    private RoomController? _room;
    private int _aliveCount;
    private bool _hasSpawned;

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
        var markers = CollectMarkers();
        var positions = markers.Count > 0 ? markers : new() { GlobalPosition };

        foreach (var pos in positions)
        {
            var enemy = Definition.Scene.Instantiate<EnemyController>();
            enemy.Definition = Definition;
            enemy.GlobalPosition = pos;
            enemy.Died += OnEnemyDied;
            // Parent under the room so it tears down with the room scene if the
            // player exits mid-fight (not possible today with EnemyLocked doors,
            // but the lifetime contract is "enemies belong to their room").
            _room.AddChild(enemy);
            _aliveCount++;
        }

        if (_aliveCount == 0)
        {
            // No spawn points and Definition.Scene was somehow nullable — clear
            // immediately so the room doesn't deadlock.
            _room.Clear();
        }
    }

    private System.Collections.Generic.List<Vector2> CollectMarkers()
    {
        var positions = new System.Collections.Generic.List<Vector2>();
        var parent = string.IsNullOrEmpty(SpawnMarkersParentPath)
            ? null
            : GetNodeOrNull<Node>(SpawnMarkersParentPath);
        if (parent == null) return positions;

        foreach (var child in parent.GetChildren())
        {
            // Marker2D is the canonical spawn-point node; accept any Node2D too so
            // designers can drop a plain Position-bearing node and have it work.
            if (child is Marker2D marker) positions.Add(marker.GlobalPosition);
        }
        return positions;
    }

    private void OnEnemyDied()
    {
        _aliveCount--;
        if (_aliveCount <= 0 && _room != null && !_room.IsCleared())
            _room.Clear();
    }
}
