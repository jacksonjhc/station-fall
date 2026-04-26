using Stationfall.Core.Combat;
using Xunit;

namespace Stationfall.Tests.Combat;

public class WeaponDefinitionTests
{
    [Fact]
    public void Sword_Has3StepCombo_LLH()
    {
        var sword = WeaponDefinition.Sword;
        Assert.Equal(3, sword.ComboLength);
        Assert.False(sword.StepAt(0).IsHeavy);
        Assert.False(sword.StepAt(1).IsHeavy);
        Assert.True(sword.StepAt(2).IsHeavy);
    }

    [Theory]
    [InlineData(0, 4, 3, 8,  1, 60,  30)]
    [InlineData(1, 4, 3, 8,  1, 60,  30)]
    [InlineData(2, 8, 4, 14, 2, 120, 60)]
    public void Sword_StepFramesAndDamage_MatchW2Spec(int index, int windup, int active, int recovery, int damage, int hsTarget, int hsAttacker)
    {
        var step = WeaponDefinition.Sword.StepAt(index);
        Assert.Equal(windup, step.WindupFrames);
        Assert.Equal(active, step.ActiveFrames);
        Assert.Equal(recovery, step.RecoveryFrames);
        Assert.Equal(damage, step.Damage);
        Assert.Equal(hsTarget, step.HitstopTargetMs);
        Assert.Equal(hsAttacker, step.HitstopAttackerMs);
    }

    [Fact]
    public void Sword_AttackerHitstop_IsHalfTargetHitstop_OnEveryStep()
    {
        // W2 invariant: hit-stop applies to both, target frozen 2× longer than attacker.
        foreach (var step in WeaponDefinition.Sword.ComboSteps)
        {
            Assert.Equal(step.HitstopTargetMs / 2, step.HitstopAttackerMs);
        }
    }

    [Fact]
    public void Sword_ReachAndArc_MatchW2Spec()
    {
        var sword = WeaponDefinition.Sword;
        Assert.Equal(WeaponReach.Medium, sword.Reach);
        Assert.Equal(90, sword.ArcDegrees);
        Assert.Equal("clean", sword.Adjective);
    }

    [Fact]
    public void CancelWindow_Is6Frames()
    {
        // W2: 6-frame cancel window at start of recovery.
        Assert.Equal(6, ComboStep.CancelWindowFrames);
    }
}
