using Stationfall.Core.Rng;

namespace Stationfall.Core.Items;

// Deterministic vendor stock builder. Given a catalog of available
// consumables and a desired slot count, returns a stable list of
// VendorStockEntry — same RngService state in produces the same stock out.
//
// Selection is "distinct draw without replacement": no SKU appears twice in
// one stock. If slotCount exceeds the catalog, the entire catalog is
// returned (in shuffle order) — the vendor is allowed to be "thinner" than
// requested rather than failing.
//
// Empty catalog → empty stock. Zero / negative slotCount → empty stock.
//
// Why a separate type instead of folding into LootTable: vendor stock has
// distinct-draw semantics and a fixed slot count, while LootTable does
// weighted with-replacement single rolls. Trying to express one in terms
// of the other costs more clarity than the small duplication of the loop.
public static class VendorStockGenerator
{
    public static IReadOnlyList<VendorStockEntry> Generate(
        RngService rng,
        IReadOnlyList<ConsumableDefinition> catalog,
        int slotCount)
    {
        if (catalog.Count == 0 || slotCount <= 0) return Array.Empty<VendorStockEntry>();

        // Fisher–Yates partial shuffle: copy the catalog, swap the first N
        // slots with random later picks. After the loop, [0..N) holds the
        // selection — distinct, deterministic, and order-stable per seed.
        int take = Math.Min(slotCount, catalog.Count);
        var working = new ConsumableDefinition[catalog.Count];
        for (int i = 0; i < catalog.Count; i++) working[i] = catalog[i];

        for (int i = 0; i < take; i++)
        {
            int j = rng.NextInt(i, catalog.Count);
            (working[i], working[j]) = (working[j], working[i]);
        }

        var stock = new VendorStockEntry[take];
        for (int i = 0; i < take; i++)
        {
            var def = working[i];
            stock[i] = new VendorStockEntry(def.Id, def.PriceCredits);
        }
        return stock;
    }
}
