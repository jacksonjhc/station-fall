using Godot;
using Stationfall.Core.Ai;
using Stationfall.Core.Entities;

namespace Stationfall.Godot.Enemies;

// Editor-authorable enemy data. One .tres per enemy under Assets/Data/Enemies/.
// All fields land back in Core via ToCoreDefinition() — Core never sees this
// type, only the EnemyDefinition record it produces.
//
// Stat field names mirror TwitchingPatientConfig 1:1; the resource is a flat
// schema so designers can read and edit it without crossing into nested
// resources. When archetype #2 lands, split into per-archetype subclasses
// (e.g., DripDroneResource) that produce their own brain config.
[GlobalClass]
public partial class EnemyResource : Resource
{
    [Export] public string Id { get; set; } = "";
    [Export] public string DisplayName { get; set; } = "";

    // Visual + scene wiring used by the spawner / controller; Core doesn't see these.
    [Export] public PackedScene? Scene { get; set; }
    [Export] public Color BodyColor { get; set; } = new Color(0.55f, 0.65f, 0.45f);
    [Export] public Color WindupTint { get; set; } = new Color(1.0f, 0.85f, 0.30f);

    // Brain config — values match TwitchingPatientConfig defaults from W3.
    [Export] public int MaxHp { get; set; } = 2;
    [Export] public int AttackDamage { get; set; } = 1;
    [Export] public float IdleMoveSpeedPxPerSec { get; set; } = 35f;
    [Export] public float ChaseMoveSpeedPxPerSec { get; set; } = 75f;
    [Export] public float LungeSpeedPxPerSec { get; set; } = 180f;
    [Export] public float AggroRangePx { get; set; } = 160f;
    [Export] public float MeleeAttackRangePx { get; set; } = 44f;
    [Export] public int LungeWindupFrames { get; set; } = 14;
    [Export] public int LungeActiveFrames { get; set; } = 8;
    [Export] public int LungeRecoveryFrames { get; set; } = 22;
    [Export] public int StaggerFrames { get; set; } = 10;
    [Export] public float AttackCooldownSeconds { get; set; } = 1.25f;
    [Export] public float LosLostExpirySeconds { get; set; } = 2.0f;

    public TwitchingPatientConfig ToBrainConfig() => new(
        MaxHp: MaxHp,
        AttackDamage: AttackDamage,
        IdleMoveSpeedPxPerSec: IdleMoveSpeedPxPerSec,
        ChaseMoveSpeedPxPerSec: ChaseMoveSpeedPxPerSec,
        LungeSpeedPxPerSec: LungeSpeedPxPerSec,
        AggroRangePx: AggroRangePx,
        MeleeAttackRangePx: MeleeAttackRangePx,
        LungeWindupFrames: LungeWindupFrames,
        LungeActiveFrames: LungeActiveFrames,
        LungeRecoveryFrames: LungeRecoveryFrames,
        StaggerFrames: StaggerFrames,
        AttackCooldownSeconds: AttackCooldownSeconds,
        LosLostExpirySeconds: LosLostExpirySeconds);

    public EnemyDefinition ToCoreDefinition() => new(
        Id: Id,
        DisplayName: DisplayName,
        Brain: ToBrainConfig());
}
