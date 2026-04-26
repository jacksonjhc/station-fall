using Godot;
using Stationfall.Core.Combat;
using Stationfall.Core.Entities;
using Stationfall.Godot.Combat;
using Stationfall.Godot.UI;

namespace Stationfall.Godot.Enemies;

// Static target for M1 combat sandbox. Takes hits, shows damage,
// auto-restores HP after a delay so the sandbox is reusable without restart.
public partial class TrainingDummy : Node2D
{
    [Signal] public delegate void DamagedEventHandler(int amount);

    [Export] public NodePath HurtboxPath { get; set; } = "Hurtbox";
    [Export] public NodePath SpritePath { get; set; } = "Sprite";
    [Export] public int MaxHp { get; set; } = 999;
    [Export] public int Armor { get; set; } = 0;
    [Export] public float HpResetDelaySeconds { get; set; } = 1.5f;
    [Export] public float FlashDurationSeconds { get; set; } = 0.15f;
    [Export] public Color FlashColor { get; set; } = Colors.White;

    public EntityStats Stats { get; private set; } = new(MaxHp: 1, Hp: 1, MoveSpeed: 0, AttackPower: 0, AttackRate: 0, Reach: 0, Luck: 0, Armor: 0);

    private HurtboxComponent? _hurtbox;
    private ColorRect? _spriteRect;
    private Color _baseColor = Colors.White;
    private double _lastDamagedAt = -999;

    public override void _Ready()
    {
        Stats = new EntityStats(MaxHp: MaxHp, Hp: MaxHp, MoveSpeed: 0, AttackPower: 0, AttackRate: 0, Reach: 0, Luck: 0, Armor: Armor);

        _hurtbox = GetNodeOrNull<HurtboxComponent>(HurtboxPath);
        if (_hurtbox != null)
        {
            _hurtbox.Owner2D = this;
            _hurtbox.GetStatsProvider = () => Stats;
            _hurtbox.OnDamage = OnHit;
        }

        _spriteRect = GetNodeOrNull<ColorRect>(SpritePath);
        if (_spriteRect != null) _baseColor = _spriteRect.Color;
    }

    public override void _Process(double delta)
    {
        // Swap the ColorRect's color directly during the flash window — modulate-tinting
        // a red box with red was imperceptible. Pure-white swap pops cleanly.
        if (_spriteRect != null)
        {
            float t = (float)((Time.GetTicksMsec() / 1000.0) - _lastDamagedAt);
            _spriteRect.Color = t < FlashDurationSeconds ? FlashColor : _baseColor;
        }

        if (Stats.Hp < Stats.MaxHp && (Time.GetTicksMsec() / 1000.0) - _lastDamagedAt > HpResetDelaySeconds)
        {
            Stats = Stats.WithHp(Stats.MaxHp);
        }
    }

    private void OnHit(DamageResult result, HitboxComponent source)
    {
        Stats = Stats.ApplyDamage(result);
        _lastDamagedAt = Time.GetTicksMsec() / 1000.0;
        EmitSignal(SignalName.Damaged, result.Amount);
        SpawnDamageNumber(result);
    }

    private void SpawnDamageNumber(DamageResult result)
    {
        int displayed = result.TotalDamage;
        if (displayed <= 0) return;
        var num = new DamageNumber { Position = new Vector2(0, -32) };
        bool heavy = result.Amount >= 2 || result.ArmorBroken;
        AddChild(num);
        num.Show(displayed, heavy);
    }
}
