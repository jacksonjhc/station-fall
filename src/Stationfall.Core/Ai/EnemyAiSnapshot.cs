namespace Stationfall.Core.Ai;

// Per-enemy runtime brain state. Mirrors the AdrenalineRushState pattern:
// pure data, advanced by EnemyAiBrain.Tick / OnDamageTaken into a new value.
//
// PhaseFrame is the integer frame counter within the current Phase, used to
// drive lunge windup/active/recovery and stagger duration. TimeSinceLosLoss
// is the seconds-of-LOS-loss accumulator that powers the 2.0 sec memory rule.
public record EnemyAiSnapshot(
    AiState Phase = AiState.Idle,
    int PhaseFrame = 0,
    float TimeSinceLosLossSeconds = 0f,
    double AttackCooldownEndsAtSeconds = 0
)
{
    public static EnemyAiSnapshot Initial { get; } = new();
}
