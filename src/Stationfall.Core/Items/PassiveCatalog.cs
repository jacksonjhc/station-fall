using Stationfall.Core.Combat;

namespace Stationfall.Core.Items;

// Hand-curated registry of slice-pool passives. M7 ships the locked W5 chain
// (Refrain → Pirouette → Curtain Call); the other 7 slice passives land
// alongside content rooms during M9.
//
// Numbers are playtest-tunable: a single edit here re-tunes every appearance.
public static class PassiveCatalog
{
    public const string RefrainId = "passive_refrain";
    public const string PirouetteId = "passive_pirouette";
    public const string CurtainCallId = "passive_curtain_call";

    // +1 combo step. Stack cap ×2 → +2 total over weapon base.
    // Tags: combo-body item, no status, "always" trigger (it modifies combo
    // length statically — not gated on the finisher firing).
    public static readonly ItemDefinition Refrain = ItemDefinition.Create(
        id: RefrainId,
        displayName: "Refrain",
        description: "+1 combo step.",
        tier: ItemTier.Common,
        stackCap: 2,
        effects: new ItemEffect[]
        {
            new ItemEffect.AddComboStep(Steps: 1),
        },
        deliveryTags: new[] { DeliveryTag.Melee, DeliveryTag.Combo },
        triggerTags: new[] { TriggerTag.Always },
        roleTags: new[] { RoleTag.Offense },
        scopes: new[] { EffectScope.Weapon });

    // Final combo hit becomes a 360° hitbox around the player. Not stackable.
    // Tags: combo + AOE delivery, on_combo_finisher trigger.
    public static readonly ItemDefinition Pirouette = ItemDefinition.Create(
        id: PirouetteId,
        displayName: "Pirouette",
        description: "The final hit of the melee combo becomes a 360° hitbox around you.",
        tier: ItemTier.Uncommon,
        stackCap: 1,
        effects: new ItemEffect[]
        {
            new ItemEffect.SetComboFinisherShape(ComboFinisherShape.Surround360),
        },
        deliveryTags: new[] { DeliveryTag.Melee, DeliveryTag.Combo, DeliveryTag.Aoe },
        triggerTags: new[] { TriggerTag.OnComboFinisher },
        roleTags: new[] { RoleTag.Offense },
        scopes: new[] { EffectScope.Weapon });

    // Final combo hit deals +50% damage and applies 1.5s Slow (Sedative Dart
    // profile). Not stackable. Routes the Slow through StatusEffect.Slow so
    // every Slow source in the slice keeps the same magnitudes.
    public static readonly ItemDefinition CurtainCall = ItemDefinition.Create(
        id: CurtainCallId,
        displayName: "Curtain Call",
        description: "The final hit of the melee combo deals +50% damage and applies Slow for 1.5 seconds.",
        tier: ItemTier.Rare,
        stackCap: 1,
        effects: new ItemEffect[]
        {
            new ItemEffect.ComboFinisherDamageMultiplier(Multiplier: 1.5f),
            new ItemEffect.ComboFinisherApplyStatus(StatusEffect.Slow(durationSeconds: 1.5f)),
        },
        statusTags: new[] { StatusTag.Slowed },
        deliveryTags: new[] { DeliveryTag.Melee, DeliveryTag.Combo },
        triggerTags: new[] { TriggerTag.OnComboFinisher },
        roleTags: new[] { RoleTag.Offense },
        scopes: new[] { EffectScope.Enemy, EffectScope.Weapon });

    // M7 demo: the deterministic synergy-proof item room offers exactly the
    // combo-finisher chain, one passive per pedestal. Order is the natural
    // chain order (body → finisher shape → finisher payoff) so the UI can
    // render them left-to-right without re-sorting. Subsequent item rooms
    // are expected to use real W5 roll weights — that path lands later.
    public static readonly IReadOnlyList<ItemDefinition> M7DemoOffering = new[]
    {
        Refrain,
        Pirouette,
        CurtainCall,
    };

    public static ItemDefinition? FindById(string id) => id switch
    {
        RefrainId => Refrain,
        PirouetteId => Pirouette,
        CurtainCallId => CurtainCall,
        _ => null,
    };
}
