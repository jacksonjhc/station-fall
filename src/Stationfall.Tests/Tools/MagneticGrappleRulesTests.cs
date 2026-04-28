using Stationfall.Core.Entities;
using Stationfall.Core.Tools;
using Xunit;

namespace Stationfall.Tests.Tools;

public class MagneticGrappleRulesTests
{
    private static readonly MagneticGrappleConfig Cfg = MagneticGrappleConfig.Default;

    // --- ResolveOutcome: per-MassClass outcomes from PLANNING.md ---

    [Fact]
    public void Light_PullsEnemyToPlayer_WithStagger()
    {
        var o = MagneticGrappleRules.ResolveOutcome(MassClass.Light, Cfg);
        Assert.Equal(GrappleOutcomeKind.PullEnemyToPlayer, o.Kind);
        Assert.Equal(Cfg.LightStaggerFrames, o.StaggerFrames);
        Assert.Equal(8, o.StaggerFrames); // PLANNING default
    }

    [Fact]
    public void Medium_SplitPullsToMidpoint_WithNoStagger()
    {
        var o = MagneticGrappleRules.ResolveOutcome(MassClass.Medium, Cfg);
        Assert.Equal(GrappleOutcomeKind.SplitPullToMidpoint, o.Kind);
        Assert.Equal(0, o.StaggerFrames);
    }

    [Fact]
    public void Heavy_PullsPlayerToTarget_WithNoStagger()
    {
        var o = MagneticGrappleRules.ResolveOutcome(MassClass.Heavy, Cfg);
        Assert.Equal(GrappleOutcomeKind.PullPlayerToTarget, o.Kind);
        Assert.Equal(0, o.StaggerFrames);
    }

    [Fact]
    public void Boss_HasNoEffectByDefault()
    {
        var o = MagneticGrappleRules.ResolveOutcome(MassClass.Boss, Cfg);
        Assert.Equal(GrappleOutcomeKind.NoEffect, o.Kind);
    }

    [Fact]
    public void Anchor_PullsPlayerToAnchor()
    {
        var o = MagneticGrappleRules.ResolveOutcome(MassClass.Anchor, Cfg);
        Assert.Equal(GrappleOutcomeKind.PullPlayerToAnchor, o.Kind);
        Assert.Equal(0, o.StaggerFrames);
    }

    [Fact]
    public void Immovable_HasNoEffect()
    {
        var o = MagneticGrappleRules.ResolveOutcome(MassClass.Immovable, Cfg);
        Assert.Equal(GrappleOutcomeKind.NoEffect, o.Kind);
    }

    [Fact]
    public void Light_StaggerFrames_FollowConfigOverride()
    {
        // Stagger value is config-driven, not hard-coded in the rule.
        var custom = Cfg with { LightStaggerFrames = 14 };
        var o = MagneticGrappleRules.ResolveOutcome(MassClass.Light, custom);
        Assert.Equal(14, o.StaggerFrames);
    }

    // --- IsTargetInRange ---

    [Theory]
    [InlineData(0f, true)]      // adjacent
    [InlineData(100f, true)]    // mid-range
    [InlineData(220f, true)]    // exactly at range — inclusive boundary
    [InlineData(220.01f, false)]
    [InlineData(500f, false)]
    public void IsTargetInRange_RespectsConfigRange(float distance, bool expected)
    {
        Assert.Equal(expected, MagneticGrappleRules.IsTargetInRange(distance, Cfg));
    }

    // --- ConeSnap ---

    [Theory]
    [InlineData(0f, true)]              // dead-on
    [InlineData(0.17f, true)]           // just inside ~10°
    [InlineData(0.17453293f, true)]     // exact half-angle
    [InlineData(0.18f, false)]          // just past
    [InlineData(-0.17f, true)]          // sign-symmetric
    [InlineData(-0.18f, false)]
    public void IsWithinConeSnap_HalfAngleSymmetric(float deltaRadians, bool expected)
    {
        Assert.Equal(expected, MagneticGrappleRules.IsWithinConeSnap(deltaRadians, Cfg));
    }

    // --- CanFire / OnResolved (cooldown enforcement) ---

    [Fact]
    public void CanFire_TrueWhenCooldownElapsed_AndGateOpen()
    {
        var s = new MagneticGrappleState(CooldownEndsAtSeconds: 1.0);
        Assert.True(MagneticGrappleRules.CanFire(s, gateAllowsFire: true, nowSeconds: 1.0));
        Assert.True(MagneticGrappleRules.CanFire(s, gateAllowsFire: true, nowSeconds: 2.5));
    }

    [Fact]
    public void CanFire_FalseWhileOnCooldown()
    {
        var s = new MagneticGrappleState(CooldownEndsAtSeconds: 5.0);
        Assert.False(MagneticGrappleRules.CanFire(s, gateAllowsFire: true, nowSeconds: 4.99));
    }

    [Fact]
    public void CanFire_FalseWhenGateClosed_EvenIfCooldownElapsed()
    {
        // gateAllowsFire is the caller's collapse of the state-gating table —
        // attacking / dodging / staggered etc. all flip it false.
        var s = MagneticGrappleState.Initial;
        Assert.False(MagneticGrappleRules.CanFire(s, gateAllowsFire: false, nowSeconds: 10.0));
    }

    [Fact]
    public void OnResolved_ArmsCooldown_FromConfig()
    {
        var s = MagneticGrappleRules.OnResolved(MagneticGrappleState.Initial, nowSeconds: 7.0, Cfg);
        Assert.Equal(9.5, s.CooldownEndsAtSeconds, 3); // 7 + 2.5
    }

    [Fact]
    public void OnResolved_FullCooldown_OnMissOrWallHit()
    {
        // PLANNING.md: cooldown on miss/wall hit is the full 2.5s ("no spam").
        // OnResolved doesn't differentiate — caller invokes it identically for
        // hit resolved, miss, wall, mid-pull death.
        var custom = Cfg with { CooldownSeconds = 4.0f };
        var s = MagneticGrappleRules.OnResolved(MagneticGrappleState.Initial, nowSeconds: 0.0, custom);
        Assert.Equal(4.0, s.CooldownEndsAtSeconds, 3);
    }

    [Fact]
    public void Fire_Resolve_Fire_Cycle_GatedByCooldown()
    {
        // End-to-end: fire at t=0 → resolve → can't fire mid-cooldown → can fire after.
        var s0 = MagneticGrappleState.Initial;
        Assert.True(MagneticGrappleRules.CanFire(s0, true, nowSeconds: 0.0));

        var s1 = MagneticGrappleRules.OnResolved(s0, nowSeconds: 0.5, Cfg);
        Assert.False(MagneticGrappleRules.CanFire(s1, true, nowSeconds: 1.0));   // mid-cooldown
        Assert.False(MagneticGrappleRules.CanFire(s1, true, nowSeconds: 2.99));  // just before
        Assert.True(MagneticGrappleRules.CanFire(s1, true, nowSeconds: 3.0));    // 0.5 + 2.5
    }

    // --- CombineStaggerFrames (max() rule, never sum) ---

    [Theory]
    [InlineData(0, 0, 0)]
    [InlineData(0, 8, 8)]
    [InlineData(8, 0, 8)]
    [InlineData(20, 8, 20)]   // existing elite stagger longer — keep it
    [InlineData(8, 20, 20)]   // grapple-applied stagger longer — extend
    [InlineData(8, 8, 8)]
    public void CombineStaggerFrames_TakesMax_NeverSums(int current, int grapple, int expected)
    {
        Assert.Equal(expected, MagneticGrappleRules.CombineStaggerFrames(current, grapple));
    }
}
