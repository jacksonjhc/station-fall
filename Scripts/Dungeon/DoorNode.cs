using Godot;
using Stationfall.Core.ProcGen;
using Stationfall.Godot.Audio;
using Stationfall.Godot.Items;

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

    // Once a KeyLocked door has been consumed (either now, or earlier this run
    // when crossed from the other side and persisted in DungeonState), it stays
    // open without re-charging the player. The flag tracks the local copy of
    // that "edge unlocked" state — DungeonRoot pre-seeds it via Configure().
    public bool KeyLockConsumed { get; private set; }

    private RoomController? _room;
    private CanvasItem? _sealVisual;
    private CanvasItem? _openVisual;
    private CollisionShape2D? _sealShape;
    private bool _keysSubscribed;
    private bool _armedPulseActive;
    private double _armedPulseTime;

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

    // Called when the room scene has a door slot at this direction but the
    // layout descriptor doesn't list one — i.e. a 4-door template placed where
    // only some walls have neighbours. Visuals off, no body_entered events,
    // and the wall-plug collider engaged so the player can't walk through the
    // missing door's gap into nothing.
    public void HideAndSeal()
    {
        Visible = false;
        SetDeferred(Area2D.PropertyName.Monitoring, false);
        if (_sealVisual != null) _sealVisual.Visible = false;
        if (_openVisual != null) _openVisual.Visible = false;
        if (_sealShape != null)
            _sealShape.SetDeferred(CollisionShape2D.PropertyName.Disabled, false);
    }

    public void Configure(DoorType type, bool keyLockConsumed = false)
    {
        Type = type;
        KeyLockConsumed = keyLockConsumed;

        // Subscribe to key-count changes once. Pickup grants a key → all
        // KeyLocked doors in this room re-evaluate their seal state without
        // the player having to re-enter the room. Defensive null-check
        // because the service binds during DungeonRoot._Ready, which runs
        // after this Configure on the entry room.
        if (Type == DoorType.KeyLocked && !_keysSubscribed && KeysService.Instance != null)
        {
            KeysService.Instance.CountChanged += OnKeyCountChanged;
            _keysSubscribed = true;
        }

        RefreshLockState();
    }

    public override void _ExitTree()
    {
        if (_keysSubscribed && KeysService.Instance != null)
        {
            KeysService.Instance.CountChanged -= OnKeyCountChanged;
            _keysSubscribed = false;
        }
    }

    private void OnKeyCountChanged(int _) => RefreshLockState();

    public override void _Process(double delta)
    {
        if (!_armedPulseActive || _sealVisual == null) return;
        // 1.5 Hz ping-pong between dim and bright. Sin → [-1, 1], remapped to
        // a 0.85 ↔ 1.7 modulate range so the swing reads as obvious pulsing
        // even on the small 64×64 seal. Faster than a heartbeat, slow enough
        // that one cycle resolves before the player hits the door.
        _armedPulseTime += delta;
        float t = (float)Math.Sin(_armedPulseTime * Math.PI * 1.5);
        float amp = 1.275f + 0.425f * t;
        _sealVisual.Modulate = new Color(amp, amp, amp * 0.9f);
    }

    public void RefreshLockState()
    {
        bool roomCleared = _room?.IsCleared() ?? true;
        // KeyLocked: physically locked while no key + not yet consumed. Once
        // consumed, sticky-open both ways for the rest of the run (W7 reset
        // rule).
        bool hasKey = (KeysService.Instance?.Count ?? 0) > 0;
        IsLocked = Type switch
        {
            DoorType.Open => false,
            DoorType.EnemyLocked => !roomCleared,
            DoorType.KeyLocked => !KeyLockConsumed && !hasKey,
            DoorType.ConditionLocked => true,   // M5+
            DoorType.Secret => true,
            _ => false,
        };

        // Visual seal decouples from the physical block for KeyLocked: the
        // yellow seal stays up until the key is actually spent, even if the
        // player is already carrying one. Otherwise the door reads as Open
        // the moment a key drops, the player walks through, the key vanishes
        // silently, and the only feedback is the HUD counter ticking down.
        // Brightening the seal when the player has a key (`armed`) gives a
        // pre-cross "this will cost you a key" tell distinct from the
        // "you don't have one" state.
        bool sealVisible = Type == DoorType.KeyLocked
            ? !KeyLockConsumed
            : IsLocked;
        bool armed = Type == DoorType.KeyLocked && hasKey && !KeyLockConsumed;

        if (_sealVisual != null) _sealVisual.Visible = sealVisible;
        if (_openVisual != null) _openVisual.Visible = !sealVisible;

        // _Process drives the modulate when armed so the player gets a clearly
        // animated "this door is interactable now" tell — a static brighten
        // gets lost against the surrounding yellow seal. When not armed, lock
        // back to neutral and stop ticking.
        _armedPulseActive = armed;
        if (!armed && _sealVisual != null) _sealVisual.Modulate = Colors.White;
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

        // Consume one generic key on first cross of a KeyLocked door. Once
        // consumed, the door is sticky-open: both this door and its mirror on
        // the destination side stay unlocked for the rest of the run via the
        // DungeonState edge entry that DungeonRoot writes from PlayerCrossed.
        if (Type == DoorType.KeyLocked && !KeyLockConsumed)
        {
            // RefreshLockState already gated us behind hasKey, so TryConsume
            // should succeed. Defensive: if a concurrent consume drained the
            // pouch (another door this same physics step), bail and leave the
            // player blocked.
            if (KeysService.Instance == null || !KeysService.Instance.TryConsume(1)) return;
            KeyLockConsumed = true;
            // Audible feedback at the consume moment. Sound persists across
            // the deferred room transition that fires immediately after, which
            // makes it the most reliable cue — the visual seal-hide and HUD
            // tick both happen in the ~16ms before the new room loads.
            Sfx.Instance?.PlayDoorUnlock();
            // RefreshLockState would have re-locked us when CountChanged fired
            // (if hasKey is now 0); flip back to unlocked since this door is
            // permanently consumed.
            RefreshLockState();
        }

        EmitSignal(SignalName.PlayerCrossed, DirectionInt);
    }
}
