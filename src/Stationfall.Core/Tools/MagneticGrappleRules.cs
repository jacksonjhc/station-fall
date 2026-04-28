using Stationfall.Core.Entities;

namespace Stationfall.Core.Tools;

// Pure rules for Magnetic Grapple. See PLANNING.md § Magnetic Grapple.
//
// The Godot side aims with cone-snap, spawns the projectile, advances it at
// config.ProjectileSpeedPxPerSec, and on hit calls ResolveOutcome to learn
// what to do. After the pull resolves (or the projectile misses / hits a
// wall / target dies mid-pull), Godot calls OnResolved which arms the
// cooldown. Core never sees the in-flight projectile.
public static class MagneticGrappleRules
{
    // Per the MassClass → outcome table in PLANNING.md.
    public static GrappleOutcome ResolveOutcome(MassClass mass, MagneticGrappleConfig config) => mass switch
    {
        MassClass.Light     => new GrappleOutcome(GrappleOutcomeKind.PullEnemyToPlayer, config.LightStaggerFrames),
        MassClass.Medium    => new GrappleOutcome(GrappleOutcomeKind.SplitPullToMidpoint, 0),
        MassClass.Heavy     => new GrappleOutcome(GrappleOutcomeKind.PullPlayerToTarget, 0),
        MassClass.Boss      => GrappleOutcome.NoEffect,
        MassClass.Anchor    => new GrappleOutcome(GrappleOutcomeKind.PullPlayerToAnchor, 0),
        MassClass.Immovable => GrappleOutcome.NoEffect,
        _                   => GrappleOutcome.NoEffect,
    };

    public static bool IsTargetInRange(float distancePx, MagneticGrappleConfig config) =>
        distancePx >= 0f && distancePx <= config.RangePx;

    // deltaAngleRadians is the absolute angle between the player's aim and a
    // candidate target — already collapsed by the caller. Half-angle test
    // mirrors PLANNING's "~10° half-angle cone-snap" (~20° full cone).
    public static bool IsWithinConeSnap(float deltaAngleRadians, MagneticGrappleConfig config) =>
        System.MathF.Abs(deltaAngleRadians) <= config.ConeSnapHalfAngleRadians;

    // gateAllowsFire collapses the multi-row state-gating table from PLANNING
    // (no fire while attacking / dodging / staggered / already grappling /
    // charging another tool) into a single bool the caller computes from
    // PlayerState. Keeping it bool means Core has no opinion on which states
    // those are — the table can grow without Core changes.
    public static bool CanFire(MagneticGrappleState state, bool gateAllowsFire, double nowSeconds) =>
        gateAllowsFire && nowSeconds >= state.CooldownEndsAtSeconds;

    // Called whenever the grapple resolves: pull complete, miss, wall hit,
    // mid-pull death. PLANNING.md is explicit that miss / wall hit incur the
    // full cooldown ("no spam") — the caller doesn't need to differentiate.
    public static MagneticGrappleState OnResolved(
        MagneticGrappleState state,
        double nowSeconds,
        MagneticGrappleConfig config) =>
        state with { CooldownEndsAtSeconds = nowSeconds + config.CooldownSeconds };

    // Combine grapple stagger with an enemy's existing stagger remainder.
    // PLANNING.md: "max(currentRemaining, grappleStagger), never sum" —
    // prevents trivial grapple-spam stagger lock on elites.
    public static int CombineStaggerFrames(int currentRemainingFrames, int grappleStaggerFrames) =>
        System.Math.Max(currentRemainingFrames, grappleStaggerFrames);
}
