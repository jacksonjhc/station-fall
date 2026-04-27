using System.Numerics;

namespace Stationfall.Core.Ai;

// Per-enemy runtime brain state. Mirrors the AdrenalineRushState pattern:
// pure data, advanced by EnemyAiBrain.Tick / OnDamageTaken into a new value.
//
// PhaseFrame is the integer frame counter within the current Phase, used to
// drive lunge windup/active/recovery and stagger duration. TimeSinceLosLoss
// is the seconds-of-LOS-loss accumulator that powers the 2.0 sec memory rule.
//
// WanderDirection is the unit-vector heading the brain has chosen for the
// current idle stutter (Vector2.Zero means "pausing in place"). WanderEndsAt
// is the absolute time at which the current stutter/pause expires and the
// brain rolls a new interval.
public record EnemyAiSnapshot(
    AiState Phase = AiState.Idle,
    int PhaseFrame = 0,
    float TimeSinceLosLossSeconds = 0f,
    double AttackCooldownEndsAtSeconds = 0,
    Vector2 WanderDirection = default,
    double WanderEndsAtSeconds = 0
)
{
    public static EnemyAiSnapshot Initial { get; } = new();
}
