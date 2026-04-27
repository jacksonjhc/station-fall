using Godot;
using Stationfall.Core.Ai;
using Stationfall.Core.Combat;
using Stationfall.Core.Entities;
using Stationfall.Core.Rng;
using Stationfall.Godot.Combat;

namespace Stationfall.Godot.Enemies;

// Runs the Core EnemyAiBrain each _PhysicsProcess and translates phase
// transitions into Godot side effects (movement, hitbox toggling, color
// telegraph, free on death). All rules live in Core; this script is the thin
// "Godot displays" half of the architecture rule.
//
// Collision layers/masks: see Scripts/Combat/CollisionLayers.cs for the
// project-wide bit assignments referenced by both .tscn files and code.
public partial class EnemyController : CharacterBody2D, IFreezable
{
    [Signal] public delegate void DiedEventHandler();

    [Export] public EnemyResource? Definition { get; set; }
    [Export] public NodePath BodyVisualPath { get; set; } = "Visual";
    [Export] public NodePath HurtboxPath { get; set; } = "Hurtbox";
    [Export] public NodePath AttackHitboxPath { get; set; } = "AttackHitbox";
    // Mask for the LOS raycast — walls only.
    [Export(PropertyHint.Layers2DPhysics)] public uint LineOfSightMask { get; set; } = CollisionLayers.Walls;

    public AiState Phase => _snapshot.Phase;
    public EntityStats Stats => _stats;

    private TwitchingPatientConfig _config = TwitchingPatientConfig.Default;
    private EnemyAiSnapshot _snapshot = EnemyAiSnapshot.Initial;
    private EntityStats _stats;
    private double _now;
    // Hit-stop: wall-clock deadline (Godot ticks-msec). While frozen, brain tick
    // and movement are skipped so the enemy visibly stalls on impact.
    private ulong _frozenUntilTicksMsec;
    private CharacterBody2D? _player;
    private Node2D? _bodyVisual;
    private HurtboxComponent? _hurtbox;
    private HitboxComponent? _attackHitbox;
    private Color _baseColor = new(0.55f, 0.65f, 0.45f);
    private Color _windupColor = new(1.0f, 0.85f, 0.30f);
    private double _hitFlashUntilSeconds;
    private Vector2 _lungeDirection = Vector2.Right;
    private Vector2 _visualFacing = Vector2.Right;
    private RngService? _rng;
    private const double HitFlashSeconds = 0.10;

    public EnemyController()
    {
        // Constructed before _Ready resolves the definition — placeholder stats so
        // the hurtbox stats provider doesn't fault if it's queried very early.
        _stats = new EntityStats(MaxHp: 1, Hp: 1, MoveSpeed: 0, AttackPower: 0, AttackRate: 1, Reach: 0, Luck: 0, Armor: 0);
    }

    public override void _Ready()
    {
        if (Definition == null)
        {
            GD.PushError($"EnemyController '{Name}' has no Definition resource set");
            return;
        }

        _config = Definition.ToBrainConfig();
        _stats = new EntityStats(
            MaxHp: _config.MaxHp,
            Hp: _config.MaxHp,
            // MoveSpeed on EntityStats is unused for enemies (the brain config drives speed),
            // but keep it populated for diagnostics / future shared damage math.
            MoveSpeed: _config.ChaseMoveSpeedPxPerSec,
            AttackPower: 1,
            AttackRate: 1,
            Reach: 0,
            Luck: 0,
            Armor: 0);
        _baseColor = Definition.BodyColor;
        _windupColor = Definition.WindupTint;

        // Per-instance seed so two patients in the same room don't shimmy in lockstep.
        // GetInstanceId is Godot's stable, unique runtime id — fine as a placeholder
        // until run-level RNG (RunState seeding) lands.
        _rng = new RngService(unchecked((int)GetInstanceId()));

        _player = GetTree().GetFirstNodeInGroup("player") as CharacterBody2D;
        _bodyVisual = GetNodeOrNull<Node2D>(BodyVisualPath);
        _hurtbox = GetNodeOrNull<HurtboxComponent>(HurtboxPath);
        _attackHitbox = GetNodeOrNull<HitboxComponent>(AttackHitboxPath);

        if (_bodyVisual != null) _bodyVisual.Modulate = _baseColor;

        if (_hurtbox != null)
        {
            _hurtbox.Owner2D = this;
            _hurtbox.GetStatsProvider = () => _stats;
            _hurtbox.OnDamage = OnDamageReceived;
        }

        if (_attackHitbox != null)
        {
            _attackHitbox.Owner2D = this;
            _attackHitbox.AttackerStats = _stats;
            _attackHitbox.SetCurrentStep(BuildLungeStep(_config));
            _attackHitbox.SetActive(false);
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        if (IsFrozen())
        {
            // Don't advance _now or tick the brain during hit-stop; the impact
            // moment freezes everything. Visual flash continues on its own clock.
            ApplyVisual();
            return;
        }

        _now += delta;

        if (_snapshot.Phase == AiState.Dead) return;
        if (_player == null) return;

        var sensor = BuildSensor();
        var nextSnapshot = EnemyAiBrain.Tick(_snapshot, sensor, _now, (float)delta, _config, _rng);
        bool phaseChanged = nextSnapshot.Phase != _snapshot.Phase;
        _snapshot = nextSnapshot;

        if (phaseChanged) OnPhaseEntered();
        ExecutePhase();
        ApplyVisual();
    }

    private SensorData BuildSensor()
    {
        if (_player == null)
            return new SensorData(DistanceToPlayerPx: float.MaxValue, HasLineOfSight: false, SelfHpRatio: _stats.HpRatio);

        var distance = (_player.GlobalPosition - GlobalPosition).Length();
        return new SensorData(
            DistanceToPlayerPx: distance,
            HasLineOfSight: HasLineOfSight(),
            SelfHpRatio: _stats.HpRatio);
    }

    private bool HasLineOfSight()
    {
        if (_player == null) return false;
        // Wall-only ray. Enemy body (layer 64) and player body (layer 1) are not in the
        // mask, so they don't block their own LOS query — no exclude list needed.
        var space = GetWorld2D().DirectSpaceState;
        var query = PhysicsRayQueryParameters2D.Create(
            from: GlobalPosition,
            to: _player.GlobalPosition,
            collisionMask: LineOfSightMask);
        var result = space.IntersectRay(query);
        return result.Count == 0;
    }

    private void OnPhaseEntered()
    {
        switch (_snapshot.Phase)
        {
            case AiState.Attack:
                // Lock the lunge direction at the start of windup. Player movement during
                // the 14-frame windup must not steer the lunge — that's the punish window.
                if (_player != null)
                {
                    _lungeDirection = (_player.GlobalPosition - GlobalPosition).Normalized();
                    if (_lungeDirection.LengthSquared() < 0.001f) _lungeDirection = Vector2.Right;
                    _attackHitbox?.SetFacing(_lungeDirection);
                }
                break;
            case AiState.Stagger:
            case AiState.Idle:
            case AiState.Patrol:
            case AiState.Chase:
                _attackHitbox?.SetActive(false);
                break;
        }
    }

    private void ExecutePhase()
    {
        switch (_snapshot.Phase)
        {
            case AiState.Idle:
            case AiState.Patrol:
                var idle = EnemyAiBrain.IdleVelocity(_snapshot, _config);
                Velocity = new Vector2(idle.X, idle.Y);
                MoveAndSlide();
                break;
            case AiState.Chase:
                MoveTowardPlayer(_config.ChaseMoveSpeedPxPerSec);
                break;
            case AiState.Attack:
                ExecuteAttackPhase();
                break;
            case AiState.Stagger:
                Velocity = Vector2.Zero;
                MoveAndSlide();
                break;
        }
    }

    private void MoveTowardPlayer(float speed)
    {
        if (_player == null)
        {
            Velocity = Vector2.Zero;
            MoveAndSlide();
            return;
        }
        var to = _player.GlobalPosition - GlobalPosition;
        var distSq = to.LengthSquared();

        // Hold position once inside attack range. The brain commits to Attack at
        // MeleeAttackRangePx as soon as cooldown clears; during the ~1.25s gap
        // between recovery and the next lunge, continually pressing chase
        // velocity into a player whose body we're already touching reads as
        // "the enemy is stuck on me" — two CharacterBody2Ds sliding past each
        // other carries the patient wherever the player walks.
        var stop = _config.MeleeAttackRangePx;
        if (distSq <= stop * stop)
        {
            Velocity = Vector2.Zero;
            MoveAndSlide();
            return;
        }

        var dir = to / MathF.Sqrt(distSq);
        Velocity = dir * speed;
        MoveAndSlide();
    }

    private void ExecuteAttackPhase()
    {
        int frame = _snapshot.PhaseFrame;
        int activeStart = _config.LungeWindupFrames;
        int recoveryStart = _config.LungeWindupFrames + _config.LungeActiveFrames;

        if (frame < activeStart)
        {
            // Windup — locked in place, telegraph color via ApplyVisual().
            Velocity = Vector2.Zero;
            MoveAndSlide();
        }
        else if (frame < recoveryStart)
        {
            // Active — lunge along the locked direction; hitbox detects the player hurtbox.
            Velocity = _lungeDirection * _config.LungeSpeedPxPerSec;
            MoveAndSlide();
            _attackHitbox?.SetActive(true);
        }
        else
        {
            // Recovery — long punish window per W3, hitbox off. The lunge can
            // end with the patient overlapping the player; if we just hold
            // Velocity=0 here, the player's MoveAndSlide depenetration carries
            // the patient along when the player walks away ("the enemy is
            // glued to me"). Gentle backoff during recovery breaks the overlap
            // and reads as the lunge over-extending.
            Velocity = Vector2.Zero;
            if (_player != null)
            {
                var to = _player.GlobalPosition - GlobalPosition;
                float dist = to.Length();
                float separation = _config.MeleeAttackRangePx * 0.7f;
                if (dist < separation)
                {
                    var awayDir = dist > 0.001f ? -to / dist : -_lungeDirection;
                    Velocity = awayDir * 40f;
                }
            }
            MoveAndSlide();
            _attackHitbox?.SetActive(false);
        }
    }

    private void OnDamageReceived(DamageResult result, HitboxComponent source)
    {
        if (_snapshot.Phase == AiState.Dead) return;
        _stats = _stats.ApplyDamage(result);
        _hitFlashUntilSeconds = _now + HitFlashSeconds;

        var sensor = BuildSensor();
        _snapshot = EnemyAiBrain.OnDamageTaken(_snapshot, sensor with { SelfHpRatio = _stats.HpRatio }, _config);

        if (_snapshot.Phase == AiState.Dead)
        {
            HandleDeath();
        }
        else
        {
            // Stagger interrupts an in-flight attack — make sure the hitbox stops dealing damage.
            _attackHitbox?.SetActive(false);
        }
    }

    public bool IsFrozen() => Time.GetTicksMsec() < _frozenUntilTicksMsec;

    public void Freeze(double seconds)
    {
        if (seconds <= 0) return;
        ulong target = Time.GetTicksMsec() + (ulong)(seconds * 1000.0);
        if (target > _frozenUntilTicksMsec) _frozenUntilTicksMsec = target;
    }

    private void HandleDeath()
    {
        // Disable any further hits in or out before the node is freed.
        _attackHitbox?.SetActive(false);
        if (_hurtbox != null) _hurtbox.SetDeferred(Area2D.PropertyName.Monitoring, false);
        EmitSignal(SignalName.Died);
        QueueFree();
    }

    private void ApplyVisual()
    {
        if (_bodyVisual == null) return;

        // Facing rules:
        //   Attack — locked to the lunge direction captured at windup start.
        //   Chase  — track the player.
        //   Idle/Patrol while stuttering — face the wander direction (random, not the
        //     player) so the patient doesn't slide sideways. While paused, hold last
        //     facing so a dormant patient doesn't betray aggro.
        if (_snapshot.Phase == AiState.Attack)
        {
            _visualFacing = _lungeDirection;
        }
        else if (_snapshot.Phase == AiState.Chase && _player != null)
        {
            var to = _player.GlobalPosition - GlobalPosition;
            if (to.LengthSquared() > 0.001f) _visualFacing = to.Normalized();
        }
        else if (_snapshot.Phase is AiState.Idle or AiState.Patrol)
        {
            var wander = _snapshot.WanderDirection;
            if (wander.LengthSquared() > 0f)
                _visualFacing = new Vector2(wander.X, wander.Y);
        }
        // Sprites are authored facing right (+X) at rotation 0.
        _bodyVisual.Rotation = _visualFacing.Angle();

        bool flashing = _now < _hitFlashUntilSeconds;
        if (flashing)
        {
            _bodyVisual.Modulate = Colors.White;
            return;
        }
        bool inWindup = _snapshot.Phase == AiState.Attack && _snapshot.PhaseFrame < _config.LungeWindupFrames;
        bool inStagger = _snapshot.Phase == AiState.Stagger;
        _bodyVisual.Modulate = (inWindup || inStagger) ? _windupColor : _baseColor;
    }

    private static ComboStep BuildLungeStep(TwitchingPatientConfig cfg) => new(
        WindupFrames: cfg.LungeWindupFrames,
        ActiveFrames: cfg.LungeActiveFrames,
        RecoveryFrames: cfg.LungeRecoveryFrames,
        Damage: cfg.AttackDamage,
        // Committed lunge — heavier than a Sword light tap (60/30) but lighter
        // than a Sword heavy finisher (120/60). Playtest-tunable.
        HitstopTargetMs: 100,
        HitstopAttackerMs: 50,
        IsHeavy: false);
}
