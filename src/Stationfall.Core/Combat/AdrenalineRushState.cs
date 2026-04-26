namespace Stationfall.Core.Combat;

public record AdrenalineRushState(
    bool BuffActive = false,
    double BuffEndsAtSeconds = 0,
    double CooldownEndsAtSeconds = 0,
    bool Queued = false
)
{
    public static AdrenalineRushState Initial { get; } = new();

    public float MoveSpeedMultiplier(AdrenalineRushConfig config) =>
        BuffActive ? config.MoveSpeedMultiplier : 1.0f;

    public float AttackRateMultiplier(AdrenalineRushConfig config) =>
        BuffActive ? config.AttackRateMultiplier : 1.0f;
}
