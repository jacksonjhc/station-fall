using Godot;
using Stationfall.Core.Combat;
using Stationfall.Core.Entities;
using Stationfall.Godot.Audio;
using Stationfall.Godot.Combat;
using Stationfall.Godot.UI;

namespace Stationfall.Godot.Player;

public enum PlayerState
{
    Idle,
    Moving,
    Dodging,
    Attacking,
    Staggered,
    Dead,
}

public partial class PlayerController : CharacterBody2D, IFreezable
{
    [Signal] public delegate void HealthChangedEventHandler(int hp, int maxHp);
    [Signal] public delegate void DiedEventHandler();
    [Signal] public delegate void AdrenalineActivatedEventHandler();
    [Signal] public delegate void AdrenalineEndedEventHandler();
    [Signal] public delegate void StateChangedEventHandler(int state);

    [Export] public NodePath AttackHitboxPath { get; set; } = "AttackHitbox";
    [Export] public NodePath HurtboxPath { get; set; } = "Hurtbox";
    [Export] public NodePath VisualPath { get; set; } = "Visual";
    [Export] public NodePath CameraPath { get; set; } = "Camera";

    // Vessel defaults to Clone for the M1 sandbox scene (BootstrapRoot has no
    // RunState to inject from). DungeonRoot calls Configure() with the vessel
    // off RunState before reading Stats, so a real run uses whatever vessel
    // the player picked at vessel-select.
    public PlayerVessel Vessel { get; private set; } = PlayerVessel.CreateClone();
    public EntityStats Stats { get; private set; }
    public DodgeProfile DodgeProfile { get; private set; } = DodgeProfile.Roll;
    public WeaponDefinition Weapon { get; private set; } = WeaponDefinition.Sword;
    public AdrenalineRushConfig SignatureConfig { get; private set; } = AdrenalineRushConfig.Default;
    public AdrenalineRushState SignatureState { get; private set; } = AdrenalineRushState.Initial;
    public PlayerState State { get; private set; } = PlayerState.Idle;
    public Vector2 Facing { get; private set; } = Vector2.Right;
    public bool GodMode { get; set; } = false;

    private const float PhysicsFps = 60f;
    private double _now;
    private int _stateFrame;
    // Hit-stop: wall-clock deadline (Godot ticks-msec) until which _PhysicsProcess
    // early-returns. Wall-clock so the freeze doesn't depend on _now, which
    // we deliberately stop advancing during the freeze.
    private ulong _frozenUntilTicksMsec;
    private double _dodgeRechargeUntil;
    private int _comboIndex;
    private double _comboResetAt;
    private bool _comboBuffered;
    private bool _attackHasHit;
    private HitboxComponent? _attackHitbox;
    private HurtboxComponent? _hurtbox;
    private Node2D? _visual;
    private GameCamera? _camera;

    public PlayerController()
    {
        // Field initializers populate Vessel; mirror its stats so Stats is
        // non-null at construction time (hurtbox stats provider may be queried
        // before _Ready in some edge orderings). _Ready calls ApplyVessel()
        // which re-runs this assignment.
        Stats = Vessel.BaseStats;
    }

    public override void _Ready()
    {
        ApplyVessel();

        _attackHitbox = GetNodeOrNull<HitboxComponent>(AttackHitboxPath);
        if (_attackHitbox != null)
        {
            _attackHitbox.SetActive(false);
            _attackHitbox.Owner2D = this;
            _attackHitbox.HitLanded += OnAttackHitLanded;
        }

        _camera = GetNodeOrNull<GameCamera>(CameraPath);

        _hurtbox = GetNodeOrNull<HurtboxComponent>(HurtboxPath);
        if (_hurtbox != null)
        {
            _hurtbox.Owner2D = this;
            _hurtbox.GetStatsProvider = () => Stats;
            _hurtbox.OnDamage = (result, _) => TakeDamage(result);
        }

        _visual = GetNodeOrNull<Node2D>(VisualPath);
        ApplyVisualFacing();

        EmitSignal(SignalName.HealthChanged, Stats.Hp, Stats.MaxHp);
    }

    // Swap to a different vessel post-_Ready (used by DungeonRoot once it has
    // resolved RunState.Vessel). Re-applies stats and re-emits HealthChanged
    // so HUD subscribers update; safe to call repeatedly.
    public void Configure(PlayerVessel vessel)
    {
        Vessel = vessel;
        ApplyVessel();
        EmitSignal(SignalName.HealthChanged, Stats.Hp, Stats.MaxHp);
    }

    private void ApplyVessel()
    {
        Stats = Vessel.BaseStats;
        DodgeProfile = Vessel.DodgeProfile;
        Weapon = Vessel.Weapon;
        SignatureConfig = Vessel.SignatureConfig;
    }

    // Kenney top-down-shooter sprites are authored facing right (+X) at rotation 0,
    // so Facing.Angle() maps directly onto the visual rotation.
    private void ApplyVisualFacing()
    {
        if (_visual == null) return;
        _visual.Rotation = Facing.Angle();
    }

    public override void _PhysicsProcess(double delta)
    {
        if (IsFrozen()) return;

        _now += delta;
        _stateFrame++;

        SignatureState = AdrenalineRushRule.Tick(SignatureState, _now, SignatureConfig);

        // Combo timeout — outside cancel window, reset.
        if (_comboIndex > 0 && _now >= _comboResetAt && State != PlayerState.Attacking)
        {
            _comboIndex = 0;
        }

        switch (State)
        {
            case PlayerState.Idle:
            case PlayerState.Moving:
                if (!HandleGroundedInput()) ApplyMovement();
                break;
            case PlayerState.Dodging:
                TickDodge();
                break;
            case PlayerState.Attacking:
                TickAttack();
                break;
            case PlayerState.Staggered:
                Velocity = Vector2.Zero;
                MoveAndSlide();
                break;
            case PlayerState.Dead:
                Velocity = Vector2.Zero;
                MoveAndSlide();
                break;
        }

        ApplyIFrameTint();
    }

    // Cyan tint while i-frames are live so the dodge punish window (last 3
    // recovery frames per W2) reads as visibly different from the safe window.
    private static readonly Color IFrameTint = new(0.6f, 0.9f, 1.0f);
    private void ApplyIFrameTint()
    {
        if (_visual is not CanvasItem ci) return;
        ci.Modulate = HasIFramesNow() ? IFrameTint : Colors.White;
    }

    // Returns true if the input switched state out of Idle/Moving — caller must skip ApplyMovement,
    // otherwise it will overwrite the new state and velocity on the same frame.
    private bool HandleGroundedInput()
    {
        if (Input.IsActionJustPressed("dodge") && _now >= _dodgeRechargeUntil)
        {
            EnterDodge();
            return true;
        }
        if (Input.IsActionJustPressed("attack"))
        {
            EnterAttack();
            return true;
        }
        return false;
    }

    private void ApplyMovement()
    {
        var input = Input.GetVector("move_left", "move_right", "move_up", "move_down");
        if (input.LengthSquared() > 0.01f)
        {
            Facing = input.Normalized();
            ApplyVisualFacing();
            ChangeState(PlayerState.Moving);
        }
        else
        {
            ChangeState(PlayerState.Idle);
        }

        float speed = Stats.MoveSpeed * SignatureState.MoveSpeedMultiplier(SignatureConfig);
        Velocity = input * speed;
        MoveAndSlide();
    }

    private void EnterDodge()
    {
        ChangeState(PlayerState.Dodging);
        _stateFrame = 0;
        _camera?.AddTrauma(0.05f);
        Sfx.Instance?.PlayDodge();
        var input = Input.GetVector("move_left", "move_right", "move_up", "move_down");
        var dir = input.LengthSquared() > 0.01f ? input.Normalized() : Facing;
        Facing = dir;
        ApplyVisualFacing();
        // distance / time: total frames at 60fps → seconds.
        float seconds = DodgeProfile.TotalFrames / PhysicsFps;
        float speed = DodgeProfile.DistancePixels / seconds;
        Velocity = dir * speed;
    }

    private void TickDodge()
    {
        MoveAndSlide();
        if (_stateFrame >= DodgeProfile.TotalFrames)
        {
            _dodgeRechargeUntil = _now + DodgeProfile.RechargeSeconds;
            Velocity = Vector2.Zero;
            ChangeState(PlayerState.Idle);
            _stateFrame = 0;
        }
    }

    public bool HasIFramesNow() =>
        State == PlayerState.Dodging && DodgeProfile.HasIFramesAt(_stateFrame);

    public bool IsFrozen() => Time.GetTicksMsec() < _frozenUntilTicksMsec;

    // Hit-stop entry. Idempotent against shorter freezes — call sites can pile
    // up smaller values without truncating an in-flight bigger freeze.
    public void Freeze(double seconds)
    {
        if (seconds <= 0) return;
        ulong target = Time.GetTicksMsec() + (ulong)(seconds * 1000.0);
        if (target > _frozenUntilTicksMsec) _frozenUntilTicksMsec = target;
    }

    private void EnterAttack()
    {
        ChangeState(PlayerState.Attacking);
        _stateFrame = 0;
        _attackHasHit = false;
        _comboBuffered = false;
        if (_attackHitbox != null)
        {
            _attackHitbox.SetActive(false);
            _attackHitbox.SetFacing(Facing);
            _attackHitbox.SetCurrentStep(Weapon.StepAt(_comboIndex));
            _attackHitbox.AttackerStats = Stats;
        }
    }

    private void TickAttack()
    {
        var step = Weapon.StepAt(_comboIndex);
        Velocity = Vector2.Zero;
        MoveAndSlide();

        // Cancel into dodge any frame ≥ end-of-windup.
        if (_stateFrame >= step.WindupFrames && Input.IsActionJustPressed("dodge") && _now >= _dodgeRechargeUntil)
        {
            DeactivateHitbox();
            EnterDodge();
            return;
        }

        // Active phase: enable hitbox.
        bool inActiveWindow = _stateFrame >= step.WindupFrames && _stateFrame < step.WindupFrames + step.ActiveFrames;
        if (_attackHitbox != null) _attackHitbox.SetActive(inActiveWindow && !_attackHasHit);

        // Cancel window for combo: first 6 recovery frames, attack input chains.
        int recoveryStart = step.WindupFrames + step.ActiveFrames;
        bool inCancelWindow = _stateFrame >= recoveryStart && _stateFrame < recoveryStart + ComboStep.CancelWindowFrames;
        if (inCancelWindow && Input.IsActionJustPressed("attack"))
        {
            _comboBuffered = true;
        }

        // End of recovery.
        if (_stateFrame >= step.TotalFrames)
        {
            DeactivateHitbox();
            if (_comboBuffered && _comboIndex + 1 < Weapon.ComboLength)
            {
                _comboIndex++;
                EnterAttack();
            }
            else
            {
                _comboIndex = 0;
                _comboResetAt = _now;
                ChangeState(PlayerState.Idle);
                _stateFrame = 0;
            }
        }
    }

    private void DeactivateHitbox()
    {
        if (_attackHitbox != null) _attackHitbox.SetActive(false);
    }

    public void NotifyHitLanded()
    {
        _attackHasHit = true;
        DeactivateHitbox();
    }

    // Light hits trauma'd down from W2's 0.15 to 0.10 — playtest-tunable.
    // Heavy combo finishers stay at the original 0.30: those are the "the
    // hit landed hard" moments. When crits land (post-slice), they should
    // ride heavy-tier trauma even on a light combo step.
    private void OnAttackHitLanded(Node2D target, int amount, bool armorBroken, bool killed)
    {
        NotifyHitLanded();
        bool heavy = Weapon.StepAt(_comboIndex).IsHeavy;
        _camera?.AddTrauma(heavy ? 0.30f : 0.10f);
    }

    public void TakeDamage(DamageResult result)
    {
        if (GodMode || State == PlayerState.Dead) return;
        // Dodge i-frames eat the hit entirely. Source: Player feedback model — the
        // dodge IS the defensive option; landing iframes is the reward.
        if (HasIFramesNow()) return;
        if (result.Amount <= 0 && result.ArmorAbsorbed <= 0) return;

        Stats = Stats.ApplyDamage(result);
        EmitSignal(SignalName.HealthChanged, Stats.Hp, Stats.MaxHp);

        // Per W2 trauma table: damage taken (1 HP) 0.25, heavy hit taken 0.50.
        // No "heavy attack taken" data on DamageResult yet — bracket via amount
        // for now. Threshold of 2 covers the only multi-damage source today
        // (Sword heavy finisher); revisit when more attack types ship.
        _camera?.AddTrauma(result.Amount >= 2 ? 0.50f : 0.25f);

        // Damage-taken cue + red burst. Direction defaults upward (no attacker
        // line-of-source on TakeDamage); the DamageTaken config has a wide
        // spread so it reads as "hit landed on me" radiating outward.
        Sfx.Instance?.PlayDamageTaken();
        HitBurstPool.Instance?.Burst(GlobalPosition, Vector2.Up, HitBurstPool.BurstKind.DamageTaken);

        bool wasBuff = SignatureState.BuffActive;
        SignatureState = AdrenalineRushRule.OnDamageTaken(
            SignatureState, Stats, isStaggered: State == PlayerState.Staggered, _now, SignatureConfig);
        if (!wasBuff && SignatureState.BuffActive) EmitSignal(SignalName.AdrenalineActivated);

        if (Stats.Hp <= 0)
        {
            ChangeState(PlayerState.Dead);
            _camera?.AddTrauma(0.80f);
            Sfx.Instance?.PlayPlayerDeath();
            HitBurstPool.Instance?.Burst(GlobalPosition, Vector2.Up, HitBurstPool.BurstKind.PlayerDeath);
            EmitSignal(SignalName.Died);
        }
    }

    public void Heal(int amount)
    {
        if (amount <= 0) return;
        Stats = Stats.WithHp(Stats.Hp + amount);
        EmitSignal(SignalName.HealthChanged, Stats.Hp, Stats.MaxHp);
    }

    public void SetHp(int hp)
    {
        Stats = Stats.WithHp(hp);
        EmitSignal(SignalName.HealthChanged, Stats.Hp, Stats.MaxHp);
    }

    private void ChangeState(PlayerState next)
    {
        if (State == next) return;
        State = next;
        EmitSignal(SignalName.StateChanged, (int)next);
    }
}
