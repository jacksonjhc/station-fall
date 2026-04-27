using Godot;
using Stationfall.Core.Combat;
using Stationfall.Core.Entities;
using Stationfall.Godot.Audio;

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
    private Polygon2D? _debugViz;
    // Hitbox tinted red — distinguishes attack volumes from hurtboxes (cyan).
    private static readonly Color DebugColor = new(1f, 0.3f, 0.3f, 0.35f);

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

        AddToGroup(CombatAreaDebug.Group);
        if (CombatAreaDebug.DefaultVisible) SetDebugVisible(true);
    }

    public void SetDebugVisible(bool visible)
    {
        if (visible && _debugViz == null)
        {
            _debugViz = CombatAreaDebug.BuildPolygon(this, DebugColor);
            if (_debugViz != null) AddChild(_debugViz);
        }
        if (_debugViz != null) _debugViz.Visible = visible;
    }

    public void SetActive(bool active)
    {
        if (_active == active) return;
        _active = active;
        // Defer physics-state writes. SetActive(false) is typically called from
        // inside area_entered (HitLanded → PlayerController.DeactivateHitbox)
        // while the physics server is flushing queries, which forbids synchronous
        // Monitoring / Disabled mutation. Visual writes stay synchronous so the
        // swing graphic clears on the same frame the hit registers.
        SetDeferred(Area2D.PropertyName.Monitoring, active);
        if (_shape != null) _shape.SetDeferred(CollisionShape2D.PropertyName.Disabled, !active);
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

        // Hit-stop. Per W2: target freeze is 2× attacker freeze; both are
        // configurable on ComboStep. Owners implement IFreezable.
        ApplyHitStop(Owner2D, CurrentStep.HitstopAttackerMs);
        ApplyHitStop(hurtbox.Owner2D, CurrentStep.HitstopTargetMs);

        // Particles + sfx. Direction = attacker → victim so sparks fly through
        // the impact. Spawn at the hurtbox center (or fall back to hurtbox area
        // origin if Owner2D is missing). Hit-landed sfx fires only when the
        // attacker is the player — enemy hits route through PlayerController
        // .TakeDamage which plays its own damage-taken cue, and we don't want
        // both stacked.
        var hitPosition = hurtbox.Owner2D?.GlobalPosition ?? hurtbox.GlobalPosition;
        var direction = ComputeImpactDirection(hurtbox);
        bool attackerIsPlayer = Owner2D?.IsInGroup("player") == true;
        var burstKind = CurrentStep.IsHeavy ? HitBurstPool.BurstKind.HitHeavy : HitBurstPool.BurstKind.HitLight;
        HitBurstPool.Instance?.Burst(hitPosition, direction, burstKind);
        if (attackerIsPlayer) Sfx.Instance?.PlayHitLanded(CurrentStep.IsHeavy);

        var target = hurtbox.Owner2D ?? hurtbox;
        EmitSignal(SignalName.HitLanded, target, result.Amount, result.ArmorBroken, result.Killed);
    }

    private Vector2 ComputeImpactDirection(HurtboxComponent hurtbox)
    {
        // Prefer the actual attacker → victim line. Falls back to the hitbox's
        // forward direction (encoded in Position relative to attacker) when
        // either owner is missing — mostly for tests and synthetic hits.
        if (Owner2D != null && hurtbox.Owner2D != null)
        {
            var to = hurtbox.Owner2D.GlobalPosition - Owner2D.GlobalPosition;
            if (to.LengthSquared() > 0.001f) return to.Normalized();
        }
        return Position.LengthSquared() > 0.001f ? Position.Normalized() : Vector2.Right;
    }

    private static void ApplyHitStop(Node2D? owner, int milliseconds)
    {
        if (milliseconds <= 0) return;
        if (owner is IFreezable freezable) freezable.Freeze(milliseconds / 1000.0);
    }
}
