namespace Stationfall.Core.Combat;

// Spec for one application of a status. Authored statically by items / weapons
// (e.g. Sedative Dart, Curtain Call); StatusTracker stamps a copy onto the
// target with an ExpireAt time computed from DurationSeconds.
//
// Magnitude fields are 1.0 = no effect, < 1.0 = reduction. Per PLANNING,
// Slowed currently does not touch attack rate (AttackRateMultiplier stays 1.0)
// — that field is plumbed through anyway so future statuses (Stun, Frozen)
// can land without a record-shape change.
public record StatusEffect(
    StatusKind Kind,
    float DurationSeconds,
    float MoveSpeedMultiplier,
    float DodgeDistanceMultiplier,
    float AttackRateMultiplier)
{
    // Sedative Dart profile per PLANNING § Drip Drone: −35% move, −20% dodge
    // distance, no attack-rate effect. Refrain × Pirouette × Curtain Call all
    // route through this factory so a single number edit re-tunes every Slow
    // application in the slice.
    public static StatusEffect Slow(float durationSeconds) => new(
        Kind: StatusKind.Slowed,
        DurationSeconds: durationSeconds,
        MoveSpeedMultiplier: 0.65f,
        DodgeDistanceMultiplier: 0.80f,
        AttackRateMultiplier: 1.0f);
}
