namespace Stationfall.Core.Tools;

// Knobs for Magnetic Grapple (PLANNING.md § Magnetic Grapple — Cooldown / range / timing).
// All values playtest-tunable; defaults match the spec table.
public record MagneticGrappleConfig(
    float RangePx = 220f,
    float ProjectileSpeedPxPerSec = 520f,
    int WindupFrames = 6,
    float CooldownSeconds = 2.5f,
    int LightStaggerFrames = 8,
    // ~10° half-angle cone-snap toward valid targets. PLANNING § Aim model.
    float ConeSnapHalfAngleRadians = 0.17453293f
)
{
    public static MagneticGrappleConfig Default { get; } = new();
}
