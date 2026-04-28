namespace Stationfall.Core.Entities;

// Tags an enemy or world object for displacement rules. Drives Magnetic
// Grapple outcome resolution today (PLANNING.md § Magnetic Grapple); future
// displacement tools (Stun Coil knockback, etc.) read the same tag so we
// don't grow parallel taxonomies.
//
// Anchor is a level-prop value (a `GrappleAnchor` static), not an enemy.
// Immovable covers world-fixed objects that should explicitly *reject* grapple
// rather than no-op silently from a missing case.
public enum MassClass
{
    Light,
    Medium,
    Heavy,
    Boss,
    Anchor,
    Immovable,
}
