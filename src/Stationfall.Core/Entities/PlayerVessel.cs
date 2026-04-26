using Stationfall.Core.Combat;

namespace Stationfall.Core.Entities;

public record PlayerVessel(
    string Name,
    EntityStats BaseStats,
    DodgeProfile DodgeProfile,
    WeaponDefinition Weapon,
    AdrenalineRushConfig SignatureConfig
)
{
    public static PlayerVessel CreateClone() => new(
        Name: "Clone",
        BaseStats: new EntityStats(
            MaxHp: 5,
            Hp: 5,
            MoveSpeed: 200f,
            AttackPower: 1,
            AttackRate: 1.5f,
            Reach: 32,
            Luck: 0,
            Armor: 0),
        DodgeProfile: DodgeProfile.Roll,
        Weapon: WeaponDefinition.Sword,
        SignatureConfig: AdrenalineRushConfig.Default
    );
}
