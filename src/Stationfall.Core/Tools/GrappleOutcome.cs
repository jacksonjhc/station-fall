namespace Stationfall.Core.Tools;

// What the Godot side must execute when the grapple projectile attaches.
// PullEnemyToPlayer  — Light: pull target in, apply LightStaggerFrames.
// SplitPullToMidpoint — Medium: both move 50/50.
// PullPlayerToTarget  — Heavy / Elite: player travels, target is fixed.
// PullPlayerToAnchor  — Anchor: like PullPlayerToTarget, but the destination
//                       is a static world point (renders/snaps differently).
// NoEffect            — Boss / Immovable.
public enum GrappleOutcomeKind
{
    NoEffect,
    PullEnemyToPlayer,
    SplitPullToMidpoint,
    PullPlayerToTarget,
    PullPlayerToAnchor,
}

// StaggerFrames is non-zero only on PullEnemyToPlayer — PLANNING.md applies
// the 8-frame stagger to the canonical Light pull-target case, not the rest.
public record GrappleOutcome(GrappleOutcomeKind Kind, int StaggerFrames)
{
    public static readonly GrappleOutcome NoEffect = new(GrappleOutcomeKind.NoEffect, 0);
}
