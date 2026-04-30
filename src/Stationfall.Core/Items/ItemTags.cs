namespace Stationfall.Core.Items;

// Five orthogonal tag axes per PLANNING.md § Item Tagging (W5).
//
// **Tags are metadata only.** Authoring / search / UI surface — they do not
// drive gameplay behavior. Behavior lives in ItemEffect handlers consumed by
// ComboModifiers.Resolve, DamageCalculator, the status pipeline, etc. A new
// item-effect rule lands as a new ItemEffect record + handler, never as a
// "did the item carry tag X" check.
//
// Per-axis enums are kept in their own namespaces (StatusTag.Slowed vs
// EffectScope.Hazard etc.) so the two intentional name collisions
// documented in PLANNING — Hazard and Tool appearing across axes — are
// unambiguous in code.

public enum StatusTag
{
    Slowed,
    Stunned,
    Bleeding,
    Poisoned,
    Burning,
    Frozen,
    Marked,
    Shielded,
    Armored,
    Contaminated,
}

public enum DeliveryTag
{
    Melee,
    Projectile,
    Beam,
    Aoe,
    Deployable,
    Hazard,
    Tool,
    Dodge,
    Combo,
    SelfBuff,
    RoomEffect,
}

public enum TriggerTag
{
    Always,
    OnHit,
    OnDamagingHit,
    OnCrit,
    OnComboFinisher,
    OnKill,
    OnRoomEntry,
    OnRoomClear,
    OnDodge,
    OnPerfectDodge,
    OnDamageTaken,
    OnLowHp,
    OnToolUse,
    OnPickup,
}

public enum RoleTag
{
    Offense,
    Defense,
    Movement,
    Utility,
    Economy,
    Perception,
    Exploration,
    Sustain,
}

public enum EffectScope
{
    Player,
    Enemy,
    Weapon,
    Projectile,
    Tool,
    Room,
    Door,
    Hazard,
    Pickup,
    Vendor,
    RunState,
    MetaState,
}
