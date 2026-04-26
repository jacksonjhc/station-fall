namespace Stationfall.Core.Ai;

// What an enemy "sees" this frame. Built by the Godot EnemyController each
// _PhysicsProcess (distance to player, raycast for LOS, own HP ratio) and
// handed to the Core brain. Pure data — no Godot types so the brain stays
// testable without the engine.
public record SensorData(
    float DistanceToPlayerPx,
    bool HasLineOfSight,
    float SelfHpRatio
)
{
    public bool IsAlive => SelfHpRatio > 0f;
}
