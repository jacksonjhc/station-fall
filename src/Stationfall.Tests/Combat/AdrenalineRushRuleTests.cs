using Stationfall.Core.Combat;
using Stationfall.Core.Entities;
using Xunit;

namespace Stationfall.Tests.Combat;

public class AdrenalineRushRuleTests
{
    private static readonly AdrenalineRushConfig Cfg = AdrenalineRushConfig.Default;

    private static EntityStats Stats(int hp, int maxHp = 5) =>
        new(MaxHp: maxHp, Hp: hp, MoveSpeed: 120f, AttackPower: 1, AttackRate: 1.5f, Reach: 32, Luck: 0, Armor: 0);

    [Fact]
    public void Triggers_WhenHpDropsToThreshold()
    {
        // 5 max, threshold 0.4 → trigger at 2 HP (40%) or below.
        var s = AdrenalineRushRule.OnDamageTaken(AdrenalineRushState.Initial, Stats(2), isStaggered: false, nowSeconds: 1.0, Cfg);
        Assert.True(s.BuffActive);
        Assert.Equal(6.0, s.BuffEndsAtSeconds, 3);
    }

    [Fact]
    public void DoesNotTrigger_WhenHpStillAboveThreshold()
    {
        // 3 / 5 = 0.6, above threshold.
        var s = AdrenalineRushRule.OnDamageTaken(AdrenalineRushState.Initial, Stats(3), isStaggered: false, nowSeconds: 1.0, Cfg);
        Assert.False(s.BuffActive);
    }

    [Fact]
    public void DoesNotTrigger_WhenHpAtZero()
    {
        // The Clone is dead — Adrenaline shouldn't fire on the killing blow.
        var s = AdrenalineRushRule.OnDamageTaken(AdrenalineRushState.Initial, Stats(0), isStaggered: false, nowSeconds: 1.0, Cfg);
        Assert.False(s.BuffActive);
    }

    [Fact]
    public void DoesNotRetrigger_WhileBuffActive()
    {
        var s1 = AdrenalineRushRule.OnDamageTaken(AdrenalineRushState.Initial, Stats(2), isStaggered: false, nowSeconds: 1.0, Cfg);
        var s2 = AdrenalineRushRule.OnDamageTaken(s1, Stats(1), isStaggered: false, nowSeconds: 2.0, Cfg);
        Assert.True(s2.BuffActive);
        Assert.Equal(s1.BuffEndsAtSeconds, s2.BuffEndsAtSeconds, 3);
    }

    [Fact]
    public void Tick_DeactivatesBuff_WhenDurationElapsed_AndStartsCooldown()
    {
        var s1 = AdrenalineRushRule.OnDamageTaken(AdrenalineRushState.Initial, Stats(2), isStaggered: false, nowSeconds: 1.0, Cfg);
        var s2 = AdrenalineRushRule.Tick(s1, nowSeconds: 6.0, Cfg);
        Assert.False(s2.BuffActive);
        Assert.Equal(36.0, s2.CooldownEndsAtSeconds, 3); // 6.0 + 30s
    }

    [Fact]
    public void DoesNotTrigger_WhileOnCooldown()
    {
        var afterBuff = new AdrenalineRushState(BuffActive: false, BuffEndsAtSeconds: 0, CooldownEndsAtSeconds: 100.0, Queued: false);
        var s = AdrenalineRushRule.OnDamageTaken(afterBuff, Stats(1), isStaggered: false, nowSeconds: 50.0, Cfg);
        Assert.False(s.BuffActive);
    }

    [Fact]
    public void TriggersAgain_AfterCooldownExpires()
    {
        var afterBuff = new AdrenalineRushState(BuffActive: false, BuffEndsAtSeconds: 0, CooldownEndsAtSeconds: 100.0, Queued: false);
        var s = AdrenalineRushRule.OnDamageTaken(afterBuff, Stats(2), isStaggered: false, nowSeconds: 100.0, Cfg);
        Assert.True(s.BuffActive);
    }

    [Fact]
    public void Staggered_QueuesInsteadOfActivating()
    {
        var s = AdrenalineRushRule.OnDamageTaken(AdrenalineRushState.Initial, Stats(2), isStaggered: true, nowSeconds: 1.0, Cfg);
        Assert.False(s.BuffActive);
        Assert.True(s.Queued);
    }

    [Fact]
    public void OnStaggerEnd_ActivatesQueuedBuff()
    {
        var staggered = AdrenalineRushRule.OnDamageTaken(AdrenalineRushState.Initial, Stats(2), isStaggered: true, nowSeconds: 1.0, Cfg);
        var s = AdrenalineRushRule.OnStaggerEnd(staggered, nowSeconds: 1.5, Cfg);
        Assert.True(s.BuffActive);
        Assert.False(s.Queued);
        Assert.Equal(6.5, s.BuffEndsAtSeconds, 3);
    }

    [Fact]
    public void OnStaggerEnd_DoesNothing_WhenNothingQueued()
    {
        var s = AdrenalineRushRule.OnStaggerEnd(AdrenalineRushState.Initial, nowSeconds: 1.0, Cfg);
        Assert.False(s.BuffActive);
        Assert.False(s.Queued);
    }

    [Fact]
    public void OnStaggerEnd_ClearsQueue_WhenAlreadyOnCooldown()
    {
        var queuedDuringCd = new AdrenalineRushState(BuffActive: false, BuffEndsAtSeconds: 0, CooldownEndsAtSeconds: 100.0, Queued: true);
        var s = AdrenalineRushRule.OnStaggerEnd(queuedDuringCd, nowSeconds: 50.0, Cfg);
        Assert.False(s.BuffActive);
        Assert.False(s.Queued);
    }

    [Fact]
    public void HealingAboveThreshold_DoesNotEndBuff()
    {
        // Activate at low HP, then "heal" by passing higher HP. Buff persists.
        var active = AdrenalineRushRule.OnDamageTaken(AdrenalineRushState.Initial, Stats(2), isStaggered: false, nowSeconds: 1.0, Cfg);
        var healed = AdrenalineRushRule.Tick(active, nowSeconds: 3.0, Cfg);
        Assert.True(healed.BuffActive);
    }

    [Fact]
    public void Multipliers_AreOneWhenBuffInactive()
    {
        var s = AdrenalineRushState.Initial;
        Assert.Equal(1.0f, s.MoveSpeedMultiplier(Cfg));
        Assert.Equal(1.0f, s.AttackRateMultiplier(Cfg));
    }

    [Fact]
    public void Multipliers_MatchConfig_WhenBuffActive()
    {
        var s = AdrenalineRushRule.OnDamageTaken(AdrenalineRushState.Initial, Stats(2), isStaggered: false, nowSeconds: 1.0, Cfg);
        Assert.Equal(1.50f, s.MoveSpeedMultiplier(Cfg));
        Assert.Equal(1.30f, s.AttackRateMultiplier(Cfg));
    }
}
