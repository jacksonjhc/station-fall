using Godot;
using Stationfall.Core.Combat;
using Stationfall.Core.Entities;

namespace Stationfall.Godot.Combat;

public partial class HurtboxComponent : Area2D
{
    [Signal] public delegate void HitTakenEventHandler(int amount, int armorAbsorbed, bool armorBroken, bool killed);

    public Node2D? Owner2D { get; set; }

    public delegate EntityStats StatsProvider();
    public delegate void DamageHandler(DamageResult result, HitboxComponent source);

    public StatsProvider? GetStatsProvider { get; set; }
    public DamageHandler? OnDamage { get; set; }

    private Polygon2D? _debugViz;
    // Hurtbox tinted cyan — pairs with the red hitbox color in the debug overlay.
    private static readonly Color DebugColor = new(0.3f, 0.9f, 1f, 0.30f);

    public override void _Ready()
    {
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

    public EntityStats GetCurrentStats() =>
        GetStatsProvider?.Invoke() ?? new EntityStats(MaxHp: 1, Hp: 1, MoveSpeed: 0, AttackPower: 0, AttackRate: 0, Reach: 0, Luck: 0, Armor: 0);

    public void ReceiveHit(DamageResult result, HitboxComponent source)
    {
        OnDamage?.Invoke(result, source);
        EmitSignal(SignalName.HitTaken, result.Amount, result.ArmorAbsorbed, result.ArmorBroken, result.Killed);
    }
}
