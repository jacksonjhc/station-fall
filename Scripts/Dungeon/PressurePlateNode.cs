using Godot;
using Stationfall.Core.Runs;
using Stationfall.Godot.Persistence;

namespace Stationfall.Godot.Dungeon;

// M2 stand-in clear-condition prop. Stand on it → triggers RoomController.Clear().
// State (whether it's been pressed) persists across room teardowns via DungeonRoot.
public partial class PressurePlateNode : Area2D, IPersistentEntity
{
    [Signal] public delegate void DepressedEventHandler();

    [Export] public string EntityId { get; set; } = "";
    [Export] public NodePath RoomControllerPath { get; set; } = "..";
    [Export] public NodePath PadVisualPath { get; set; } = "Pad";
    [Export] public Color RestColor { get; set; } = new Color(0.55f, 0.55f, 0.20f);
    [Export] public Color PressedColor { get; set; } = new Color(0.30f, 0.85f, 0.30f);
    [Export] public bool OneShot { get; set; } = true;

    private RoomController? _room;
    private ColorRect? _pad;
    private bool _triggered;

    public override void _Ready()
    {
        _room = GetNodeOrNull<RoomController>(RoomControllerPath);
        _pad = GetNodeOrNull<ColorRect>(PadVisualPath);
        if (_pad != null) _pad.Color = RestColor;
        BodyEntered += OnBodyEntered;
    }

    private void OnBodyEntered(Node2D body)
    {
        if (_triggered && OneShot) return;
        if (!body.IsInGroup("player")) return;
        _triggered = true;
        if (_pad != null) _pad.Color = PressedColor;
        _room?.Clear();
        EmitSignal(SignalName.Depressed);
    }

    public EntityState? CaptureState() => new PressurePlateState(_triggered);

    public void RestoreState(EntityState state)
    {
        if (state is not PressurePlateState plate) return;
        _triggered = plate.Triggered;
        if (_pad != null) _pad.Color = _triggered ? PressedColor : RestColor;
    }
}
