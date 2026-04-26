namespace Stationfall.Core.Combat;

public record DamageResult(
    int Amount,
    int ArmorAbsorbed,
    bool ArmorBroken,
    bool Killed
)
{
    public static DamageResult None => new(0, 0, false, false);

    public int TotalDamage => Amount + ArmorAbsorbed;
}
