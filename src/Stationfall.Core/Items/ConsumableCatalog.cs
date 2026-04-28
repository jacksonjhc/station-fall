namespace Stationfall.Core.Items;

// Static registry of consumable SKUs known to Core. Hand-curated for M4 —
// vendors and loot drops both look up by Id here. The list is intentionally
// small (one heal + one larger heal) so the vendor stock generator has
// something to choose from without W5's full consumable design landing
// first. Numbers are placeholder, playtest-tunable: the data lives here so
// a single edit re-prices every appearance.
//
// When a consumable lands as a .tres-driven resource (W5 / content
// pipeline), this static catalog becomes the loader's seed list and
// callers stay on the same lookup API.
public static class ConsumableCatalog
{
    public const string MedkitSmallId = "consumable_medkit_small";
    public const string MedkitLargeId = "consumable_medkit_large";

    public static readonly ConsumableDefinition MedkitSmall = new(
        Id: MedkitSmallId,
        DisplayName: "Field Patch",
        PriceCredits: 5,
        HealAmount: 1);

    public static readonly ConsumableDefinition MedkitLarge = new(
        Id: MedkitLargeId,
        DisplayName: "Trauma Kit",
        PriceCredits: 12,
        HealAmount: 3);

    public static readonly IReadOnlyList<ConsumableDefinition> All = new[]
    {
        MedkitSmall,
        MedkitLarge,
    };

    public static ConsumableDefinition? FindById(string id)
    {
        for (int i = 0; i < All.Count; i++)
        {
            if (All[i].Id == id) return All[i];
        }
        return null;
    }
}
