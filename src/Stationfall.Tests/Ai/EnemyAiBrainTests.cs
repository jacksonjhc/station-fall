using System.Numerics;
using Stationfall.Core.Ai;
using Stationfall.Core.Rng;
using Xunit;

namespace Stationfall.Tests.Ai;

public class EnemyAiBrainTests
{
    private static readonly TwitchingPatientConfig Cfg = TwitchingPatientConfig.Default;
    private const float DtSixtyHz = 1f / 60f;

    private static SensorData Sees(float distancePx, float hpRatio = 1f) =>
        new(DistanceToPlayerPx: distancePx, HasLineOfSight: true, SelfHpRatio: hpRatio);

    private static SensorData Blind(float distancePx, float hpRatio = 1f) =>
        new(DistanceToPlayerPx: distancePx, HasLineOfSight: false, SelfHpRatio: hpRatio);

    // --- Aggro acquisition (Idle → Chase) ---

    [Fact]
    public void Idle_AcquiresChase_WhenInsideAggroRangeWithLineOfSight()
    {
        var s = EnemyAiBrain.Tick(EnemyAiSnapshot.Initial, Sees(100f), nowSeconds: 0, DtSixtyHz, Cfg);
        Assert.Equal(AiState.Chase, s.Phase);
        Assert.Equal(0, s.PhaseFrame);
    }

    [Fact]
    public void Idle_DoesNotAcquire_WhenOutsideAggroRange()
    {
        // Use a config-relative distance so this test doesn't break when AggroRangePx
        // is retuned for play-scale (which it has been, repeatedly).
        var s = EnemyAiBrain.Tick(EnemyAiSnapshot.Initial, Sees(Cfg.AggroRangePx + 50f), nowSeconds: 0, DtSixtyHz, Cfg);
        Assert.Equal(AiState.Idle, s.Phase);
    }

    [Fact]
    public void Idle_DoesNotAcquire_WithoutLineOfSight()
    {
        // Inside aggro range but blocked. Vision perception requires LOS per W3.
        var s = EnemyAiBrain.Tick(EnemyAiSnapshot.Initial, Blind(50f), nowSeconds: 0, DtSixtyHz, Cfg);
        Assert.Equal(AiState.Idle, s.Phase);
    }

    [Fact]
    public void Patrol_AcquiresChase_LikeIdle()
    {
        // Twitching Patient never patrols, but the enum slot exists for future archetypes —
        // the brain must treat Patrol as an aggro-eligible dormant state.
        var patrolling = EnemyAiSnapshot.Initial with { Phase = AiState.Patrol };
        var s = EnemyAiBrain.Tick(patrolling, Sees(100f), nowSeconds: 0, DtSixtyHz, Cfg);
        Assert.Equal(AiState.Chase, s.Phase);
    }

    // --- Melee commitment (Chase → Attack) ---

    [Fact]
    public void Chase_CommitsAttack_InMeleeRangeWithLosAndCooldownReady()
    {
        var chasing = EnemyAiSnapshot.Initial with { Phase = AiState.Chase };
        var s = EnemyAiBrain.Tick(chasing, Sees(40f), nowSeconds: 1.0, DtSixtyHz, Cfg);
        Assert.Equal(AiState.Attack, s.Phase);
        Assert.Equal(0, s.PhaseFrame);
    }

    [Fact]
    public void Chase_DoesNotAttack_WhenOutsideMeleeRange()
    {
        var chasing = EnemyAiSnapshot.Initial with { Phase = AiState.Chase };
        var s = EnemyAiBrain.Tick(chasing, Sees(80f), nowSeconds: 1.0, DtSixtyHz, Cfg);
        Assert.Equal(AiState.Chase, s.Phase);
    }

    [Fact]
    public void Chase_DoesNotAttack_WhenCooldownActive()
    {
        var chasing = EnemyAiSnapshot.Initial with
        {
            Phase = AiState.Chase,
            AttackCooldownEndsAtSeconds = 5.0,
        };
        var s = EnemyAiBrain.Tick(chasing, Sees(40f), nowSeconds: 1.0, DtSixtyHz, Cfg);
        Assert.Equal(AiState.Chase, s.Phase);
    }

    [Fact]
    public void Chase_DoesNotAttack_WithoutLineOfSight()
    {
        var chasing = EnemyAiSnapshot.Initial with { Phase = AiState.Chase };
        var s = EnemyAiBrain.Tick(chasing, Blind(40f), nowSeconds: 1.0, DtSixtyHz, Cfg);
        Assert.Equal(AiState.Chase, s.Phase);
    }

    // --- LOS-loss memory (Chase → Idle after 2.0 s) ---

    [Fact]
    public void Chase_LosesAggro_AfterLosLostExpiry()
    {
        var snap = EnemyAiSnapshot.Initial with { Phase = AiState.Chase };
        // Tick blind for slightly over 2.0 s of dt accumulation.
        for (int i = 0; i < 121; i++)
            snap = EnemyAiBrain.Tick(snap, Blind(80f), nowSeconds: 0, DtSixtyHz, Cfg);
        Assert.Equal(AiState.Idle, snap.Phase);
    }

    [Fact]
    public void Chase_KeepsAggro_BeforeLosLostExpiry()
    {
        var snap = EnemyAiSnapshot.Initial with { Phase = AiState.Chase };
        // 1.0 s of LOS loss — still under the 2.0 s threshold.
        for (int i = 0; i < 60; i++)
            snap = EnemyAiBrain.Tick(snap, Blind(80f), nowSeconds: 0, DtSixtyHz, Cfg);
        Assert.Equal(AiState.Chase, snap.Phase);
    }

    [Fact]
    public void Chase_ResetsLosTimer_WhenLineOfSightRegained()
    {
        var snap = EnemyAiSnapshot.Initial with { Phase = AiState.Chase, TimeSinceLosLossSeconds = 1.9f };
        var s = EnemyAiBrain.Tick(snap, Sees(80f), nowSeconds: 0, DtSixtyHz, Cfg);
        Assert.Equal(AiState.Chase, s.Phase);
        Assert.Equal(0f, s.TimeSinceLosLossSeconds);
    }

    // --- Attack lifecycle ---

    [Fact]
    public void Attack_AdvancesPhaseFrame_DuringWindup()
    {
        var attacking = EnemyAiSnapshot.Initial with { Phase = AiState.Attack };
        var s = EnemyAiBrain.Tick(attacking, Sees(40f), nowSeconds: 1.0, DtSixtyHz, Cfg);
        Assert.Equal(AiState.Attack, s.Phase);
        Assert.Equal(1, s.PhaseFrame);
    }

    [Fact]
    public void Attack_ReturnsToChase_AfterRecovery_AndArmsCooldown()
    {
        var snap = EnemyAiSnapshot.Initial with { Phase = AiState.Attack, PhaseFrame = 0 };
        // Walk through every frame of windup (14) + active (8) + recovery (22) = 44.
        for (int i = 0; i < Cfg.LungeTotalFrames; i++)
            snap = EnemyAiBrain.Tick(snap, Sees(40f), nowSeconds: 2.0, DtSixtyHz, Cfg);
        Assert.Equal(AiState.Chase, snap.Phase);
        Assert.Equal(0, snap.PhaseFrame);
        // Cooldown armed from end-of-recovery.
        Assert.Equal(2.0 + Cfg.AttackCooldownSeconds, snap.AttackCooldownEndsAtSeconds, 6);
    }

    [Fact]
    public void Attack_DoesNotEnd_BeforeRecoveryComplete()
    {
        var snap = EnemyAiSnapshot.Initial with { Phase = AiState.Attack, PhaseFrame = 0 };
        for (int i = 0; i < Cfg.LungeTotalFrames - 1; i++)
            snap = EnemyAiBrain.Tick(snap, Sees(40f), nowSeconds: 1.0, DtSixtyHz, Cfg);
        Assert.Equal(AiState.Attack, snap.Phase);
    }

    // --- Stagger ---

    [Fact]
    public void OnDamageTaken_FromIdle_EntersStagger()
    {
        var s = EnemyAiBrain.OnDamageTaken(EnemyAiSnapshot.Initial, Sees(80f, hpRatio: 0.5f), Cfg);
        Assert.Equal(AiState.Stagger, s.Phase);
        Assert.Equal(0, s.PhaseFrame);
    }

    [Fact]
    public void OnDamageTaken_InterruptsAttack()
    {
        var attacking = EnemyAiSnapshot.Initial with { Phase = AiState.Attack, PhaseFrame = 5 };
        var s = EnemyAiBrain.OnDamageTaken(attacking, Sees(40f, hpRatio: 0.5f), Cfg);
        Assert.Equal(AiState.Stagger, s.Phase);
        Assert.Equal(0, s.PhaseFrame);
    }

    [Fact]
    public void Stagger_TransitionsToChase_AfterDuration_WhenPlayerOnSensor()
    {
        var snap = EnemyAiSnapshot.Initial with { Phase = AiState.Stagger };
        for (int i = 0; i < Cfg.StaggerFrames; i++)
            snap = EnemyAiBrain.Tick(snap, Sees(80f), nowSeconds: 1.0, DtSixtyHz, Cfg);
        Assert.Equal(AiState.Chase, snap.Phase);
    }

    [Fact]
    public void Stagger_TransitionsToIdle_AfterDuration_WhenPlayerOffSensor()
    {
        var snap = EnemyAiSnapshot.Initial with { Phase = AiState.Stagger };
        for (int i = 0; i < Cfg.StaggerFrames; i++)
            snap = EnemyAiBrain.Tick(snap, Blind(500f), nowSeconds: 1.0, DtSixtyHz, Cfg);
        Assert.Equal(AiState.Idle, snap.Phase);
    }

    [Fact]
    public void Stagger_DoesNotEnd_BeforeDuration()
    {
        var snap = EnemyAiSnapshot.Initial with { Phase = AiState.Stagger };
        for (int i = 0; i < Cfg.StaggerFrames - 1; i++)
            snap = EnemyAiBrain.Tick(snap, Sees(80f), nowSeconds: 1.0, DtSixtyHz, Cfg);
        Assert.Equal(AiState.Stagger, snap.Phase);
    }

    // --- Death ---

    [Fact]
    public void Tick_TransitionsToDead_WhenSensorReportsZeroHp()
    {
        var s = EnemyAiBrain.Tick(EnemyAiSnapshot.Initial, Sees(80f, hpRatio: 0f), nowSeconds: 1.0, DtSixtyHz, Cfg);
        Assert.Equal(AiState.Dead, s.Phase);
    }

    [Fact]
    public void OnDamageTaken_KillingBlow_EntersDead_NotStagger()
    {
        var s = EnemyAiBrain.OnDamageTaken(EnemyAiSnapshot.Initial, Sees(40f, hpRatio: 0f), Cfg);
        Assert.Equal(AiState.Dead, s.Phase);
    }

    [Fact]
    public void Dead_IsTerminal()
    {
        var dead = EnemyAiSnapshot.Initial with { Phase = AiState.Dead };
        var s = EnemyAiBrain.Tick(dead, Sees(40f), nowSeconds: 1.0, DtSixtyHz, Cfg);
        Assert.Equal(AiState.Dead, s.Phase);
        var s2 = EnemyAiBrain.OnDamageTaken(dead, Sees(40f, hpRatio: 0.5f), Cfg);
        Assert.Equal(AiState.Dead, s2.Phase);
    }

    // --- Idle stutter-wander ---

    [Fact]
    public void Idle_WithoutRng_LeavesWanderDirectionAtZero()
    {
        // Backward compat: existing call sites that don't pass an rng must keep
        // the old "stand still" behavior — required so OnDamageTaken etc. work
        // unchanged in tests that don't care about wander.
        var s = EnemyAiBrain.Tick(EnemyAiSnapshot.Initial, Blind(500f), nowSeconds: 1.0, DtSixtyHz, Cfg);
        Assert.Equal(Vector2.Zero, s.WanderDirection);
        Assert.Equal(0, s.WanderEndsAtSeconds);
    }

    [Fact]
    public void Idle_WithRng_RollsAStutterDirection_OnFirstTick()
    {
        var rng = new RngService(seed: 1);
        var s = EnemyAiBrain.Tick(EnemyAiSnapshot.Initial, Blind(500f), nowSeconds: 0.0, DtSixtyHz, Cfg, rng);
        // First tick: was paused (zero direction) → starts a stutter.
        Assert.True(s.WanderDirection.LengthSquared() > 0f, "expected non-zero wander direction after first idle tick");
        // Direction is a unit vector (within float tolerance).
        Assert.Equal(1f, s.WanderDirection.Length(), 4);
        // Stutter duration is within configured bounds.
        Assert.InRange(s.WanderEndsAtSeconds, Cfg.IdleStutterMinSeconds, Cfg.IdleStutterMaxSeconds);
    }

    [Fact]
    public void Idle_WanderDirection_PersistsBetweenRollIntervals()
    {
        var rng = new RngService(seed: 2);
        // First tick rolls a stutter at t=0.
        var s = EnemyAiBrain.Tick(EnemyAiSnapshot.Initial, Blind(500f), nowSeconds: 0.0, DtSixtyHz, Cfg, rng);
        var directionAfterRoll = s.WanderDirection;
        // Tick again well before the stutter expires — direction must be unchanged.
        var s2 = EnemyAiBrain.Tick(s, Blind(500f), nowSeconds: 0.1, DtSixtyHz, Cfg, rng);
        Assert.Equal(directionAfterRoll, s2.WanderDirection);
    }

    [Fact]
    public void Idle_AlternatesStutterAndPause_AcrossIntervals()
    {
        var rng = new RngService(seed: 3);
        var snap = EnemyAiSnapshot.Initial;
        double now = 0.0;
        // Step time forward in coarse increments past each WanderEndsAt boundary,
        // collecting whether each rolled interval was a stutter (non-zero dir) or pause.
        var samples = new System.Collections.Generic.List<bool>(); // true = moving
        for (int i = 0; i < 20; i++)
        {
            snap = EnemyAiBrain.Tick(snap, Blind(500f), now, DtSixtyHz, Cfg, rng);
            samples.Add(snap.WanderDirection.LengthSquared() > 0f);
            now = snap.WanderEndsAtSeconds + 0.001;
        }
        Assert.Contains(true, samples);
        Assert.Contains(false, samples);
        // Strict alternation: each sample differs from the previous one.
        for (int i = 1; i < samples.Count; i++)
            Assert.NotEqual(samples[i - 1], samples[i]);
    }

    [Fact]
    public void IdleVelocity_IsZero_WhenWanderDirectionIsZero()
    {
        var v = EnemyAiBrain.IdleVelocity(EnemyAiSnapshot.Initial, Cfg);
        Assert.Equal(Vector2.Zero, v);
    }

    [Fact]
    public void IdleVelocity_ScalesByIdleMoveSpeed()
    {
        var snap = EnemyAiSnapshot.Initial with { WanderDirection = new Vector2(1f, 0f) };
        var v = EnemyAiBrain.IdleVelocity(snap, Cfg);
        Assert.Equal(Cfg.IdleMoveSpeedPxPerSec, v.X);
        Assert.Equal(0f, v.Y);
    }

    [Fact]
    public void Idle_AggroPathStill_TakesPrecedenceOverWander()
    {
        // Even with rng available, sighting the player must still trigger Chase
        // (without rolling new wander state to clobber the transition).
        var rng = new RngService(seed: 4);
        var s = EnemyAiBrain.Tick(EnemyAiSnapshot.Initial, Sees(100f), nowSeconds: 1.0, DtSixtyHz, Cfg, rng);
        Assert.Equal(AiState.Chase, s.Phase);
    }
}
