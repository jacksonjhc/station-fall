namespace Stationfall.Core.Entities;

public record Stats(
    int MaxHp,
    int Hp,
    int Speed,
    int Attack,
    int Defense
)
{
    public bool IsAlive => Hp > 0;
    public Stats WithHp(int hp) => this with { Hp = Math.Clamp(hp, 0, MaxHp) };
}
