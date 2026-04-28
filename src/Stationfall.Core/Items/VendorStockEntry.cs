namespace Stationfall.Core.Items;

// One slot of a vendor's offered stock. References a ConsumableDefinition
// by Id (kept as a string so generated layouts can be serialized without
// dragging the catalog around) and pins a price at generation time.
//
// PriceCredits is captured here rather than re-read from the consumable
// because vendors can apply per-stock modifiers (sector multiplier, sale
// rooms, post-mid-boss markup) without mutating the catalog itself. The
// generator copies the consumable's base price by default; future tier or
// sector logic overrides it before constructing the entry.
public record VendorStockEntry(string ConsumableId, int PriceCredits);
