using Godot;
using Stationfall.Core.Combat;
using Stationfall.Core.Entities;
using Stationfall.Core.Items;
using Stationfall.Core.Tools;
using Stationfall.Godot.Audio;
using Stationfall.Godot.Combat;
using Stationfall.Godot.Items;
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
    // Driven by Magnetic Grapple's PullPlayerToAnchor / PullPlayerToTarget /
    // SplitPullToMidpoint outcomes. Player input is ignored; position is
    // lerped toward the pull destination by the tool. PLANNING.md gates the
    // post-attach pull as non-cancellable by default (Grapple Cancel passive
    // changes that, post-slice).
    BeingPulled,
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

    // Combo input feel — three separate concepts:
    //
    //   1. Commitment.       During Attacking the player cannot enter a new
    //                        attack early. They can only buffer the next press.
    //   2. In-swing buffer.  Press during the late part of the swing → chained
    //                        when recovery ends. Window:
    //                          [recoveryStart - AttackInputBufferFrames, totalFrames)
    //                        (entire recovery counts; 12-frame pre-recovery
    //                        cushion lets a press just before recovery start
    //                        still chain.)
    //   3. Continuation grace. After recovery fully completes with no in-swing
    //                        buffer, the combo stays alive for
    //                        ComboContinuationGraceFrames. Pressing attack from
    //                        Idle/Moving inside that window advances to the
    //                        next combo step. Outside it, the press starts a
    //                        fresh combo at step 0.
    //
    // The 6-frame ComboStep.CancelWindowFrames constant is no longer the only
    // chain-eligible window — it now lives only as a reference value for
    // future "perfect-cancel" timing rewards. The broader buffer + grace
    // pair carries normal-cadence play.
    [Export] public int AttackInputBufferFrames { get; set; } = 12;
    [Export] public int ComboContinuationGraceFrames { get; set; } = 24;
    // Radius for the Pirouette finisher hitbox. Sword reach is medium (≈90°
    // sweep at ~62 px); 80 px for the ring keeps roughly the same bite range
    // but applied around the player. Playtest-tunable.
    [Export] public float PirouetteRadius { get; set; } = 80f;

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
    private Vector2 _pullDestination;
    private float _pullSpeedPxPerSec;
    private float _pullArrivalRadiusPx;
    // Hit-stop: wall-clock deadline (Godot ticks-msec) until which _PhysicsProcess
    // early-returns. Wall-clock so the freeze doesn't depend on _now, which
    // we deliberately stop advancing during the freeze.
    private ulong _frozenUntilTicksMsec;
    private double _dodgeRechargeUntil;
    private int _comboIndex;
    private double _comboResetAt;
    private bool _comboBuffered;
    private bool _attackHasHit;
    // Latest frame within the active swing on which attack was pressed. Read
    // at end-of-recovery to decide whether to chain — see TickAttack.
    // ComboInputResolver.NoPress (-1) means "no press captured this swing."
    //
    // **Max-one-queued invariant:** this field is a single int, not a queue.
    // Mashing during a swing overwrites with the latest frame so at most one
    // chain attack is ever scheduled. The chain-decision site explicitly
    // resets to NoPress on consumption (defense-in-depth alongside the
    // EnterAttack reset), so a buffered press cannot leak past one swing.
    // ChangeState into Dodging/Staggered/Dead/BeingPulled also clears via
    // ClearAttackBuffer.
    private int _attackBufferedFrame = ComboInputResolver.NoPress;
    // Snapshot of combo modifiers resolved from active passives at the start
    // of each EnterAttack (M7). Cached for the lifetime of the swing so a
    // pickup landing mid-swing doesn't change the active step's behavior;
    // the next EnterAttack picks the new mods up.
    private ComboModifiers _comboMods = ComboModifiers.Default(WeaponDefinition.Sword);

    // Read-only debug surface for the M7 readability overlay (DebugOverlay
    // queries these each frame). They're already used internally — exposing
    // does not change behavior.
    //
    // ComboIndex semantics:
    //   - During Attacking: index of the active step.
    //   - During Idle/Moving + ComboInGrace: index of the LAST COMPLETED step
    //     (next press chains to ComboIndex+1 if available).
    //   - During Idle/Moving + !ComboInGrace: stale; treat as "no combo".
    public int ComboIndex => _comboIndex;
    public ComboModifiers ComboMods => _comboMods;
    public bool ComboInGrace => State != PlayerState.Attacking && _now < _comboResetAt;
    public bool IsCurrentStepFinisher => State == PlayerState.Attacking && _comboMods.IsFinisher(_comboIndex);
    public bool IsCurrentFinisherRadial => IsCurrentStepFinisher && _comboMods.FinisherShape == ComboFinisherShape.Surround360;
    // True when the player just completed any step (in grace) — used by the
    // overlay to keep showing the combo line briefly after a non-finisher
    // tail, instead of flickering to "—" between presses.
    public bool HasComboLineToShow => State == PlayerState.Attacking || ComboInGrace;
    private HitboxComponent? _attackHitbox;
    private HurtboxComponent? _hurtbox;
    private Node2D? _visual;
    private GameCamera? _camera;
    private MagneticGrappleTool? _grappleTool;
    public ToolKind? EquippedToolKind { get; private set; }
    public MagneticGrappleTool? GrappleTool => _grappleTool;

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

    // Live weapon swap for the cadence A/B pass. Intentionally not gated on
    // anything in the run model — this is a debug-console hook that lets the
    // playtester compare default Sword vs SwordSlow timings without
    // restarting. Resets combo state so a mid-chain swap doesn't leave the
    // player at index 2 of a weapon that only has 3 steps anyway.
    public void SwapWeapon(WeaponDefinition weapon)
    {
        Weapon = weapon;
        _comboIndex = 0;
        _comboResetAt = 0;
        ClearAttackBuffer();
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

        // Tool-use press cancels any in-flight buffered attack so a player
        // who changes their mind mid-swing ("not another swing — grapple")
        // doesn't get a chain attack after the grapple resolves. We clear
        // unconditionally on the press; the tool's own gate (Idle/Moving)
        // decides whether the press actually fires the tool. Either way the
        // buffered chain is the wrong outcome once the player has signalled
        // tool intent. Dodge / damage / stagger / death / being-pulled are
        // all routed through ChangeState which clears the buffer there.
        if (Input.IsActionJustPressed("tool_use")) ClearAttackBuffer();

        // Continuation grace expired — drop _comboIndex back to 0 so the next
        // attack press starts a fresh chain. Only relevant when _comboIndex
        // > 0 (after step 0 the index already sits at 0; the grace window
        // still elapses but there's nothing to clear).
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
            case PlayerState.BeingPulled:
                TickBeingPulled();
                break;
        }

        ApplyIFrameTint();
    }

    // Tint priority (highest first):
    //   - Dodge i-frames    → cyan, marks the safe window
    //   - Grapple windup    → pale violet for ~6f, telegraphs incoming fire
    //   - default            → white
    // The two never coexist — grapple gating blocks fire while dodging — so
    // the layering is just for read-the-state cleanliness.
    private static readonly Color IFrameTint = new(0.6f, 0.9f, 1.0f);
    private static readonly Color WindupTint = new(0.85f, 0.75f, 1.0f);
    private void ApplyIFrameTint()
    {
        if (_visual is not CanvasItem ci) return;
        if (HasIFramesNow()) ci.Modulate = IFrameTint;
        else if (_grappleTool != null && _grappleTool.IsWindingUp) ci.Modulate = WindupTint;
        else ci.Modulate = Colors.White;
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
            // Continuation grace: pressing attack from a grounded state while
            // a previous swing's grace window is still open advances to the
            // next combo step. _comboIndex was preserved at swing-end as the
            // last-completed step. Outside the grace window — or after the
            // finisher (no next step exists) — start a fresh combo at step 0.
            bool inGrace = _now < _comboResetAt;
            bool hasNext = _comboIndex + 1 < _comboMods.FinalComboLength;
            if (inGrace && hasNext) _comboIndex++;
            else _comboIndex = 0;
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
        _attackBufferedFrame = ComboInputResolver.NoPress;

        // Resolve combo modifiers fresh at swing start so a passive picked up
        // between swings (Refrain mid-room, e.g.) takes effect immediately.
        var passives = PassivesService.Instance?.Passives;
        _comboMods = passives != null
            ? ComboModifiers.Resolve(Weapon, passives)
            : ComboModifiers.Default(Weapon);

        bool isFinisher = _comboMods.IsFinisher(_comboIndex);
        var step = ComboResolver.StepAt(Weapon, _comboIndex, _comboMods);

        if (_attackHitbox != null)
        {
            _attackHitbox.SetActive(false);
            // Pirouette: finisher only — body steps keep the directional sweep.
            bool radial = isFinisher && _comboMods.FinisherShape == ComboFinisherShape.Surround360;
            _attackHitbox.SetRadialMode(radial, PirouetteRadius);
            _attackHitbox.SetFacing(Facing);
            _attackHitbox.SetCurrentStep(step);
            _attackHitbox.AttackerStats = Stats;
            // Curtain Call: +50% damage and Slow application — finisher only.
            _attackHitbox.Modifiers = isFinisher
                ? new DamageModifiers(Multiplier: _comboMods.FinisherDamageMultiplier)
                : DamageModifiers.None;
            _attackHitbox.PendingFinisherStatus = isFinisher ? _comboMods.FinisherStatus : null;

            // Pirouette readability: telegraph the radial finisher with a ring
            // particle burst, debug print, and (later) a louder hit cue. Spawn
            // here at swing start — players read the ring as the wind-up sweep
            // before the active frames land.
            if (radial)
            {
                HitBurstPool.Instance?.Burst(GlobalPosition, Vector2.Right, HitBurstPool.BurstKind.PirouetteRing);
                GD.Print($"[combo] PIROUETTE finisher start  index={_comboIndex} length={_comboMods.FinalComboLength} radius={PirouetteRadius}");
            }
        }
    }

    private void TickAttack()
    {
        var step = ComboResolver.StepAt(Weapon, _comboIndex, _comboMods);
        Velocity = Vector2.Zero;
        MoveAndSlide();

        // Cancel into dodge any frame ≥ end-of-windup.
        if (_stateFrame >= step.WindupFrames && Input.IsActionJustPressed("dodge") && _now >= _dodgeRechargeUntil)
        {
            DeactivateHitbox();
            EnterDodge();
            return;
        }

        // Active phase: enable hitbox. Note: combo progression is purely
        // input-driven — `_attackHasHit` only deactivates the hitbox so a
        // single swing can't multi-tap the same enemy. A whiffed swing still
        // chains the next step normally if the player buffered an input.
        bool inActiveWindow = _stateFrame >= step.WindupFrames && _stateFrame < step.WindupFrames + step.ActiveFrames;
        if (_attackHitbox != null) _attackHitbox.SetActive(inActiveWindow && !_attackHasHit);

        // In-swing buffer: capture every attack press during the swing.
        // JustPressed is edge-triggered, so holding the button does NOT
        // refresh the buffer — a held attack still produces only one
        // captured frame (the initial press, which started this swing and
        // has already been reset to NoPress in EnterAttack). End-of-
        // recovery eligibility is decided by ComboInputResolver, which
        // explicitly rejects the NoPress sentinel — without that guard, a
        // generous AttackInputBufferFrames pushes windowStart below 0 and
        // -1 satisfies the range, causing a single press to auto-chain the
        // entire combo. Pinned in ComboInputResolverTests.
        if (Input.IsActionJustPressed("attack")) _attackBufferedFrame = _stateFrame;

        int recoveryStart = step.WindupFrames + step.ActiveFrames;
        if (ComboInputResolver.ShouldChain(
                _attackBufferedFrame, recoveryStart, step.TotalFrames, AttackInputBufferFrames))
        {
            _comboBuffered = true;
        }

        // End of recovery.
        if (_stateFrame >= step.TotalFrames)
        {
            DeactivateHitbox();
            // Refrain extends FinalComboLength past the weapon's authored
            // ComboLength; chain off that so extra body hits feed the
            // finisher slot at the end.
            if (_comboBuffered && _comboIndex + 1 < _comboMods.FinalComboLength)
            {
                // Explicit consumption — reset BEFORE EnterAttack so the
                // buffered intent is observably cleared even if any future
                // hook between here and EnterAttack reads these fields.
                _comboBuffered = false;
                _attackBufferedFrame = ComboInputResolver.NoPress;
                _comboIndex++;
                EnterAttack();
            }
            else
            {
                // No in-swing chain. Open the continuation grace window —
                // _comboIndex stays at the last-completed step so HandleGround
                // edInput can advance from it. Top-of-process resets to 0
                // when the grace window expires without an input.
                _comboResetAt = _now + Math.Max(0, ComboContinuationGraceFrames) / (double)PhysicsFps;
                _comboBuffered = false;
                _attackBufferedFrame = ComboInputResolver.NoPress;
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
        // Use the resolved step (Refrain shifts the heavy slot to a later
        // index; ComboResolver routes the finisher to the weapon's authored
        // heavy step regardless).
        bool heavy = ComboResolver.StepAt(Weapon, _comboIndex, _comboMods).IsHeavy;
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
        // A hit that actually lands cancels any pending chain attack — a
        // buffered press from before the hit shouldn't fire after hit-stop.
        // ChangeState into Dead (below) re-clears, but most damage doesn't
        // kill, so do it explicitly here too.
        ClearAttackBuffer();

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

    // Driven by MagneticGrappleTool. Locks input until the player's body is
    // within arrivalRadius of destination, then snaps to Idle. Damage taken
    // mid-pull still applies; if it kills, Dead supersedes BeingPulled and
    // the tool sees the state change as "settled" (correctly interrupts).
    // Equip the player's single tool slot. Phase B ships only Magnetic Grapple
    // — other ToolKinds log a warning. Pickup-when-occupied (swap-prompt UX
    // per W5 / PLANNING.md § Tool slot rules) is post-slice; for now a second
    // EquipTool call no-ops if a tool is already equipped.
    public bool EquipTool(ToolResource resource)
    {
        if (resource.Kind != ToolKind.MagneticGrapple)
        {
            GD.PushWarning($"EquipTool: kind {resource.Kind} not implemented yet");
            return false;
        }
        if (_grappleTool != null) return false;

        var tool = new MagneticGrappleTool();
        tool.Configure(resource.ToGrappleConfig());
        AddChild(tool);
        _grappleTool = tool;
        EquippedToolKind = resource.Kind;
        return true;
    }

    public void BeginBeingPulled(Vector2 destination, float speedPxPerSec, float arrivalRadiusPx)
    {
        if (State == PlayerState.Dead) return;
        _pullDestination = destination;
        _pullSpeedPxPerSec = speedPxPerSec;
        _pullArrivalRadiusPx = arrivalRadiusPx;
        ChangeState(PlayerState.BeingPulled);
    }

    private void TickBeingPulled()
    {
        var to = _pullDestination - GlobalPosition;
        float dist = to.Length();
        if (dist <= _pullArrivalRadiusPx)
        {
            // Snap-to-rest: PLANNING.md is explicit that the pull resolves
            // with no leftover momentum (no slide). Land in Idle, not Moving,
            // so Adrenaline / animation logic doesn't latch a movement frame.
            Velocity = Vector2.Zero;
            MoveAndSlide();
            ChangeState(PlayerState.Idle);
            return;
        }
        var dir = to / dist;
        Velocity = dir * _pullSpeedPxPerSec;
        Facing = dir;
        ApplyVisualFacing();
        MoveAndSlide();
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
        // Higher-priority intent — Dodging / Staggered / Dead / BeingPulled —
        // overrides any pending chain attack. Without this clear, a buffered
        // attack from earlier in the swing could fire after dodge completes
        // or after a stagger ends, betraying the player's later input.
        // Idle / Moving / Attacking transitions don't clear: those are the
        // normal swing-end and chain-start paths, where EnterAttack already
        // resets the buffer fresh.
        if (next == PlayerState.Dodging || next == PlayerState.Staggered ||
            next == PlayerState.Dead || next == PlayerState.BeingPulled)
        {
            ClearAttackBuffer();
        }
        State = next;
        EmitSignal(SignalName.StateChanged, (int)next);
    }

    // Drop any pending chain-attack intent. Safe to call repeatedly. Combo
    // chain length / grace timer / _comboIndex are NOT touched here — the
    // chain can still naturally continue if the player opts back in by
    // pressing attack inside the grace window.
    private void ClearAttackBuffer()
    {
        _attackBufferedFrame = ComboInputResolver.NoPress;
        _comboBuffered = false;
    }
}
