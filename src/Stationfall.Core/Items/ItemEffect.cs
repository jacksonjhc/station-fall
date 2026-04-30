using Stationfall.Core.Combat;

namespace Stationfall.Core.Items;

// Polymorphic value description of "what the item does." Behavior dispatch
// happens at call sites by pattern-matching on concrete record types — never
// by reading tags on ItemDefinition.
//
// New behavior = new concrete record + new handler arm. Tags only feed
// authoring / UI / search.
public abstract record ItemEffect
{
    private ItemEffect() { }

    // +N steps to the held weapon's combo length, summed across stacks then
    // capped by the item's StackCap (Refrain ships at cap ×2 → max +2 total).
    // The cap is on stacks, not on Steps directly: an item with Steps=2 and
    // StackCap=1 still grants +2.
    public sealed record AddComboStep(int Steps) : ItemEffect;

    // Forces the combo finisher hitbox shape. Default = use the weapon's own
    // ArcDegrees (the Sword's 90° forward swing). Surround360 = full ring
    // around the player (Pirouette). If multiple effects of this kind are
    // active, the highest precedence wins — Surround360 > Default.
    public sealed record SetComboFinisherShape(ComboFinisherShape Shape) : ItemEffect;

    // Multiplies the finisher step's damage. Multipliers from multiple
    // sources compose multiplicatively (Curtain Call alone is the only M7
    // source; future "+50% finisher" stacks with it cleanly).
    public sealed record ComboFinisherDamageMultiplier(float Multiplier) : ItemEffect;

    // Applies this status to every enemy struck by the finisher hit. The
    // status pipeline's own refresh / no-magnitude-stack rule decides what
    // happens when the same enemy is re-hit (StatusTracker.Apply).
    public sealed record ComboFinisherApplyStatus(StatusEffect Status) : ItemEffect;
}

public enum ComboFinisherShape
{
    // Default: weapon's authored ArcDegrees, swept along Facing.
    Default,
    // Pirouette: 360° around the player.
    Surround360,
}
