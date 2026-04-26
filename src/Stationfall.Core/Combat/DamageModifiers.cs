namespace Stationfall.Core.Combat;

public record DamageModifiers(
    float Multiplier = 1.0f,
    int FlatBonus = 0,
    bool IgnoreArmor = false
)
{
    public static DamageModifiers None { get; } = new();
}
