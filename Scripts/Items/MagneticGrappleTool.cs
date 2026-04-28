using Godot;
using Stationfall.Core.Ai;
using Stationfall.Core.Entities;
using Stationfall.Core.Tools;
using Stationfall.Godot.Enemies;
using Stationfall.Godot.Player;

namespace Stationfall.Godot.Items;

// Runtime orchestrator for Magnetic Grapple. Lives as a child of
// PlayerController. State machine: Idle → Windup (6f) → Projectile in flight
// → Pulling → Idle (cooldown armed via Core MagneticGrappleRules).
//
// All rules live in Core (target outcomes, range, cone-snap, cooldown). This
// node only does Godot-side work: read input, find candidate targets via
// scene-tree groups, spawn the projectile, drive the pull animation, and
// hand control back when the pull settles.
public partial class MagneticGrappleTool : Node2D
{
    private enum Phase { Idle, Windup, Projectile, Pulling }

    private const string ProjectileScenePath = "res://Scenes/Items/GrappleProjectile.tscn";
    private static PackedScene? _projectileScene;

    // Pull animation speed used by both Player.BeginBeingPulled and
    // EnemyController.BeginExternalPull. Higher = snappier; PLANNING doesn't
    // pin a number, this is playtest-tunable like the Core knobs.
    [Export] public float PullSpeedPxPerSec { get; set; } = 800f;
    // Distance from the destination at which the pull is considered "done"
    // and control is restored. Slightly larger than the player body so we
    // don't fight depenetration on the last frame.
    [Export] public float PullArrivalRadiusPx { get; set; } = 24f;

    public MagneticGrappleConfig Config { get; private set; } = MagneticGrappleConfig.Default;

    private PlayerController? _player;
    private MagneticGrappleState _state = MagneticGrappleState.Initial;
    private Phase _phase = Phase.Idle;
    private double _now;
    private int _windupFrame;
    private Vector2 _aimDirection = Vector2.Right;
    private GrappleProjectile? _activeProjectile;
    private GrappleOutcome? _activeOutcome;
    private Node2D? _activeTarget;

    public void Configure(MagneticGrappleConfig config) => Config = config;

    public override void _Ready()
    {
        _player = GetParent() as PlayerController;
        if (_player == null)
            GD.PushError("MagneticGrappleTool must be parented to a PlayerController");

        _projectileScene ??= ResourceLoader.Load<PackedScene>(ProjectileScenePath);
        if (_projectileScene == null)
            GD.PushError($"MagneticGrappleTool: missing scene at {ProjectileScenePath}");
    }

    public override void _PhysicsProcess(double delta)
    {
        _now += delta;
        switch (_phase)
        {
            case Phase.Idle:       TickIdle(); break;
            case Phase.Windup:     TickWindup(); break;
            case Phase.Projectile: TickProjectile(); break;
            case Phase.Pulling:    TickPull(); break;
        }
    }

    // The state-gating predicate from PLANNING.md collapsed to a bool.
    // Idle/Moving allow fire; Attacking/Dodging/Staggered/Dead/BeingPulled
    // block. (Already-grappling is implicit — _phase != Idle blocks here.)
    private bool GateAllowsFire()
    {
        if (_player == null) return false;
        var s = _player.State;
        return s == PlayerState.Idle || s == PlayerState.Moving;
    }

    private void TickIdle()
    {
        if (!Input.IsActionJustPressed("tool_use")) return;
        if (!MagneticGrappleRules.CanFire(_state, GateAllowsFire(), _now)) return;
        EnterWindup();
    }

    private void EnterWindup()
    {
        _aimDirection = ResolveAim();
        _phase = Phase.Windup;
        _windupFrame = 0;
    }

    private void TickWindup()
    {
        if (PlayerCancelled())
        {
            CancelAndArmCooldown();
            return;
        }
        _windupFrame++;
        if (_windupFrame >= Config.WindupFrames)
            SpawnProjectile();
    }

    private void TickProjectile()
    {
        // Dodge-cancel during projectile travel kills the projectile and
        // still arms full cooldown (PLANNING.md: "no spam").
        if (PlayerCancelled())
        {
            if (_activeProjectile != null && IsInstanceValid(_activeProjectile))
            {
                _activeProjectile.QueueFree();
                _activeProjectile = null;
            }
            CancelAndArmCooldown();
        }
    }

    private void TickPull()
    {
        // Pull is "done" once both moving entities reach the destination.
        // Player-side and enemy-side tick toward their target on their own
        // _PhysicsProcess; this just polls until both have settled.
        // PLANNING § Mid-pull death: if the pulled enemy is freed (hazard
        // tick, ally projectile), the pull cancels and cooldown starts as
        // if it resolved — IsInstanceValid catches that case.
        bool playerSettled = _player == null || _player.State != PlayerState.BeingPulled;
        bool targetSettled;
        if (_activeTarget == null || !IsInstanceValid(_activeTarget))
            targetSettled = true;
        else if (_activeTarget is EnemyController e)
            targetSettled = !e.IsBeingPulled;
        else
            targetSettled = true;

        if (playerSettled && targetSettled)
        {
            _activeOutcome = null;
            _activeTarget = null;
            ArmCooldown();
        }
    }

    // PLANNING § State gating: dodge / stagger / death cancel during windup
    // and projectile travel. Post-attach pull is committed (no cancel) — that
    // path is enforced by the Pulling phase not consulting this predicate.
    private bool PlayerCancelled()
    {
        if (_player == null) return true;
        var s = _player.State;
        return s == PlayerState.Dodging || s == PlayerState.Staggered || s == PlayerState.Dead;
    }

    // Free aim toward the mouse, with cone-snap to nearest valid target
    // within the cone (PLANNING § Aim model: ~10° half-angle).
    private Vector2 ResolveAim()
    {
        if (_player == null) return Vector2.Right;
        var origin = _player.GlobalPosition;
        var raw = _player.GetGlobalMousePosition() - origin;
        if (raw.LengthSquared() < 0.001f) raw = _player.Facing;
        var dir = raw.Normalized();

        var snap = FindConeSnapTarget(origin, dir);
        if (snap != null)
        {
            var toSnap = snap.GlobalPosition - origin;
            if (toSnap.LengthSquared() > 0.001f) return toSnap.Normalized();
        }
        return dir;
    }

    private Node2D? FindConeSnapTarget(Vector2 origin, Vector2 aim)
    {
        Node2D? best = null;
        float bestDist = float.MaxValue;

        foreach (var node in GetTree().GetNodesInGroup("enemies"))
        {
            if (node is not EnemyController e) continue;
            if (e.Phase == AiState.Dead) continue;
            var to = e.GlobalPosition - origin;
            float dist = to.Length();
            if (dist > Config.RangePx || dist < 0.001f) continue;
            float angle = aim.AngleTo(to / dist);
            if (!MagneticGrappleRules.IsWithinConeSnap(angle, Config)) continue;
            if (dist < bestDist) { bestDist = dist; best = e; }
        }

        foreach (var node in GetTree().GetNodesInGroup(GrappleAnchor.Group))
        {
            if (node is not Node2D anchor) continue;
            var to = anchor.GlobalPosition - origin;
            float dist = to.Length();
            if (dist > Config.RangePx || dist < 0.001f) continue;
            float angle = aim.AngleTo(to / dist);
            if (!MagneticGrappleRules.IsWithinConeSnap(angle, Config)) continue;
            if (dist < bestDist) { bestDist = dist; best = anchor; }
        }

        return best;
    }

    private void SpawnProjectile()
    {
        if (_player == null) return;
        if (_projectileScene == null)
        {
            CancelAndArmCooldown();
            return;
        }
        var proj = _projectileScene.Instantiate<GrappleProjectile>();
        proj.SpawnPosition = _player.GlobalPosition;
        proj.Direction = _aimDirection;
        proj.SpeedPxPerSec = Config.ProjectileSpeedPxPerSec;
        proj.MaxRangePx = Config.RangePx;
        proj.Resolved += OnProjectileResolved;

        // Parent to the current scene root so the projectile persists across
        // the player's transform changes (and so we don't accidentally pull
        // it along on PullPlayerToTarget).
        var host = GetTree().CurrentScene ?? (Node)this;
        host.AddChild(proj);
        _activeProjectile = proj;
        _phase = Phase.Projectile;
    }

    private void OnProjectileResolved()
    {
        var proj = _activeProjectile;
        _activeProjectile = null;
        if (proj?.Payload == null)
        {
            CancelAndArmCooldown();
            return;
        }

        var outcome = MagneticGrappleRules.ResolveOutcome(proj.Payload.MassClass, Config);
        if (outcome.Kind == GrappleOutcomeKind.NoEffect || proj.Payload.Target == null)
        {
            // Miss / wall / boss / immovable: full cooldown, no pull.
            ArmCooldown();
            return;
        }
        BeginPull(outcome, proj.Payload.Target);
    }

    private void BeginPull(GrappleOutcome outcome, Node2D target)
    {
        if (_player == null) { ArmCooldown(); return; }

        _activeOutcome = outcome;
        _activeTarget = target;
        _phase = Phase.Pulling;

        switch (outcome.Kind)
        {
            case GrappleOutcomeKind.PullEnemyToPlayer:
                if (target is EnemyController e)
                    e.BeginExternalPull(_player.GlobalPosition, PullSpeedPxPerSec, outcome.StaggerFrames, PullArrivalRadiusPx);
                else ArmCooldown();
                break;

            case GrappleOutcomeKind.PullPlayerToTarget:
            case GrappleOutcomeKind.PullPlayerToAnchor:
                _player.BeginBeingPulled(target.GlobalPosition, PullSpeedPxPerSec, PullArrivalRadiusPx);
                break;

            case GrappleOutcomeKind.SplitPullToMidpoint:
                var mid = (_player.GlobalPosition + target.GlobalPosition) * 0.5f;
                _player.BeginBeingPulled(mid, PullSpeedPxPerSec, PullArrivalRadiusPx);
                if (target is EnemyController em)
                    em.BeginExternalPull(mid, PullSpeedPxPerSec, 0, PullArrivalRadiusPx);
                break;

            default:
                ArmCooldown();
                break;
        }
    }

    private void CancelAndArmCooldown()
    {
        _activeOutcome = null;
        _activeTarget = null;
        ArmCooldown();
    }

    private void ArmCooldown()
    {
        _state = MagneticGrappleRules.OnResolved(_state, _now, Config);
        _phase = Phase.Idle;
        _windupFrame = 0;
    }
}
