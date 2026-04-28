using Godot;
using Stationfall.Core.Entities;
using Stationfall.Godot.Combat;
using Stationfall.Godot.Enemies;

namespace Stationfall.Godot.Items;

// Magnetic Grapple projectile in flight. Travels at a fixed speed in a fixed
// direction from spawn. Each physics tick: raycast forward against walls (hit
// → resolve as miss at hit point), check overlapping Hurtable/GrappleTarget
// areas (hit → resolve with the resolved attach payload), and stop after
// MaxRangePx travelled (resolve as miss).
//
// Resolution is one-shot: the projectile signals Resolved exactly once with
// either a Light/Heavy/Medium/Anchor/etc. MassClass + the hit Node2D, or a
// "miss" payload, then queues itself free. The owning MagneticGrappleTool
// translates the payload into Core via MagneticGrappleRules.ResolveOutcome
// and animates the pull from there.
public partial class GrappleProjectile : Area2D
{
    public sealed record AttachPayload(MassClass MassClass, Node2D? Target, Vector2 HitPosition);

    [Signal] public delegate void ResolvedEventHandler();

    public AttachPayload? Payload { get; private set; }
    public bool IsResolved { get; private set; }

    public Vector2 SpawnPosition { get; set; }
    public Vector2 Direction { get; set; } = Vector2.Right;
    public float SpeedPxPerSec { get; set; } = 520f;
    public float MaxRangePx { get; set; } = 220f;

    private float _travelled;

    public override void _Ready()
    {
        // Detect anchors (GrappleTarget) and enemy hurtboxes (Hurtable).
        // Walls are checked via raycast each tick because the projectile is
        // an Area2D and Area2Ds don't body-collide with StaticBody2D walls
        // by default — a ray gives us a precise hit point too.
        CollisionLayer = 0;
        CollisionMask = CollisionLayers.Hurtable | CollisionLayers.GrappleTarget;
        Monitoring = true;
        AreaEntered += OnAreaEntered;
        GlobalPosition = SpawnPosition;
        Rotation = Direction.Angle();
    }

    public override void _PhysicsProcess(double delta)
    {
        if (IsResolved) return;

        var step = Direction * SpeedPxPerSec * (float)delta;
        var from = GlobalPosition;
        var to = from + step;

        // Wall raycast for the next-step segment. If the ray hits, we resolve
        // as a miss precisely at the impact point — looks like a clean stop
        // against geometry rather than a clip-through.
        var space = GetWorld2D().DirectSpaceState;
        var rayQuery = PhysicsRayQueryParameters2D.Create(from, to, CollisionLayers.Walls);
        var rayResult = space.IntersectRay(rayQuery);
        if (rayResult.Count > 0)
        {
            var hitPos = (Vector2)rayResult["position"];
            ResolveAsMiss(hitPos);
            return;
        }

        GlobalPosition = to;
        _travelled += step.Length();
        if (_travelled >= MaxRangePx)
        {
            ResolveAsMiss(GlobalPosition);
        }
    }

    private void OnAreaEntered(Area2D area)
    {
        if (IsResolved) return;

        // GrappleAnchor (its inner Area2D) ships on the GrappleTarget layer,
        // so any layer-9 area we touch is by definition an anchor. Walk up
        // to the anchor node — group membership keeps this resilient if the
        // scene gets reorganized.
        if (area.GetParent() is GrappleAnchor anchor)
        {
            ResolveAttach(new AttachPayload(MassClass.Anchor, anchor, anchor.GlobalPosition));
            return;
        }

        // Enemy hurtbox path. Walk up to the EnemyController to read its
        // MassClass. Future Grappable props will use the GrappleTarget layer
        // and a similar lookup.
        if (area is HurtboxComponent hurtbox && hurtbox.Owner2D is EnemyController enemy)
        {
            var mass = enemy.Definition?.MassClass ?? MassClass.Light;
            ResolveAttach(new AttachPayload(mass, enemy, enemy.GlobalPosition));
            return;
        }

        // Hurtable but neither a hurtbox-with-enemy nor an anchor (e.g. a
        // breakable crate). Treat as immovable for now; far-side switches
        // and crate-yank are deferred per PLANNING.md.
        if (area.GetParent() is Node2D fallback)
        {
            ResolveAttach(new AttachPayload(MassClass.Immovable, fallback, fallback.GlobalPosition));
        }
        else
        {
            ResolveAsMiss(GlobalPosition);
        }
    }

    private void ResolveAttach(AttachPayload payload)
    {
        IsResolved = true;
        Payload = payload;
        SetDeferred(Area2D.PropertyName.Monitoring, false);
        EmitSignal(SignalName.Resolved);
        QueueFree();
    }

    private void ResolveAsMiss(Vector2 at)
    {
        IsResolved = true;
        Payload = new AttachPayload(MassClass.Immovable, null, at);
        SetDeferred(Area2D.PropertyName.Monitoring, false);
        EmitSignal(SignalName.Resolved);
        QueueFree();
    }
}
