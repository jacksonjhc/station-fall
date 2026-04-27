using Godot;
using Stationfall.Core.Combat;
using Stationfall.Core.Entities;
using Stationfall.Core.Runs;
using Stationfall.Godot.Combat;
using Stationfall.Godot.Persistence;

namespace Stationfall.Godot.Dungeon;

// Static target that takes player melee damage and despawns.
// HP and destroyed-state persist across room teardowns via DungeonRoot.
public partial class BreakableCrateNode : Node2D, IPersistentEntity
{
    [Signal] public delegate void DestroyedEventHandler();

    [Export] public string EntityId { get; set; } = "";
    [Export] public NodePath HurtboxPath { get; set; } = "Hurtbox";
    [Export] public NodePath SpritePath { get; set; } = "Sprite";
    [Export] public int Hp { get; set; } = 2;
    [Export] public int Armor { get; set; } = 0;

    public EntityStats Stats { get; private set; } = new(MaxHp: 1, Hp: 1, MoveSpeed: 0, AttackPower: 0, AttackRate: 0, Reach: 0, Luck: 0, Armor: 0);

    private HurtboxComponent? _hurtbox;
    private bool _destroyed;

    public override void _Ready()
    {
        Stats = new EntityStats(MaxHp: Hp, Hp: Hp, MoveSpeed: 0, AttackPower: 0, AttackRate: 0, Reach: 0, Luck: 0, Armor: Armor);

        _hurtbox = GetNodeOrNull<HurtboxComponent>(HurtboxPath);
        if (_hurtbox != null)
        {
            _hurtbox.Owner2D = this;
            _hurtbox.GetStatsProvider = () => Stats;
            _hurtbox.OnDamage = OnHit;
        }
    }

    private void OnHit(DamageResult result, HitboxComponent source)
    {
        if (_destroyed) return;
        Stats = Stats.ApplyDamage(result);
        if (Stats.Hp <= 0)
        {
            _destroyed = true;
            EmitSignal(SignalName.Destroyed);
            ApplyDestroyedAppearance();
        }
    }

    public EntityState? CaptureState() => new BreakableCrateState(Stats.Hp, _destroyed);

    public void RestoreState(EntityState state)
    {
        if (state is not BreakableCrateState s) return;
        if (s.Destroyed)
        {
            _destroyed = true;
            ApplyDestroyedAppearance();
            return;
        }
        Stats = Stats.WithHp(s.Hp);
    }

    // Hide the crate and disable its hurtbox + body collision instead of QueueFree-ing.
    // The room owns this node's lifetime: when the player leaves, the room's QueueFree
    // tears the crate down with it. Surviving until then lets SnapshotRoom capture the
    // destroyed state — QueueFree-ing immediately would have the node gone from the
    // scene tree by the next room exit, causing CaptureState to never run for it.
    private void ApplyDestroyedAppearance()
    {
        Visible = false;
        // ApplyDestroyedAppearance fires from OnHit, which is invoked inside
        // HitboxComponent.OnAreaEntered — physics flush territory. Monitorable
        // toggles must defer for the same reason as Monitoring / Disabled.
        if (_hurtbox != null) _hurtbox.SetDeferred(Area2D.PropertyName.Monitorable, false);
        // Also disable any solid body so the player can walk through.
        var body = GetNodeOrNull<StaticBody2D>("Body");
        if (body != null)
        {
            var shape = body.GetNodeOrNull<CollisionShape2D>("BodyShape");
            if (shape != null) shape.SetDeferred(CollisionShape2D.PropertyName.Disabled, true);
        }
    }
}
