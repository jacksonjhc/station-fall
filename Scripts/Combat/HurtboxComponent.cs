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

    public EntityStats GetCurrentStats() =>
        GetStatsProvider?.Invoke() ?? new EntityStats(MaxHp: 1, Hp: 1, MoveSpeed: 0, AttackPower: 0, AttackRate: 0, Reach: 0, Luck: 0, Armor: 0);

    public void ReceiveHit(DamageResult result, HitboxComponent source)
    {
        OnDamage?.Invoke(result, source);
        EmitSignal(SignalName.HitTaken, result.Amount, result.ArmorAbsorbed, result.ArmorBroken, result.Killed);
    }
}
