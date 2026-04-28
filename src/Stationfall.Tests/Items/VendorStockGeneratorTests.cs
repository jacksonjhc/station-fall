using System.Collections.Generic;
using System.Linq;
using Stationfall.Core.Items;
using Stationfall.Core.Rng;
using Xunit;

namespace Stationfall.Tests.Items;

public class VendorStockGeneratorTests
{
    private static IReadOnlyList<ConsumableDefinition> Catalog(int count)
    {
        var list = new List<ConsumableDefinition>(count);
        for (int i = 0; i < count; i++)
        {
            list.Add(new ConsumableDefinition(
                Id: $"sku_{i}",
                DisplayName: $"Item {i}",
                PriceCredits: 5 + i,
                HealAmount: 1));
        }
        return list;
    }

    [Fact]
    public void Generate_SameSeed_ProducesSameStock()
    {
        var catalog = Catalog(6);
        var a = VendorStockGenerator.Generate(new RngService(42), catalog, slotCount: 3);
        var b = VendorStockGenerator.Generate(new RngService(42), catalog, slotCount: 3);
        Assert.Equal(a, b);
    }

    [Fact]
    public void Generate_DistinctSeeds_OftenDiffer()
    {
        var catalog = Catalog(8);
        var a = VendorStockGenerator.Generate(new RngService(1), catalog, 3);
        var b = VendorStockGenerator.Generate(new RngService(999), catalog, 3);
        // Probabilistic check — for an 8-SKU catalog and 3 slots, two
        // arbitrary seeds are overwhelmingly likely to differ. Asserting
        // inequality keeps the regression sharp without a flaky tolerance.
        Assert.NotEqual(a, b);
    }

    [Fact]
    public void Generate_AllEntriesReferenceCatalog()
    {
        var catalog = Catalog(5);
        var stock = VendorStockGenerator.Generate(new RngService(7), catalog, slotCount: 4);
        var ids = catalog.Select(c => c.Id).ToHashSet();
        foreach (var entry in stock)
            Assert.Contains(entry.ConsumableId, ids);
    }

    [Fact]
    public void Generate_NoDuplicateSkus()
    {
        var catalog = Catalog(5);
        var stock = VendorStockGenerator.Generate(new RngService(123), catalog, slotCount: 5);
        var distinct = stock.Select(e => e.ConsumableId).ToHashSet();
        Assert.Equal(stock.Count, distinct.Count);
    }

    [Fact]
    public void Generate_SlotCount_CapsAtCatalogSize()
    {
        var catalog = Catalog(3);
        var stock = VendorStockGenerator.Generate(new RngService(0), catalog, slotCount: 10);
        Assert.Equal(3, stock.Count);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Generate_ZeroOrNegativeSlots_ReturnsEmpty(int slots)
    {
        var stock = VendorStockGenerator.Generate(new RngService(0), Catalog(4), slots);
        Assert.Empty(stock);
    }

    [Fact]
    public void Generate_EmptyCatalog_ReturnsEmpty()
    {
        var stock = VendorStockGenerator.Generate(new RngService(0), Array.Empty<ConsumableDefinition>(), slotCount: 3);
        Assert.Empty(stock);
    }

    [Fact]
    public void Generate_PriceMatchesCatalogEntry()
    {
        var catalog = Catalog(4);
        var byId = catalog.ToDictionary(c => c.Id);
        var stock = VendorStockGenerator.Generate(new RngService(11), catalog, slotCount: 3);
        foreach (var entry in stock)
            Assert.Equal(byId[entry.ConsumableId].PriceCredits, entry.PriceCredits);
    }

    [Fact]
    public void Generate_AgainstShippedCatalog_PicksRealConsumables()
    {
        // Smoke test against the real ConsumableCatalog so a registry edit
        // that breaks the M4 vendor surfaces here, not at scene-load.
        var stock = VendorStockGenerator.Generate(
            new RngService(2026),
            ConsumableCatalog.All,
            slotCount: 2);

        Assert.Equal(2, stock.Count);
        foreach (var entry in stock)
            Assert.NotNull(ConsumableCatalog.FindById(entry.ConsumableId));
    }

    [Fact]
    public void Generate_FullCatalogPick_ReturnsEveryEntry()
    {
        var catalog = Catalog(4);
        var stock = VendorStockGenerator.Generate(new RngService(99), catalog, slotCount: 4);
        var got = stock.Select(e => e.ConsumableId).ToHashSet();
        var expected = catalog.Select(c => c.Id).ToHashSet();
        Assert.Equal(expected, got);
    }
}
