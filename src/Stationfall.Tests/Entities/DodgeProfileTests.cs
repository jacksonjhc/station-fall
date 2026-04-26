using Stationfall.Core.Entities;
using Xunit;

namespace Stationfall.Tests.Entities;

public class DodgeProfileTests
{
    [Fact]
    public void RollProfile_MatchesW2Spec()
    {
        var roll = DodgeProfile.Roll;
        Assert.Equal(DodgeKind.Roll, roll.Kind);
        Assert.Equal(1, roll.StartupFrames);
        Assert.Equal(8, roll.IFrameDurationFrames);
        Assert.Equal(3, roll.RecoveryFrames);
        Assert.Equal(12, roll.TotalFrames);
        Assert.Equal(96f, roll.DistancePixels);
        Assert.Equal(0.6f, roll.RechargeSeconds);
        Assert.Equal(0, roll.ContactDamage);
    }

    [Fact]
    public void ShoulderChargeProfile_MatchesW2Spec()
    {
        var sc = DodgeProfile.ShoulderCharge;
        Assert.Equal(DodgeKind.ShoulderCharge, sc.Kind);
        Assert.Equal(2, sc.StartupFrames);
        Assert.Equal(10, sc.IFrameDurationFrames);
        Assert.Equal(4, sc.RecoveryFrames);
        Assert.Equal(16, sc.TotalFrames);
        Assert.Equal(128f, sc.DistancePixels);
        Assert.Equal(1.0f, sc.RechargeSeconds);
        Assert.Equal(1, sc.ContactDamage);
        Assert.True(sc.KnockbackOnContact);
    }

    [Fact]
    public void HasIFrames_DuringIFrameWindow_OnlyTrueInsideRange()
    {
        var roll = DodgeProfile.Roll;
        Assert.False(roll.HasIFramesAt(1));   // startup
        Assert.True(roll.HasIFramesAt(2));    // first i-frame
        Assert.True(roll.HasIFramesAt(9));    // last i-frame
        Assert.False(roll.HasIFramesAt(10));  // first recovery (punish window)
        Assert.False(roll.HasIFramesAt(12));  // last recovery
    }

    [Fact]
    public void RollProfile_LastThreeRecoveryFramesHaveNoIFrames()
    {
        // W2 punish-window invariant: i-frame chaining cannot cover the entire dodge.
        var roll = DodgeProfile.Roll;
        for (int frame = roll.TotalFrames - roll.RecoveryFrames + 1; frame <= roll.TotalFrames; frame++)
        {
            Assert.False(roll.HasIFramesAt(frame), $"frame {frame} should be punishable");
        }
    }
}
