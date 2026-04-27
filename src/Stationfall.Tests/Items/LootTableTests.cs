using Stationfall.Core.Items;
using Stationfall.Core.Rng;
using Xunit;

namespace Stationfall.Tests.Items;

public class LootTableTests
{
    [Fact]
    public void Roll_SameSeed_ProducesSameSequence()
    {
        var table = new LootTable(
            new LootEntry("credit", Weight: 7, MinAmount: 1, MaxAmount: 3),
            new LootEntry("key_generic", Weight: 2, MinAmount: 1, MaxAmount: 1),
            new LootEntry("nothing", Weight: 1, MinAmount: 0, MaxAmount: 0));

        var rngA = new RngService(42);
        var rngB = new RngService(42);

        for (int i = 0; i < 50; i++)
        {
            var a = table.Roll(rngA);
            var b = table.Roll(rngB);
            Assert.Equal(a, b);
        }
    }

    [Fact]
    public void Roll_EmptyTable_ReturnsNull()
    {
        var table = new LootTable();
        Assert.Null(table.Roll(new RngService(0)));
    }

    [Fact]
    public void Roll_AllZeroWeight_ReturnsNull()
    {
        var table = new LootTable(
            new LootEntry("credit", Weight: 0, MinAmount: 1, MaxAmount: 1),
            new LootEntry("key_generic", Weight: 0, MinAmount: 1, MaxAmount: 1));

        Assert.Null(table.Roll(new RngService(123)));
    }

    [Fact]
    public void Roll_SkipsZeroWeightEntries()
    {
        var table = new LootTable(
            new LootEntry("disabled", Weight: 0, MinAmount: 99, MaxAmount: 99),
            new LootEntry("only", Weight: 5, MinAmount: 1, MaxAmount: 1));

        var rng = new RngService(7);
        for (int i = 0; i < 20; i++)
        {
            var roll = table.Roll(rng);
            Assert.NotNull(roll);
            Assert.Equal("only", roll!.ItemKey);
        }
    }

    [Fact]
    public void Roll_AmountWithinRange()
    {
        var table = new LootTable(new LootEntry("credit", Weight: 1, MinAmount: 2, MaxAmount: 5));
        var rng = new RngService(99);
        for (int i = 0; i < 200; i++)
        {
            var roll = table.Roll(rng);
            Assert.NotNull(roll);
            Assert.InRange(roll!.Amount, 2, 5);
        }
    }

    [Fact]
    public void Roll_FixedAmount_WhenMinEqualsMax()
    {
        var table = new LootTable(new LootEntry("key_generic", 1, 1));
        var rng = new RngService(0);
        for (int i = 0; i < 50; i++)
        {
            var roll = table.Roll(rng);
            Assert.NotNull(roll);
            Assert.Equal(1, roll!.Amount);
        }
    }

    [Fact]
    public void Roll_DistributionRoughlyMatchesWeights()
    {
        // 7:2:1 split → expected proportions ~0.7 / 0.2 / 0.1 over many rolls.
        var table = new LootTable(
            new LootEntry("a", 7, 1),
            new LootEntry("b", 2, 1),
            new LootEntry("c", 1, 1));

        var rng = new RngService(2026);
        var counts = new Dictionary<string, int> { ["a"] = 0, ["b"] = 0, ["c"] = 0 };
        const int Trials = 10_000;
        for (int i = 0; i < Trials; i++)
        {
            var roll = table.Roll(rng);
            counts[roll!.ItemKey]++;
        }

        // ±2% tolerance on each bucket — generous enough to avoid flakiness
        // across .NET runtime updates while still catching a busted distribution.
        Assert.InRange(counts["a"] / (double)Trials, 0.68, 0.72);
        Assert.InRange(counts["b"] / (double)Trials, 0.18, 0.22);
        Assert.InRange(counts["c"] / (double)Trials, 0.08, 0.12);
    }

    [Fact]
    public void Roll_NegativeAmounts_FloorAtZero()
    {
        var table = new LootTable(new LootEntry("credit", Weight: 1, MinAmount: -5, MaxAmount: -1));
        var roll = table.Roll(new RngService(0));
        Assert.NotNull(roll);
        Assert.Equal(0, roll!.Amount);
    }
}
