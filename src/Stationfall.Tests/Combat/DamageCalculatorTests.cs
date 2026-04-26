using Stationfall.Core.Combat;
using Stationfall.Core.Entities;
using Xunit;

namespace Stationfall.Tests.Combat;

public class DamageCalculatorTests
{
    private static EntityStats Stats(int hp = 5, int maxHp = 5, int armor = 0, int attackPower = 1) =>
        new(MaxHp: maxHp, Hp: hp, MoveSpeed: 120f, AttackPower: attackPower, AttackRate: 1.5f, Reach: 32, Luck: 0, Armor: armor);

    private static ComboStep LightStep() => WeaponDefinition.Sword.StepAt(0); // dmg 1
    private static ComboStep HeavyStep() => WeaponDefinition.Sword.StepAt(2); // dmg 2

    [Fact]
    public void LightSwordHit_AgainstUnarmored_DealsOneHpDamage()
    {
        var result = DamageCalculator.Calculate(Stats(), Stats(), LightStep(), DamageModifiers.None);
        Assert.Equal(1, result.Amount);
        Assert.Equal(0, result.ArmorAbsorbed);
        Assert.False(result.ArmorBroken);
        Assert.False(result.Killed);
    }

    [Fact]
    public void HeavySwordHit_AgainstUnarmored_DealsTwoHpDamage()
    {
        var result = DamageCalculator.Calculate(Stats(), Stats(), HeavyStep(), DamageModifiers.None);
        Assert.Equal(2, result.Amount);
    }

    [Fact]
    public void Armor_AbsorbsDamageFirst_ThenSpillsToHp()
    {
        // 2-damage heavy hit vs 1 armor → 1 absorbed, 1 to HP, armor breaks.
        var result = DamageCalculator.Calculate(Stats(), Stats(armor: 1), HeavyStep(), DamageModifiers.None);
        Assert.Equal(1, result.ArmorAbsorbed);
        Assert.Equal(1, result.Amount);
        Assert.True(result.ArmorBroken);
    }

    [Fact]
    public void Armor_FullyAbsorbsLightHit_NoHpDamage_NoBreak()
    {
        // 1-damage light hit vs 4 armor → 1 absorbed, 0 to HP, armor still has 3.
        var result = DamageCalculator.Calculate(Stats(), Stats(armor: 4), LightStep(), DamageModifiers.None);
        Assert.Equal(1, result.ArmorAbsorbed);
        Assert.Equal(0, result.Amount);
        Assert.False(result.ArmorBroken);
    }

    [Fact]
    public void IgnoreArmor_BypassesArmorEntirely()
    {
        var result = DamageCalculator.Calculate(Stats(), Stats(armor: 4), HeavyStep(), DamageModifiers.None with { IgnoreArmor = true });
        Assert.Equal(0, result.ArmorAbsorbed);
        Assert.Equal(2, result.Amount);
    }

    [Fact]
    public void Multiplier_ScalesBaseDamage()
    {
        // Light step (dmg 1) × 1.5 multiplier → rounds to 2.
        var result = DamageCalculator.Calculate(Stats(), Stats(), LightStep(), DamageModifiers.None with { Multiplier = 1.5f });
        Assert.Equal(2, result.Amount);
    }

    [Fact]
    public void FlatBonus_AddedAfterMultiplier()
    {
        // Light (dmg 1) × 1.0 + 2 flat = 3.
        var result = DamageCalculator.Calculate(Stats(), Stats(), LightStep(), DamageModifiers.None with { FlatBonus = 2 });
        Assert.Equal(3, result.Amount);
    }

    [Fact]
    public void AttackPower_MultipliesStepDamage()
    {
        // Heavy (dmg 2) × AP 2 = 4.
        var result = DamageCalculator.Calculate(Stats(attackPower: 2), Stats(), HeavyStep(), DamageModifiers.None);
        Assert.Equal(4, result.Amount);
    }

    [Fact]
    public void Killed_TrueWhenHpDropsToZero()
    {
        var result = DamageCalculator.Calculate(Stats(), Stats(hp: 1, maxHp: 5), LightStep(), DamageModifiers.None);
        Assert.True(result.Killed);
    }

    [Fact]
    public void Killed_TrueWhenOverkill()
    {
        var result = DamageCalculator.Calculate(Stats(), Stats(hp: 1, maxHp: 5), HeavyStep(), DamageModifiers.None);
        Assert.True(result.Killed);
    }

    [Fact]
    public void Killed_FalseWhenAbsorbedFully_EvenAtOneHp()
    {
        // 2 damage vs 4 armor: nothing reaches HP, no kill.
        var result = DamageCalculator.Calculate(Stats(), Stats(hp: 1, maxHp: 5, armor: 4), HeavyStep(), DamageModifiers.None);
        Assert.False(result.Killed);
        Assert.Equal(0, result.Amount);
    }

    [Fact]
    public void Killed_FalseWhenAlreadyDead()
    {
        var result = DamageCalculator.Calculate(Stats(), Stats(hp: 0, maxHp: 5), HeavyStep(), DamageModifiers.None);
        Assert.False(result.Killed);
    }

    [Fact]
    public void ApplyDamage_ReducesHpAndArmor()
    {
        var defender = Stats(hp: 5, maxHp: 5, armor: 2);
        var result = DamageCalculator.Calculate(Stats(), defender, HeavyStep(), DamageModifiers.None);
        var after = defender.ApplyDamage(result);
        Assert.Equal(5, after.Hp); // 2 dmg fully absorbed by 2 armor
        Assert.Equal(0, after.Armor);
        Assert.True(result.ArmorBroken);
    }

    [Fact]
    public void ApplyDamage_DoesNotPushHpBelowZero()
    {
        var defender = Stats(hp: 1, maxHp: 5);
        var result = DamageCalculator.Calculate(Stats(), defender, HeavyStep(), DamageModifiers.None);
        var after = defender.ApplyDamage(result);
        Assert.Equal(0, after.Hp);
    }
}
