using System.Linq;
using Godot;
using Stationfall.Core.Entities;
using Stationfall.Core.ProcGen;
using Stationfall.Core.Progression;
using Stationfall.Core.Runs;
using Stationfall.Godot.Enemies;
using Stationfall.Godot.Items;
using Stationfall.Godot.Persistence;
using Stationfall.Godot.Player;
using Stationfall.Godot.UI;

namespace Stationfall.Godot.Dungeon;

// Owns the layout, instantiates the active room, persists the player across transitions.
// For M2 the layout is a hand-built fixture; M5 swaps in a generated layout.
public partial class DungeonRoot : Node2D
{
    [Signal] public delegate void RoomVisitedEventHandler(string roomId);
    [Signal] public delegate void RoomEnteredEventHandler(string roomId);

    [Export] public NodePath PlayerPath { get; set; } = "Player";
    [Export] public NodePath RoomSlotPath { get; set; } = "RoomSlot";
    [Export] public NodePath HealthBarPath { get; set; } = "HUD/HealthBar";
    [Export] public NodePath MinimapPath { get; set; } = "HUD/Minimap";
    [Export] public PackedScene? EntryRoomScene { get; set; }
    [Export] public PackedScene? WestHallScene { get; set; }
    [Export] public PackedScene? FarRoomScene { get; set; }
    [Export] public PackedScene? VaultRoomScene { get; set; }
    [Export] public PackedScene? RewardRoomScene { get; set; }

    public DungeonLayout Layout { get; private set; } = HandBuiltLayouts.M2Sandbox();
    public RunState State => _runState;
    public string ActiveRoomId => _runState.Dungeon.ActiveRoomId;
    public IReadOnlySet<string> VisitedRoomIds => _runState.Dungeon.VisitedRoomIds;
    public MetaState Meta => _meta;
    public RoomController? ActiveRoom => _activeRoom;
    public PlayerController? Player => _player;

    // M3 will inject this from outside (vessel select → run start). For now,
    // construct a default Clone-vessel run so DungeonRoot owns its own state.
    private readonly RunState _runState = new(PlayerVessel.CreateClone());
    private MetaState _meta = new();
    private PlayerController? _player;
    private Node2D? _roomSlot;
    private RoomController? _activeRoom;
    private HealthBar? _healthBar;
    private Minimap? _minimap;

    public override void _Ready()
    {
        _meta = SaveFile.LoadOrCreate();
        GD.Print($"[save] loaded MetaState ({_meta.NarrativeFlags.Count} flags)");

        _player = GetNodeOrNull<PlayerController>(PlayerPath);
        _roomSlot = GetNodeOrNull<Node2D>(RoomSlotPath);
        _healthBar = GetNodeOrNull<HealthBar>(HealthBarPath);
        _minimap = GetNodeOrNull<Minimap>(MinimapPath);

        if (_player != null)
        {
            // Apply the run's vessel before anything reads Stats. Player._Ready
            // ran first (depth-first) with its default Clone vessel; this is
            // where vessel-select (M8) will diverge from the sandbox default.
            _player.Configure(_runState.Vessel);
            if (!_player.IsInGroup("player")) _player.AddToGroup("player");
            if (_healthBar != null)
            {
                _healthBar.SetHealth(_player.Stats.Hp, _player.Stats.MaxHp);
                _player.HealthChanged += (hp, maxHp) => _healthBar.SetHealth((int)hp, (int)maxHp);
            }
            _player.Died += OnPlayerDied;
        }

        if (_minimap != null) _minimap.SetLayout(Layout);

        // Bind run currency + keys to the HUD-facing services. Deferred so
        // listeners (HUD counters, KeyLocked DoorNodes) that subscribe in
        // their own _Ready see the initial balance via the change-emit.
        CreditsService.Instance?.Bind(_runState);
        KeysService.Instance?.Bind(_runState);

        EnterRoom(Layout.EntryRoomId, fromDirection: null);
    }

    private void OnPlayerDied()
    {
        // M3 closing: prove the meta-save pipe is real end to end. Stamp a
        // first-death flag, persist, and reload the scene. RunState is
        // discarded — only MetaState survives.
        if (_meta.AddFlag("firstDeath"))
            GD.Print("[save] first death recorded");
        SaveFile.Save(_meta);
        GD.Print("[save] meta written; reloading scene");
        GetTree().ReloadCurrentScene();
    }

    private void EnterRoom(string roomId, CardinalDirection? fromDirection)
    {
        if (!Layout.TryGetRoom(roomId, out var descriptor))
        {
            GD.PushError($"DungeonRoot: room id '{roomId}' not in layout");
            return;
        }
        if (_roomSlot == null) return;

        // Capture leaving room's state before teardown.
        if (_activeRoom != null)
        {
            SnapshotRoom(_runState.Dungeon.ActiveRoomId, _activeRoom);
            _activeRoom.QueueFree();
            _activeRoom = null;
        }

        var scene = ResolveScene(descriptor.TemplateName);
        if (scene == null)
        {
            GD.PushError($"DungeonRoot: no scene wired for template '{descriptor.TemplateName}'");
            return;
        }

        var instance = scene.Instantiate<RoomController>();
        instance.RoomId = descriptor.Id;

        // Teleport the player BEFORE adding the new room to the tree.
        // The new room's door Area2Ds sit at the same world positions as the
        // doors the player just crossed. If the room is added first, Godot's
        // physics server runs an overlap query against the still-pre-teleport
        // body position and fires body_entered for the new Area2D. Pre-positioning
        // the body fixes the overlap query at registration. Door GlobalPosition
        // is well-defined on an un-parented instance because the room's local
        // transform is identity and RoomSlot is at world origin.
        PositionPlayer(instance, fromDirection);

        _roomSlot.AddChild(instance);
        _activeRoom = instance;
        _runState.Dungeon.ActiveRoomId = roomId;

        ConfigureDoors(instance, descriptor);

        // Push saved per-entity state into nodes before OnPlayerEntered runs,
        // so the room's entry logic sees correct entity state (e.g., a plate
        // already triggered, a crate already destroyed).
        RestoreRoom(roomId, instance);

        // Pre-apply persisted clear status BEFORE OnPlayerEntered. Otherwise
        // a freshly instantiated room briefly transitions Unexplored→Active
        // during OnPlayerEntered, firing StateChanged(Active) — which wakes
        // content spawners (EnemySpawner) that should stay dormant in a
        // previously-cleared room. With this set first, OnPlayerEntered sees
        // Cleared and returns early; doors update via the Cleared signal.
        if (_runState.Dungeon.IsRoomCleared(roomId) && !instance.IsCleared())
            instance.Clear();

        instance.OnPlayerEntered();

        if (_runState.Dungeon.VisitedRoomIds.Add(roomId))
        {
            EmitSignal(SignalName.RoomVisited, roomId);
            _minimap?.OnRoomVisited(roomId);
        }
        EmitSignal(SignalName.RoomEntered, roomId);
        _minimap?.SetActiveRoom(roomId);
    }

    private void SnapshotRoom(string roomId, RoomController room)
    {
        if (string.IsNullOrEmpty(roomId)) return;
        var rs = _runState.Dungeon.GetOrCreateRoom(roomId);
        rs.IsCleared = room.IsCleared();
        foreach (var entity in CollectPersistentEntities(room))
        {
            if (string.IsNullOrEmpty(entity.EntityId))
            {
                GD.PushWarning($"DungeonRoot: persistent entity in room '{roomId}' has no EntityId — state will not be saved");
                continue;
            }
            var captured = entity.CaptureState();
            if (captured != null) rs.Entities[entity.EntityId] = captured;
        }
    }

    private void RestoreRoom(string roomId, RoomController room)
    {
        if (!_runState.Dungeon.Rooms.TryGetValue(roomId, out var rs)) return;

        var matchedIds = new HashSet<string>();
        foreach (var entity in CollectPersistentEntities(room))
        {
            if (string.IsNullOrEmpty(entity.EntityId)) continue;
            if (rs.Entities.TryGetValue(entity.EntityId, out var state))
            {
                entity.RestoreState(state);
                matchedIds.Add(entity.EntityId);
            }
        }

        // Orphan pass: any saved entries with no matching node in the freshly
        // instantiated scene. For dynamic pickups (no .tscn placement) this is
        // how they reappear after a room re-entry — re-spawn at the saved
        // position with no kick. Collected dynamic entries are skipped (they
        // shouldn't reappear), and we leave the entry in Entities so future
        // snapshots don't churn — the dictionary is room-scoped and small.
        foreach (var (id, state) in rs.Entities)
        {
            if (matchedIds.Contains(id)) continue;
            if (state is not PickupState p) continue;
            if (p.Collected) continue;
            var pos = new Vector2(p.PositionX, p.PositionY);
            switch (p.ItemKey)
            {
                case CreditPickupNode.ItemKey:
                    CreditPickupNode.Restore(room, pos, p.Value, id);
                    break;
                case KeyPickupNode.ItemKey:
                    KeyPickupNode.Restore(room, pos, p.Value, id);
                    break;
            }
        }
    }

    private static IEnumerable<IPersistentEntity> CollectPersistentEntities(Node parent)
    {
        foreach (var child in parent.GetChildren())
        {
            if (child is IPersistentEntity entity) yield return entity;
            if (child is Node n)
                foreach (var grand in CollectPersistentEntities(n))
                    yield return grand;
        }
    }

    private void ConfigureDoors(RoomController room, RoomDescriptor descriptor)
    {
        var doorNodes = new System.Collections.Generic.List<DoorNode>();
        CollectDoors(room, doorNodes);

        foreach (var node in doorNodes)
        {
            if (descriptor.Doors.TryGetValue(node.Direction, out var doorDesc))
            {
                // Pre-seed the door's "already consumed" flag from DungeonState so
                // a KeyLocked door the player crossed earlier this run stays open
                // when they re-enter the room without spending another key.
                bool keyLockConsumed = doorDesc.Type == DoorType.KeyLocked
                    && _runState.Dungeon.IsDoorUnlocked(descriptor.Id, doorDesc.TargetRoomId);
                node.Configure(doorDesc.Type, keyLockConsumed);
                node.Visible = true;
                node.PlayerCrossed += dirInt =>
                {
                    var dir = (CardinalDirection)dirInt;
                    if (!descriptor.Doors.TryGetValue(dir, out var d)) return;
                    // KeyLocked cross consumed a key inside DoorNode.OnBodyEntered
                    // — record the edge so the destination room's matching door
                    // (and any future re-entries) skip the consume step.
                    if (d.Type == DoorType.KeyLocked)
                        _runState.Dungeon.MarkDoorUnlocked(descriptor.Id, d.TargetRoomId);
                    // PlayerCrossed fires from inside Area2D.body_entered, which
                    // runs during the physics-server flush. EnterRoom tears down
                    // the active room and AddChilds a new one — that triggers
                    // _Ready on its DoorNodes, which mutate Monitoring and
                    // CollisionShape2D.Disabled. Both writes throw "Can't change
                    // this state while flushing queries". Defer the room swap
                    // until idle time so physics is no longer mid-step.
                    var targetId = d.TargetRoomId;
                    Callable.From(() => EnterRoom(targetId, dir)).CallDeferred();
                };
            }
            else
            {
                // Door slot exists in the room scene but the descriptor has nothing at that wall — hide it.
                node.Visible = false;
                node.Monitoring = false;
            }
        }
    }

    private static void CollectDoors(Node parent, System.Collections.Generic.List<DoorNode> doors)
    {
        foreach (var child in parent.GetChildren())
        {
            if (child is DoorNode door) doors.Add(door);
            else if (child is Node n) CollectDoors(n, doors);
        }
    }

    private void PositionPlayer(RoomController room, CardinalDirection? fromDirection)
    {
        if (_player == null) return;
        _player.GlobalPosition = FindSpawnPoint(room, fromDirection);
    }

    private Vector2 FindSpawnPoint(RoomController room, CardinalDirection? fromDirection)
    {
        // Crossed in from `fromDirection` of the previous room → arrive at the
        // matching door in the new room (the one on the OPPOSITE wall).
        if (fromDirection.HasValue)
        {
            var arrival = fromDirection.Value.Opposite();
            var doors = new System.Collections.Generic.List<DoorNode>();
            CollectDoors(room, doors);
            foreach (var d in doors)
                if (d.Direction == arrival) return d.GlobalPosition + InsetForDirection(arrival);
        }

        // No fromDirection (initial spawn) → use named "PlayerSpawn" marker if present.
        var marker = room.GetNodeOrNull<Node2D>("PlayerSpawn");
        return marker?.GlobalPosition ?? room.GlobalPosition;
    }

    private static Vector2 InsetForDirection(CardinalDirection d) => d switch
    {
        CardinalDirection.North => new Vector2(0, 64),
        CardinalDirection.South => new Vector2(0, -64),
        CardinalDirection.East => new Vector2(-64, 0),
        CardinalDirection.West => new Vector2(64, 0),
        _ => Vector2.Zero,
    };

    private PackedScene? ResolveScene(string templateName) => templateName switch
    {
        "EntryRoom" => EntryRoomScene,
        "WestHall" => WestHallScene,
        "FarRoom" => FarRoomScene,
        "VaultRoom" => VaultRoomScene,
        "RewardRoom" => RewardRoomScene,
        _ => null,
    };

    // -------- Debug API --------
    // Surfaces used by Scripts/Debug/DebugOverlay.cs. Intentionally narrow:
    // public methods for the actions the console invokes, not blanket access
    // to the internal lifecycle. Deferred so the room swap stays out of any
    // physics-flush window it might be triggered from.

    public IEnumerable<string> AllRoomIds => Layout.Rooms.Select(r => r.Id);

    public void TeleportTo(string roomId)
    {
        if (!Layout.TryGetRoom(roomId, out _))
        {
            GD.PushWarning($"DungeonRoot: room id '{roomId}' not in layout");
            return;
        }
        Callable.From(() => EnterRoom(roomId, fromDirection: null)).CallDeferred();
    }

    public void ClearActiveRoom()
    {
        if (_activeRoom == null) return;
        var room = _activeRoom;
        foreach (var node in GetTree().GetNodesInGroup("enemies"))
        {
            if (node is EnemyController enemy && room.IsAncestorOf(enemy))
                enemy.KillFromDebug();
        }
        if (!room.IsCleared()) room.Clear();
    }

    public void KillAllEnemies()
    {
        foreach (var node in GetTree().GetNodesInGroup("enemies"))
        {
            if (node is EnemyController enemy) enemy.KillFromDebug();
        }
    }

    public bool TrySpawnEnemy(EnemyResource definition, Vector2 globalPosition)
    {
        if (_activeRoom == null) return false;
        if (definition.Scene == null)
        {
            GD.PushWarning($"DungeonRoot: enemy '{definition.Id}' has no Scene set");
            return false;
        }
        var enemy = definition.Scene.Instantiate<EnemyController>();
        enemy.Definition = definition;
        enemy.GlobalPosition = globalPosition;
        // Parent under the active room so the enemy tears down with the room
        // on transition — same lifetime contract EnemySpawner enforces.
        _activeRoom.AddChild(enemy);
        return true;
    }
}
