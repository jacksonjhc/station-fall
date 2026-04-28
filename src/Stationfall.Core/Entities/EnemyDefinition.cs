using Stationfall.Core.Ai;

namespace Stationfall.Core.Entities;

// Schema target for the .tres → Core data pipeline (PLANNING.md § Content
// Authoring Pipeline). Godot-side EnemyResource projects its [Export] fields
// into one of these so Core never touches engine types.
//
// One archetype in M3 — Brain is concretely TwitchingPatientConfig. When a
// second archetype lands in M9, swap Brain for a polymorphic base type and
// add an Archetype discriminator.
public record EnemyDefinition(
    string Id,
    string DisplayName,
    TwitchingPatientConfig Brain,
    MassClass MassClass = MassClass.Light
);
