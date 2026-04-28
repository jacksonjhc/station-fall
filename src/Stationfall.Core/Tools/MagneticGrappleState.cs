namespace Stationfall.Core.Tools;

// Cooldown timer is the only Core-side runtime state. The in-flight projectile
// position and active-pull animation live in Godot — Core gets re-queried only
// when those resolve (hit / miss / wall / mid-pull death), at which point
// MagneticGrappleRules.OnResolved arms the cooldown.
public record MagneticGrappleState(double CooldownEndsAtSeconds)
{
    public static MagneticGrappleState Initial { get; } = new(0.0);
}
