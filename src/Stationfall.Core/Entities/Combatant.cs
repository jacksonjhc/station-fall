namespace Stationfall.Core.Entities;

public class Combatant
{
    public Guid Id { get; } = Guid.NewGuid();
    public string Name { get; }
    public Stats Stats { get; private set; }
    public bool IsAlive => Stats.IsAlive;

    public Combatant(string name, Stats stats)
    {
        Name = name;
        Stats = stats;
    }

    public void ApplyDamage(int amount) => Stats = Stats.WithHp(Stats.Hp - amount);
    public void ApplyHeal(int amount) => Stats = Stats.WithHp(Stats.Hp + amount);
}
