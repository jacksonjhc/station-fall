using System.Collections.Generic;
using Godot;

namespace Stationfall.Godot.Combat;

// GPU-particle burst pool. Sits at the root of Dungeon.tscn; combat sites
// reach it through HitBurstPool.Instance to spawn a one-shot directional
// emit at a given world point.
//
// Pooled because hit-landing rates of 5-10/sec across attackers would
// otherwise allocate Node + GpuParticles2D + ParticleProcessMaterial per
// impact, churning the .NET GC mid-combo. Each pooled emitter owns its own
// ParticleProcessMaterial — we mutate per-spawn rather than allocate.
//
// Soft cap: if every emitter is busy and we're under SoftCap, allocate one
// more. Above SoftCap, drop the burst silently rather than queue (a missed
// burst is invisible, queued bursts produce after-the-fact pops).
public partial class HitBurstPool : Node2D
{
    public enum BurstKind
    {
        HitLight,
        HitHeavy,
        DamageTaken,
        EnemyDeath,
        PlayerDeath,
        // Cyan spark fan at the projectile's resolution point. Reads as
        // "the grapple connected" and visually reinforces the icon palette
        // shared by GrappleAnchor / GrappleProjectile / cooldown HUD.
        GrappleHit,
        // Small grey puff at the projectile's wall / range-out position.
        // Without this, dry-fires felt like the projectile vanished.
        GrappleMiss,
    }

    public const string Group = "hit_burst_pool";
    public static HitBurstPool? Instance { get; private set; }

    private const int InitialSize = 16;
    private const int SoftCap = 32;

    // Particle texture paths — Kenney particle pack. See CLAUDE.md § Asset Bootstrap.
    private const string SparkTexturePath = "res://Assets/Kenney/kenney_particle-pack/PNG (Transparent)/spark_05.png";
    private const string SmokeTexturePath = "res://Assets/Kenney/kenney_particle-pack/PNG (Transparent)/smoke_03.png";

    private readonly List<GpuParticles2D> _pool = new();
    private Texture2D? _sparkTexture;
    private Texture2D? _smokeTexture;

    public override void _Ready()
    {
        Instance = this;
        AddToGroup(Group);

        _sparkTexture = ResourceLoader.Load<Texture2D>(SparkTexturePath);
        _smokeTexture = ResourceLoader.Load<Texture2D>(SmokeTexturePath);
        if (_sparkTexture == null) GD.PushWarning($"HitBurstPool: missing {SparkTexturePath}");
        if (_smokeTexture == null) GD.PushWarning($"HitBurstPool: missing {SmokeTexturePath}");

        for (int i = 0; i < InitialSize; i++) _pool.Add(CreateEmitter());
    }

    public override void _ExitTree()
    {
        if (Instance == this) Instance = null;
    }

    public void Burst(Vector2 globalPosition, Vector2 direction, BurstKind kind)
    {
        var emitter = AcquireEmitter();
        if (emitter == null) return;
        if (emitter.ProcessMaterial is not ParticleProcessMaterial mat) return;

        var cfg = ConfigFor(kind);
        emitter.Texture = cfg.UseSmoke ? _smokeTexture : _sparkTexture;
        emitter.Amount = cfg.Amount;
        emitter.Lifetime = cfg.Lifetime;

        mat.Direction = new Vector3(direction.X, direction.Y, 0);
        mat.Spread = cfg.SpreadDegrees;
        mat.InitialVelocityMin = cfg.SpeedMin;
        mat.InitialVelocityMax = cfg.SpeedMax;
        mat.ScaleMin = cfg.ScaleMin;
        mat.ScaleMax = cfg.ScaleMax;
        mat.DampingMin = cfg.Damping;
        mat.DampingMax = cfg.Damping;
        mat.Gravity = new Vector3(0, cfg.GravityY, 0);
        mat.Color = cfg.Color;

        emitter.GlobalPosition = globalPosition;
        // Restart (rather than Emitting=true) clears any in-flight particles
        // and forces a fresh explosive emit. Finished signal still fires when
        // this new emission completes — pool slot returns to free state then.
        emitter.Restart();
    }

    private GpuParticles2D? AcquireEmitter()
    {
        foreach (var e in _pool)
            if (!e.Emitting) return e;
        if (_pool.Count >= SoftCap) return null;
        var fresh = CreateEmitter();
        _pool.Add(fresh);
        return fresh;
    }

    private GpuParticles2D CreateEmitter()
    {
        var p = new GpuParticles2D
        {
            Emitting = false,
            OneShot = true,
            Explosiveness = 1.0f,
            Amount = 32,
            ProcessMaterial = new ParticleProcessMaterial(),
        };
        AddChild(p);
        return p;
    }

    // Scales halved from the M3.5-3 first pass — initial values obscured the
    // action (death clouds blacked out enemy positions, hit sparks dwarfed
    // sprites). Counts and speeds kept; tuning further from playtest.
    private static BurstConfig ConfigFor(BurstKind kind) => kind switch
    {
        BurstKind.HitLight => new BurstConfig(
            UseSmoke: false, Amount: 8, Lifetime: 0.25,
            SpreadDegrees: 55, SpeedMin: 200, SpeedMax: 400,
            ScaleMin: 0.15f, ScaleMax: 0.30f, Damping: 8f, GravityY: 0,
            Color: new Color(1.0f, 0.95f, 0.7f)),
        BurstKind.HitHeavy => new BurstConfig(
            UseSmoke: false, Amount: 16, Lifetime: 0.35,
            SpreadDegrees: 80, SpeedMin: 250, SpeedMax: 520,
            ScaleMin: 0.25f, ScaleMax: 0.48f, Damping: 8f, GravityY: 0,
            Color: new Color(1.0f, 0.85f, 0.45f)),
        BurstKind.DamageTaken => new BurstConfig(
            UseSmoke: false, Amount: 12, Lifetime: 0.30,
            SpreadDegrees: 70, SpeedMin: 180, SpeedMax: 360,
            ScaleMin: 0.18f, ScaleMax: 0.32f, Damping: 8f, GravityY: 0,
            Color: new Color(1.0f, 0.30f, 0.30f)),
        BurstKind.EnemyDeath => new BurstConfig(
            UseSmoke: true, Amount: 15, Lifetime: 0.70,
            SpreadDegrees: 180, SpeedMin: 60, SpeedMax: 140,
            ScaleMin: 0.30f, ScaleMax: 0.60f, Damping: 1.5f, GravityY: -40,
            Color: new Color(0.55f, 0.85f, 0.65f, 0.9f)),
        BurstKind.PlayerDeath => new BurstConfig(
            UseSmoke: true, Amount: 22, Lifetime: 0.85,
            SpreadDegrees: 180, SpeedMin: 80, SpeedMax: 180,
            ScaleMin: 0.40f, ScaleMax: 0.75f, Damping: 1.5f, GravityY: -30,
            Color: new Color(0.95f, 0.30f, 0.30f, 0.95f)),
        BurstKind.GrappleHit => new BurstConfig(
            UseSmoke: false, Amount: 10, Lifetime: 0.30,
            SpreadDegrees: 360, SpeedMin: 140, SpeedMax: 280,
            ScaleMin: 0.18f, ScaleMax: 0.32f, Damping: 6f, GravityY: 0,
            Color: new Color(0.70f, 0.95f, 1.0f)),
        BurstKind.GrappleMiss => new BurstConfig(
            UseSmoke: true, Amount: 6, Lifetime: 0.40,
            SpreadDegrees: 360, SpeedMin: 30, SpeedMax: 80,
            ScaleMin: 0.18f, ScaleMax: 0.32f, Damping: 4f, GravityY: -10,
            Color: new Color(0.65f, 0.70f, 0.78f, 0.7f)),
        _ => default,
    };

    private readonly record struct BurstConfig(
        bool UseSmoke,
        int Amount,
        double Lifetime,
        float SpreadDegrees,
        float SpeedMin,
        float SpeedMax,
        float ScaleMin,
        float ScaleMax,
        float Damping,
        float GravityY,
        Color Color);
}
