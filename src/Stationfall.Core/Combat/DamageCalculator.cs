using Stationfall.Core.Entities;

namespace Stationfall.Core.Combat;

public static class DamageCalculator
{
    public static DamageResult Calculate(
        EntityStats attacker,
        EntityStats defender,
        ComboStep step,
        DamageModifiers modifiers)
    {
        int attackPower = Math.Max(0, attacker.AttackPower);
        float scaled = step.Damage * attackPower * Math.Max(0f, modifiers.Multiplier);
        int rawDamage = (int)Math.Round(scaled) + modifiers.FlatBonus;
        rawDamage = Math.Max(0, rawDamage);

        int armorAbsorbed = 0;
        int hpDamage = rawDamage;
        bool armorBroken = false;

        if (!modifiers.IgnoreArmor && defender.Armor > 0 && rawDamage > 0)
        {
            armorAbsorbed = Math.Min(defender.Armor, rawDamage);
            hpDamage = rawDamage - armorAbsorbed;
            armorBroken = (defender.Armor - armorAbsorbed) == 0;
        }

        bool killed = defender.Hp > 0 && (defender.Hp - hpDamage) <= 0;

        return new DamageResult(
            Amount: hpDamage,
            ArmorAbsorbed: armorAbsorbed,
            ArmorBroken: armorBroken,
            Killed: killed
        );
    }
}
