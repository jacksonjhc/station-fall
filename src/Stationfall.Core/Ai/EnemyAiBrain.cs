using System;
using System.Numerics;
using Stationfall.Core.Rng;

namespace Stationfall.Core.Ai;

// Twitching Patient brain (W3 / PLANNING.md § Enemy Roster). Pure transition
// rules — same shape as AdrenalineRushRule. The Godot EnemyController will
// call Tick each _PhysicsProcess and OnDamageTaken when its Hurtbox fires.
//
// Patrol exists in the AiState enum for future archetypes; the Twitching
// Patient itself is dormant in Idle until aggro'd, so this brain treats
// Patrol identically to Idle.
public static class EnemyAiBrain
{
    public static EnemyAiSnapshot Tick(
        EnemyAiSnapshot snapshot,
        SensorData sensor,
        double nowSeconds,
        float deltaSeconds,
        TwitchingPatientConfig config,
        RngService? rng = null)
    {
        if (snapshot.Phase == AiState.Dead) return snapshot;
        if (!sensor.IsAlive) return snapshot with { Phase = AiState.Dead, PhaseFrame = 0 };

        return snapshot.Phase switch
        {
            AiState.Idle or AiState.Patrol => TickIdle(snapshot, sensor, nowSeconds, config, rng),
            AiState.Chase => TickChase(snapshot, sensor, nowSeconds, deltaSeconds, config),
            AiState.Attack => TickAttack(snapshot, nowSeconds, config),
            AiState.Stagger => TickStagger(snapshot, sensor, config),
            _ => snapshot,
        };
    }

    public static EnemyAiSnapshot OnDamageTaken(
        EnemyAiSnapshot snapshot,
        SensorData sensorAfterDamage,
        TwitchingPatientConfig config)
    {
        if (snapshot.Phase == AiState.Dead) return snapshot;
        if (!sensorAfterDamage.IsAlive) return snapshot with { Phase = AiState.Dead, PhaseFrame = 0 };
        // Stagger always wins — interrupts windup, active, recovery, chase, idle.
        // Damage also forces aggro per W3 shared defaults: clear LOS-loss memory
        // so the Patient stays engaged after recovering.
        return snapshot with
        {
            Phase = AiState.Stagger,
            PhaseFrame = 0,
            TimeSinceLosLossSeconds = 0f,
        };
    }

    // Velocity the controller should apply during Idle/Patrol — direction the brain
    // picked, scaled by idle move speed. Zero while paused or whenever no rng was
    // supplied (the brain leaves WanderDirection at default in that case).
    public static Vector2 IdleVelocity(EnemyAiSnapshot snapshot, TwitchingPatientConfig config)
        => snapshot.WanderDirection * config.IdleMoveSpeedPxPerSec;

    private static EnemyAiSnapshot TickIdle(
        EnemyAiSnapshot snapshot,
        SensorData sensor,
        double nowSeconds,
        TwitchingPatientConfig config,
        RngService? rng)
    {
        // Vision perception: needs LOS AND distance ≤ aggro range.
        if (sensor.HasLineOfSight && sensor.DistanceToPlayerPx <= config.AggroRangePx)
            return snapshot with { Phase = AiState.Chase, PhaseFrame = 0, TimeSinceLosLossSeconds = 0f };

        // Roll a new stutter/pause interval each time the prior one elapses. Alternation
        // (currently moving → next is pause; currently paused → next is stutter) reads
        // more like a twitch than coin-flip roulette.
        if (rng != null && nowSeconds >= snapshot.WanderEndsAtSeconds)
        {
            bool currentlyMoving = snapshot.WanderDirection.LengthSquared() > 0f;
            if (currentlyMoving)
            {
                double pauseSeconds = config.IdlePauseMinSeconds
                    + rng.NextDouble() * (config.IdlePauseMaxSeconds - config.IdlePauseMinSeconds);
                return snapshot with
                {
                    PhaseFrame = snapshot.PhaseFrame + 1,
                    WanderDirection = Vector2.Zero,
                    WanderEndsAtSeconds = nowSeconds + pauseSeconds,
                };
            }
            else
            {
                double angle = rng.NextDouble() * 2.0 * Math.PI;
                var direction = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle));
                double stutterSeconds = config.IdleStutterMinSeconds
                    + rng.NextDouble() * (config.IdleStutterMaxSeconds - config.IdleStutterMinSeconds);
                return snapshot with
                {
                    PhaseFrame = snapshot.PhaseFrame + 1,
                    WanderDirection = direction,
                    WanderEndsAtSeconds = nowSeconds + stutterSeconds,
                };
            }
        }

        return snapshot with { PhaseFrame = snapshot.PhaseFrame + 1 };
    }

    private static EnemyAiSnapshot TickChase(
        EnemyAiSnapshot snapshot,
        SensorData sensor,
        double nowSeconds,
        float deltaSeconds,
        TwitchingPatientConfig config)
    {
        // LOS-loss accumulator: holds aggro for LosLostExpirySeconds after sight breaks,
        // resets to 0 the moment LOS is regained. Frame-rate independent (uses dt).
        float losLoss = sensor.HasLineOfSight ? 0f : snapshot.TimeSinceLosLossSeconds + deltaSeconds;
        if (losLoss >= config.LosLostExpirySeconds)
            return snapshot with { Phase = AiState.Idle, PhaseFrame = 0, TimeSinceLosLossSeconds = 0f };

        // Commit to the lunge if in melee range, with LOS, and cooldown elapsed.
        if (sensor.HasLineOfSight
            && sensor.DistanceToPlayerPx <= config.MeleeAttackRangePx
            && nowSeconds >= snapshot.AttackCooldownEndsAtSeconds)
        {
            return snapshot with
            {
                Phase = AiState.Attack,
                PhaseFrame = 0,
                TimeSinceLosLossSeconds = losLoss,
            };
        }

        return snapshot with
        {
            PhaseFrame = snapshot.PhaseFrame + 1,
            TimeSinceLosLossSeconds = losLoss,
        };
    }

    private static EnemyAiSnapshot TickAttack(
        EnemyAiSnapshot snapshot, double nowSeconds, TwitchingPatientConfig config)
    {
        int next = snapshot.PhaseFrame + 1;
        if (next >= config.LungeTotalFrames)
        {
            // Recovery complete. Cooldown counts from now (end of recovery), per W3:
            // "Attack cooldown 1.25 sec after recovery". Resume Chase to keep pressure
            // on; if the player has fled out of range, the next Chase tick may demote
            // to Idle via the LOS-loss path.
            return snapshot with
            {
                Phase = AiState.Chase,
                PhaseFrame = 0,
                AttackCooldownEndsAtSeconds = nowSeconds + config.AttackCooldownSeconds,
            };
        }
        return snapshot with { PhaseFrame = next };
    }

    private static EnemyAiSnapshot TickStagger(
        EnemyAiSnapshot snapshot, SensorData sensor, TwitchingPatientConfig config)
    {
        int next = snapshot.PhaseFrame + 1;
        if (next >= config.StaggerFrames)
        {
            // Resume Chase if the player is still on sensor (range + LOS), otherwise Idle.
            // Damage already forced aggro — this just decides whether to keep pressing.
            var phase = (sensor.HasLineOfSight && sensor.DistanceToPlayerPx <= config.AggroRangePx)
                ? AiState.Chase
                : AiState.Idle;
            return snapshot with { Phase = phase, PhaseFrame = 0, TimeSinceLosLossSeconds = 0f };
        }
        return snapshot with { PhaseFrame = next };
    }
}
