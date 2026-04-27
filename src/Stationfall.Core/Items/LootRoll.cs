namespace Stationfall.Core.Items;

// Result of a single LootTable.Roll. ItemKey identifies the dropped item;
// Amount is the quantity rolled within the entry's Min/Max range.
public record LootRoll(string ItemKey, int Amount);
