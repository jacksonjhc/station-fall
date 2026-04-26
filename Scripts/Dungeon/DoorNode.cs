using Godot;
using Stationfall.Core.ProcGen;

namespace Stationfall.Godot.Dungeon;

// One door, one direction. Knows the cardinal it sits on; the parent room owns the layout
// and resolves the destination room id when the player crosses.
public partial class DoorNode : Area2D
{
    [Signal] public delegate void PlayerCrossedEventHandler(int direction);

    [Export(PropertyHint.Enum, "North,East,South,West")]
    public int DirectionInt { get; set; } = (int)CardinalDirection.East;

    [Export] public NodePath SealVisualPath { get; set; } = "Seal";
    [Export] public NodePath OpenVisualPath { get; set; } = "Open";
    [Export] public NodePath RoomControllerPath { get; set; } = "..";
    // Optional: a sibling StaticBody2D's CollisionShape2D that physically blocks when locked.
    [Export] public NodePath SealShapePath { get; set; } = "";

    public CardinalDirection Direction => (CardinalDirection)DirectionInt;
    public DoorType Type { get; set; } = DoorType.Open;
    public bool IsLocked { get; private set; }

    private RoomController? _room;
    private CanvasItem? _sealVisual;
    private CanvasItem? _openVisual;
    private CollisionShape2D? _sealShape;

    public override void _Ready()
    {
        _room = GetNodeOrNull<RoomController>(RoomControllerPath);
        _sealVisual = GetNodeOrNull<CanvasItem>(SealVisualPath);
        _openVisual = GetNodeOrNull<CanvasItem>(OpenVisualPath);
        _sealShape = string.IsNullOrEmpty(SealShapePath) ? null : GetNodeOrNull<CollisionShape2D>(SealShapePath);

        BodyEntered += OnBodyEntered;

        if (_room != null)
        {
            _room.Entered += RefreshLockState;
            _room.Cleared += RefreshLockState;
        }

        // Don't run RefreshLockState until Configure() has set the door type.
        // Otherwise the default Type=Open causes Monitoring=true during the
        // window between AddChild and ConfigureDoors. Godot's physics server
        // queues an overlap query during that window using the player body's
        // pre-teleport position (the body transform sync is deferred), and
        // fires a spurious body_entered against a still-stale position —
        // routing the player straight through doors they aren't near.
        // Default to fully-inert until Configure() arrives.
        Monitoring = false;
        if (_sealVisual != null) _sealVisual.Visible = false;
        if (_openVisual != null) _openVisual.Visible = false;
        if (_sealShape != null) _sealShape.Disabled = true;
    }

    public void Configure(DoorType type)
    {
        Type = type;
        RefreshLockState();
    }

    public void RefreshLockState()
    {
        bool roomCleared = _room?.IsCleared() ?? true;
        IsLocked = Type switch
        {
            DoorType.Open => false,
            DoorType.EnemyLocked => !roomCleared,
            DoorType.KeyLocked => true,         // M4 will add key-lookup
            DoorType.ConditionLocked => true,   // M5+
            DoorType.Secret => true,
            _ => false,
        };
        if (_sealVisual != null) _sealVisual.Visible = IsLocked;
        if (_openVisual != null) _openVisual.Visible = !IsLocked;
        // Both physics writes are deferred. RefreshLockState may be invoked from
        // inside a physics callback (plate BodyEntered → room.Clear → Cleared
        // signal → here, or inside EnterRoom from a door's BodyEntered chain).
        // A synchronous Monitoring=true would register the Area2D with the
        // physics server mid-step, against a body-position snapshot that hasn't
        // yet picked up the player's teleport — Godot then queues a phantom
        // body_entered against the player's *previous* world position, routing
        // them straight through doors they aren't near. Deferring to idle-time
        // lets the body's transform propagate first.
        if (_sealShape != null) _sealShape.SetDeferred(CollisionShape2D.PropertyName.Disabled, !IsLocked);
        SetDeferred(Area2D.PropertyName.Monitoring, !IsLocked);
    }

    // Door shape is 64x64. Anything farther than this from the door center is not
    // actually overlapping — it's a stale physics-server snapshot from a previous
    // step (Godot fires body_entered for newly-registered Area2Ds against the
    // body's pre-teleport position, regardless of where the body has been moved
    // to since). We defensively reject these spurious events.
    private const float MaxValidCrossDistanceSq = 96f * 96f;

    private void OnBodyEntered(Node2D body)
    {
        if (IsLocked) return;
        if (!body.IsInGroup("player")) return;

        // Reject Godot's stale body_entered events. Godot fires body_entered
        // for newly-registered Area2Ds against the body's pre-teleport
        // physics-server snapshot, which on a room transition can be at the
        // *previous* room's matching door (same world coords). If the body
        // isn't actually within our shape, drop the event.
        if ((body.GlobalPosition - GlobalPosition).LengthSquared() > MaxValidCrossDistanceSq) return;

        EmitSignal(SignalName.PlayerCrossed, DirectionInt);
    }
}
