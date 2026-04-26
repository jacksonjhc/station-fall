namespace Stationfall.Core.Ai;

// W3 / PLANNING.md § Enemy Roster — Twitching Patient (starter melee rusher).
// All numbers are playtest-tunable; .tres pipeline (M3-2) will source them from data.
public record TwitchingPatientConfig(
    int MaxHp = 2,
    int AttackDamage = 1,
    float IdleMoveSpeedPxPerSec = 35f,
    float ChaseMoveSpeedPxPerSec = 75f,
    float LungeSpeedPxPerSec = 180f,
    // Bumped from W3's 160 after Day 3 playtest — at the play-scale character
    // size (44 px), 160 was "almost on top of them before they start moving."
    float AggroRangePx = 280f,
    // Bumped from W3's 44 to match the player's larger attack reach. The brain
    // uses this as the melee threshold — the Patient commits when the player
    // is within "they could hit me" range.
    float MeleeAttackRangePx = 60f,
    // Frames @ 60fps. IV Lunge: long recovery is the punish window per W3.
    int LungeWindupFrames = 14,
    int LungeActiveFrames = 8,
    int LungeRecoveryFrames = 22,
    int StaggerFrames = 10,
    float AttackCooldownSeconds = 1.25f,
    float LosLostExpirySeconds = 2.0f
)
{
    public static TwitchingPatientConfig Default { get; } = new();

    public int LungeTotalFrames => LungeWindupFrames + LungeActiveFrames + LungeRecoveryFrames;
}
