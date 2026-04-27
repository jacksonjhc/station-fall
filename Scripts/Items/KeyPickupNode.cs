using Godot;
using Stationfall.Core.Rng;
using Stationfall.Core.Runs;
using Stationfall.Godot.Combat;
using Stationfall.Godot.Persistence;

namespace Stationfall.Godot.Items;

// Magnetic key pickup. Same shape as CreditPickupNode — kick-out window so
// the player can see the drop, then magnet engages and the pickup is
// consumed on contact, calling KeysService.Add. Persists across room exits
// the same way credit pickups do (see CreditPickupNode for the model).
public partial class KeyPickupNode : Area2D, IPersistentEntity
{
    [Export] public string EntityId { get; set; } = "";
    [Export] public int Value { get; set; } = 1;
    [Export] public float MagnetRadiusPx { get; set; } = 90f;
    [Export] public float MaxMagnetSpeedPxPerSec { get; set; } = 540f;
    [Export] public float CollectRadiusPx { get; set; } = 22f;

    public const string ItemKey = "key_generic";

    private Vector2 _kickVelocity;
    private double _kickRemainingSec;
    private CharacterBody2D? _player;
    private bool _collected;

    private const double KickDurationSec = 0.4;
    private const string PickupScenePath = "res://Scenes/Items/KeyPickup.tscn";
    private static PackedScene? _scene;

    public static void Spawn(Node parent, Vector2 globalPosition, int value, RngService rng)
    {
        if (value <= 0) return;
        _scene ??= ResourceLoader.Load<PackedScene>(PickupScenePath);
        if (_scene == null)
        {
            GD.PushError($"KeyPickupNode: missing scene at {PickupScenePath}");
            return;
        }
        var pickup = _scene.Instantiate<KeyPickupNode>();
        pickup.Value = value;
        pickup.EntityId = NewDynamicId();

        double angle = rng.NextDouble() * Math.PI * 2.0;
        // Slightly slower kick than credits — a key is heavier visually.
        float speed = 200f + (float)(rng.NextDouble() * 120.0);
        var kick = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * speed;

        // Same physics-flush guard as CreditPickupNode.Spawn — defer the
        // AddChild so Area2D shape registration doesn't fire mid-flush.
        Callable.From(() =>
        {
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

    public static void Restore(Node parent, Vector2 globalPosition, int value, string entityId)
    {
        if (value <= 0) return;
        _scene ??= ResourceLoader.Load<PackedScene>(PickupScenePath);
        if (_scene == null)
        {
            GD.PushError($"KeyPickupNode: missing scene at {PickupScenePath}");
            return;
        }
        var pickup = _scene.Instantiate<KeyPickupNode>();
        pickup.Value = value;
        pickup.EntityId = entityId;
        parent.AddChild(pickup);
        pickup.GlobalPosition = globalPosition;
    }

    private static string NewDynamicId() =>
        "pickup_key_" + System.Guid.NewGuid().ToString("N").Substring(0, 12);

    public void ApplyDropKick(Vector2 velocity)
    {
        _kickVelocity = velocity;
        _kickRemainingSec = KickDurationSec;
    }

    public override void _Ready()
    {
        CollisionLayer = 0;
        CollisionMask = CollisionLayers.PlayerBody;
        BodyEntered += OnBodyEntered;
    }

    public override void _PhysicsProcess(double delta)
    {
        if (_collected) return;

        // Magnet/collect gated during the toss window so the player sees the key
        // fan out from the corpse before it can be picked up.
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

        if (dist <= CollectRadiusPx)
        {
            Collect();
            return;
        }

        if (dist > MagnetRadiusPx) return;

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
        KeysService.Instance?.Add(Value);
        ApplyCollectedAppearance();
    }

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
        GlobalPosition = new Vector2(s.PositionX, s.PositionY);
    }
}
