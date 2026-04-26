using Stationfall.Core.Combat;
using Stationfall.Core.Entities;
using Xunit;

namespace Stationfall.Tests.Entities;

public class PlayerVesselTests
{
    [Fact]
    public void Clone_BaselineStats_MatchW1Spec()
    {
        var clone = PlayerVessel.CreateClone();
        Assert.Equal("Clone", clone.Name);
        var s = clone.BaseStats;
        Assert.Equal(5, s.MaxHp);
        Assert.Equal(5, s.Hp);
        Assert.Equal(200f, s.MoveSpeed);
        Assert.Equal(1, s.AttackPower);
        Assert.Equal(1.5f, s.AttackRate);
        Assert.Equal(32, s.Reach);
        Assert.Equal(0, s.Luck);
        Assert.Equal(0, s.Armor);
    }

    [Fact]
    public void Clone_UsesRollDodge_AndSwordDefault()
    {
        var clone = PlayerVessel.CreateClone();
        Assert.Equal(DodgeKind.Roll, clone.DodgeProfile.Kind);
        Assert.Equal("Sword", clone.Weapon.Name);
    }

    [Fact]
    public void Clone_SignatureUsesDefaultAdrenalineConfig()
    {
        var clone = PlayerVessel.CreateClone();
        Assert.Equal(AdrenalineRushConfig.Default, clone.SignatureConfig);
    }
}
