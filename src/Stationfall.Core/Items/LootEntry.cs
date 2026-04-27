namespace Stationfall.Core.Items;

// One slot in a LootTable.
//
// ItemKey is a stable string ("credit", "key_generic", "consumable_medkit")
// that the Godot side maps to a pickup scene. Keeping it a string keeps Core
// free of Godot scene references and lets new drop types land without enum
// edits.
//
// Weight is the relative likelihood the entry is chosen — they do not need
// to sum to any specific number. Weight 0 disables the entry.
//
// MinAmount/MaxAmount give the rolled quantity once the entry is picked
// (inclusive on both ends). Single-quantity drops set both to the same
// value. Negative values are rejected by LootTable.Roll.
public record LootEntry(string ItemKey, int Weight, int MinAmount, int MaxAmount)
{
    public LootEntry(string itemKey, int weight, int amount) : this(itemKey, weight, amount, amount) { }
}
