using Stationfall.Core.Rng;

namespace Stationfall.Core.Items;

// Weighted-pick drop table. Pure C# — Godot constructs one in code (or, later,
// from a Godot Resource that mirrors this shape) and calls Roll with a
// seeded RngService.
//
// Determinism: the same RngService state in produces the same LootRoll out.
// Tests cover this so future generator/save work can rely on it.
//
// Empty/zero-weight handling: a table with no entries — or whose entries
// all have Weight ≤ 0 — returns null from Roll. Callers treat null as
// "nothing dropped." This keeps the no-drop path explicit at the call site
// instead of forcing a sentinel ItemKey.
public class LootTable
{
    public IReadOnlyList<LootEntry> Entries { get; }

    public LootTable(IEnumerable<LootEntry> entries)
    {
        Entries = entries.ToList();
    }

    public LootTable(params LootEntry[] entries) : this((IEnumerable<LootEntry>)entries) { }

    public LootRoll? Roll(RngService rng)
    {
        int totalWeight = 0;
        for (int i = 0; i < Entries.Count; i++)
        {
            int w = Entries[i].Weight;
            if (w > 0) totalWeight += w;
        }
        if (totalWeight <= 0) return null;

        // NextInt is half-open [min, max). A pick in [0, totalWeight) lands in
        // exactly one entry's weight band.
        int pick = rng.NextInt(0, totalWeight);
        for (int i = 0; i < Entries.Count; i++)
        {
            int w = Entries[i].Weight;
            if (w <= 0) continue;
            if (pick < w)
            {
                var entry = Entries[i];
                int amount = RollAmount(rng, entry);
                return new LootRoll(entry.ItemKey, amount);
            }
            pick -= w;
        }

        // Unreachable: totalWeight > 0 guarantees the loop returns.
        return null;
    }

    private static int RollAmount(RngService rng, LootEntry entry)
    {
        int min = Math.Max(0, entry.MinAmount);
        int max = Math.Max(min, entry.MaxAmount);
        if (min == max) return min;
        // NextInt is half-open, so add 1 to make MaxAmount inclusive.
        return rng.NextInt(min, max + 1);
    }
}
