namespace Stationfall.Core.ProcGen;

// Per-room content scaling. Sourced from MetaState's current difficulty tier
// (M8 wires that up); for now passed via DungeonGeneratorOptions and applied
// uniformly. PLANNING § Difficulty Escalation defines the tier semantics —
// rooms tagged here so M5-4 / M9 templates can read it when picking enemy
// counts, loot quality, and elite presence.
public enum ContentTier
{
    Onboarding,
    Standard,
    Escalated,
    TruePath,
}
