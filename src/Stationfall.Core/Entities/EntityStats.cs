namespace Stationfall.Core.Entities;

public record EntityStats(
    int MaxHp,
    int Hp,
    float MoveSpeed,
    int AttackPower,
    float AttackRate,
    int Reach,
    int Luck,
    int Armor
)
{
    public bool IsAlive => Hp > 0;

    public EntityStats WithHp(int hp) => this with { Hp = Math.Clamp(hp, 0, MaxHp) };
    public EntityStats WithArmor(int armor) => this with { Armor = Math.Max(0, armor) };
    public EntityStats WithMaxHp(int maxHp) => this with { MaxHp = Math.Max(0, maxHp), Hp = Math.Clamp(Hp, 0, Math.Max(0, maxHp)) };

    public float HpRatio => MaxHp <= 0 ? 0f : (float)Hp / MaxHp;
}
