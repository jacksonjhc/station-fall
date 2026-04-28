using Godot;

namespace Stationfall.Godot.Items;

// Static world prop the Magnetic Grapple can attach to. The grapple projectile
// resolves anchors as MassClass.Anchor → PullPlayerToAnchor outcome.
//
// Phase B scope: a single anchor type for traversal-teach rooms. Future
// Grappable props (yankable crates, ceiling anchors) will share the same
// GrappleTarget layer so the projectile finds them with one mask.
//
// No body collision — the player passes through the anchor's footprint at the
// end of the pull. Level geometry (walls under / around the anchor) is what
// actually keeps the player on the far platform.
public partial class GrappleAnchor : Node2D
{
    public const string Group = "grapple_anchors";

    [Export] public NodePath DetectionAreaPath { get; set; } = "DetectionArea";

    public override void _Ready()
    {
        AddToGroup(Group);

        var area = GetNodeOrNull<Area2D>(DetectionAreaPath);
        if (area != null)
        {
            // Project to the GrappleTarget layer so the projectile mask picks
            // up anchors. Empty mask — anchors don't actively monitor anything.
            area.CollisionLayer = Stationfall.Godot.Combat.CollisionLayers.GrappleTarget;
            area.CollisionMask = 0;
        }
    }
}
