using Godot;
using Stationfall.Core.Combat;
using Stationfall.Core.Entities;

namespace Stationfall.Godot.Combat;

public partial class HitboxComponent : Area2D
{
    [Signal] public delegate void HitLandedEventHandler(Node2D target, int amount, bool armorBroken, bool killed);

    [Export] public NodePath ShapePath { get; set; } = "Shape";
    [Export] public NodePath VisualPath { get; set; } = "Visual";

    public Node2D? Owner2D { get; set; }
    public EntityStats AttackerStats { get; set; } = new(MaxHp: 1, Hp: 1, MoveSpeed: 0, AttackPower: 1, AttackRate: 1, Reach: 0, Luck: 0, Armor: 0);
    public ComboStep? CurrentStep { get; private set; }
    public DamageModifiers Modifiers { get; set; } = DamageModifiers.None;

    private bool _active;
    private CollisionShape2D? _shape;
    private CanvasItem? _visual;
    private float _forwardOffset;
    private readonly System.Collections.Generic.HashSet<HurtboxComponent> _alreadyHit = new();

    public override void _Ready()
    {
        _shape = GetNodeOrNull<CollisionShape2D>(ShapePath);
        _visual = GetNodeOrNull<CanvasItem>(VisualPath);
        // Scene-defined offset is "how far in front of the owner" the swing should land.
        // We rotate this offset around the owner each time SetFacing is called.
        _forwardOffset = Position.Length();
        Monitoring = false;
        if (_shape != null) _shape.Disabled = true;
        if (_visual != null) _visual.Visible = false;
        AreaEntered += OnAreaEntered;
    }

    public void SetActive(bool active)
    {
        if (_active == active) return;
        _active = active;
        Monitoring = active;
        if (_shape != null) _shape.Disabled = !active;
        if (_visual != null) _visual.Visible = active;
        if (active) _alreadyHit.Clear();
    }

    public void SetCurrentStep(ComboStep step) => CurrentStep = step;

    public void SetFacing(Vector2 facing)
    {
        if (facing.LengthSquared() < 0.001f) return;
        var dir = facing.Normalized();
        Position = dir * _forwardOffset;
        Rotation = dir.Angle();
    }

    private void OnAreaEntered(Area2D area)
    {
        if (!_active) return;
        if (area is not HurtboxComponent hurtbox) return;
        if (_alreadyHit.Contains(hurtbox)) return;
        if (CurrentStep == null) return;

        _alreadyHit.Add(hurtbox);
        var defenderStats = hurtbox.GetCurrentStats();
        var result = DamageCalculator.Calculate(AttackerStats, defenderStats, CurrentStep, Modifiers);
        hurtbox.ReceiveHit(result, this);
        var target = hurtbox.Owner2D ?? hurtbox;
        EmitSignal(SignalName.HitLanded, target, result.Amount, result.ArmorBroken, result.Killed);
    }
}
