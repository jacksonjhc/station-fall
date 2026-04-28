namespace Stationfall.Core.Items;

// Per-SKU description of a consumable item the player can buy or pick up.
//
// Id is the stable string the Godot side maps to a sprite/icon and the
// vendor pedestal/HUD references when listing stock. Convention mirrors
// LootEntry.ItemKey ("consumable_medkit_small") so future loot drops and
// vendor stock can share a single registry without enum edits.
//
// HealAmount is the only effect today — a positive integer adds HP on
// consume. Zero means "non-heal consumable" and reserves space for the
// shield/buff/utility consumables that land alongside W5. The vendor uses
// HealAmount > 0 as the discriminator for "apply heal on purchase"; once
// non-heal effects exist they get their own field rather than overloading
// this one.
public record ConsumableDefinition(
    string Id,
    string DisplayName,
    int PriceCredits,
    int HealAmount);
