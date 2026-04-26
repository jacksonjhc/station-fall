namespace Stationfall.Core.Combat;

public record AdrenalineRushConfig(
    float HpThresholdRatio = 0.40f,
    float BuffDurationSeconds = 5.0f,
    float CooldownSeconds = 30.0f,
    float MoveSpeedMultiplier = 1.50f,
    float AttackRateMultiplier = 1.30f
)
{
    public static AdrenalineRushConfig Default { get; } = new();
}
