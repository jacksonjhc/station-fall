using Godot;
using Stationfall.Core.Rng;
using Stationfall.Core.Runs;
using Stationfall.Godot.Combat;
using Stationfall.Godot.Persistence;

namespace Stationfall.Godot.Items;

// One credit pickup. Magnet pulls toward the player when in range, body_entered
// against the player grants on contact. Lives parented under the active room
// and persists across room exits via IPersistentEntity — the snapshotted
// PickupState records position + collected status so re-entry restores both
// authored vault credits (collected = stay invisible) and dynamic enemy
// drops (uncollected = re-spawn at saved position).
//
// Collision contract:
//   collision_layer = 0  (nothing else queries pickups)
//   collision_mask  = PlayerBody  (Area2D detects the player CharacterBody2D)
//
// EntityId rules:
//   - Authored placements (e.g. VaultRoom credits) set EntityId in the .tscn.
//   - Dynamic drops auto-assign a GUID-based id at Spawn / Restore time so
//     SnapshotRoom has something to key off.
public partial class CreditPickupNode : Area2D, IPersistentEntity
{
    [Export] public string EntityId { get; set; } = "";
    [Export] public int Value { get; set; } = 1;
    [Export] public float MagnetRadiusPx { get; set; } = 110f;
    [Export] public float MaxMagnetSpeedPxPerSec { get; set; } = 720f;
    [Export] public float CollectRadiusPx { get; set; } = 18f;

    public const string ItemKey = "credit";

    private Vector2 _kickVelocity;
    private double _kickRemainingSec;
    private CharacterBody2D? _player;
    private bool _collected;

    private const double KickDurationSec = 0.35;
    private const string PickupScenePath = "res://Scenes/Items/CreditPickup.tscn";
    private static PackedScene? _scene;

    // Spawn a pickup under `parent` at world `globalPosition`, fanning outward
    // with a random kick. Loads the scene resource once and caches it.
    // Caller passes their own RngService so death-time spawns stay reproducible
    // from a seed (the enemy's per-instance _rng today; per-room seeded RNG
    // in M5).
    public static void Spawn(Node parent, Vector2 globalPosition, int value, RngService rng)
    {
        if (value <= 0) return;
        _scene ??= ResourceLoader.Load<PackedScene>(PickupScenePath);
        if (_scene == null)
        {
            GD.PushError($"CreditPickupNode: missing scene at {PickupScenePath}");
            return;
        }
        var pickup = _scene.Instantiate<CreditPickupNode>();
        pickup.Value = value;
        pickup.EntityId = NewDynamicId();

        // Random outward kick — angle from rng so it's reproducible. Speed
        // 280–460 px/s combined with the linear-taper integration over
        // KickDurationSec (~0.35s) lands each coin ~50–80 px from the death
        // point, well outside the player's melee stop distance so the fan-out
        // reads visually before the magnet engages.
        double angle = rng.NextDouble() * Math.PI * 2.0;
        float speed = 280f + (float)(rng.NextDouble() * 180.0);
        var kick = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * speed;

        // Spawn() is called from EnemyController.HandleDeath, which is reached
        // from inside HurtboxComponent.OnAreaEntered — the Godot physics flush
        // window. Calling AddChild here triggers the new Area2D's shape
        // registration with the physics server, which Godot rejects with
        // "Can't change this state while flushing queries." Defer the add to
        // idle time, where the physics step is no longer in flight. Same
        // pattern DungeonRoot uses for door-driven room swaps. Position +
        // kick are applied inside the callback so the first _PhysicsProcess
        // sees the correct world position.
        Callable.From(() =>
        {
            // Parent may have been freed between schedule and fire (rare, but
            // possible if the room transitions in the same frame). Drop the
            // pickup if so.
            if (!GodotObject.IsInstanceValid(parent))
            {
                pickup.QueueFree();
                return;
            }
            parent.AddChild(pickup);
            pickup.GlobalPosition = globalPosition;
            pickup.ApplyDropKick(kick);
        }).CallDeferred();
    }

    // Re-spawn an orphan pickup whose state was snapshotted last visit but
    // whose source isn't authored in the room scene. No kick — the pickup
    // resumes at its saved-rest position. Called from DungeonRoot.RestoreRoom
    // outside any physics-flush window, so the AddChild is immediate.
    public static void Restore(Node parent, Vector2 globalPosition, int value, string entityId)
    {
        if (value <= 0) return;
        _scene ??= ResourceLoader.Load<PackedScene>(PickupScenePath);
        if (_scene == null)
        {
            GD.PushError($"CreditPickupNode: missing scene at {PickupScenePath}");
            return;
        }
        var pickup = _scene.Instantiate<CreditPickupNode>();
        pickup.Value = value;
        pickup.EntityId = entityId;
        parent.AddChild(pickup);
        pickup.GlobalPosition = globalPosition;
    }

    private static string NewDynamicId() =>
        "pickup_credit_" + System.Guid.NewGuid().ToString("N").Substring(0, 12);

    public override void _Ready()
    {
        CollisionLayer = 0;
        CollisionMask = CollisionLayers.PlayerBody;

        BodyEntered += OnBodyEntered;
    }

    public void ApplyDropKick(Vector2 velocity)
    {
        _kickVelocity = velocity;
        _kickRemainingSec = KickDurationSec;
    }

    public override void _PhysicsProcess(double delta)
    {
        if (_collected) return;

        // Decaying drop kick: linear taper to zero over KickDurationSec.
        // While the kick is in flight, magnet and collection are gated off so
        // the player sees the coin actually fan outward before it can snap to
        // them. Without this, melee kills land the player inside magnet range
        // immediately and coins vacuum invisibly. Isaac/Hades both use this
        // "drop has a brief grace window" pattern for the same reason.
        if (_kickRemainingSec > 0)
        {
            float t = (float)(_kickRemainingSec / KickDurationSec);
            Position += _kickVelocity * t * (float)delta;
            _kickRemainingSec -= delta;
            return;
        }

        if (_player == null)
            _player = GetTree().GetFirstNodeInGroup("player") as CharacterBody2D;
        if (_player == null) return;

        var to = _player.GlobalPosition - GlobalPosition;
        float dist = to.Length();

        // Fallback collect: if the player is very close but body_entered hasn't
        // fired yet (e.g., spawned overlapping the body inside a physics flush),
        // collect on proximity. Cheaper than fighting the physics queue.
        if (dist <= CollectRadiusPx)
        {
            Collect();
            return;
        }

        if (dist > MagnetRadiusPx) return;

        // Speed ramps from 0 at MagnetRadius up to MaxMagnetSpeed at 0 distance.
        // Using (1 - dist/radius) keeps the start-of-magnet motion gentle so the
        // pickup doesn't snap; closing speed accelerates as it nears the player.
        float speedRatio = 1f - (dist / MagnetRadiusPx);
        float speed = MaxMagnetSpeedPxPerSec * speedRatio;
        var dir = dist > 0.001f ? to / dist : Vector2.Zero;
        Position += dir * speed * (float)delta;
    }

    private void OnBodyEntered(Node2D body)
    {
        if (_collected) return;
        if (!body.IsInGroup("player")) return;
        Collect();
    }

    private void Collect()
    {
        if (_collected) return;
        _collected = true;
        CreditsService.Instance?.Add(Value);
        ApplyCollectedAppearance();
    }

    // Stay alive in the tree after collection so SnapshotRoom can capture
    // Collected=true (mirrors BreakableCrateNode). The room teardown frees
    // the node when the player exits. For dynamic pickups this also means the
    // saved entry has Collected=true, which the orphan-restore pass skips.
    private void ApplyCollectedAppearance()
    {
        Visible = false;
        SetDeferred(Area2D.PropertyName.Monitoring, false);
    }

    public EntityState? CaptureState() => new PickupState(
        ItemKey,
        Value,
        GlobalPosition.X,
        GlobalPosition.Y,
        _collected);

    public void RestoreState(EntityState state)
    {
        if (state is not PickupState s) return;
        if (s.Collected)
        {
            _collected = true;
            ApplyCollectedAppearance();
            return;
        }
        // Authored pickup that wasn't picked up last visit — resume from saved
        // position so e.g. a kicked-around vault credit lands wherever the
        // player nudged it, not at the .tscn-authored spot.
        GlobalPosition = new Vector2(s.PositionX, s.PositionY);
    }
}
