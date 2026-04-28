using Stationfall.Core.ProcGen;
using Xunit;

namespace Stationfall.Tests.ProcGen;

public class DungeonGeneratorTests
{
    private static readonly int[] SampleSeeds = { 0, 1, 7, 42, 99, 1337, 2024, -5, int.MaxValue, int.MinValue };

    [Theory]
    [MemberData(nameof(Seeds))]
    public void Generate_IsDeterministicForSameSeed(int seed)
    {
        var a = DungeonGenerator.Generate(seed);
        var b = DungeonGenerator.Generate(seed);

        Assert.Equal(a.RoomCount, b.RoomCount);
        Assert.Equal(a.EntryRoomId, b.EntryRoomId);
        for (var i = 0; i < a.Rooms.Count; i++)
        {
            var ra = a.Rooms[i];
            var rb = b.Rooms[i];
            Assert.Equal(ra.Id, rb.Id);
            Assert.Equal(ra.Type, rb.Type);
            Assert.Equal(ra.TemplateName, rb.TemplateName);
            Assert.Equal(ra.Doors.Count, rb.Doors.Count);
            foreach (var (dir, door) in ra.Doors)
            {
                Assert.True(rb.Doors.TryGetValue(dir, out var doorB));
                Assert.Equal(door.TargetRoomId, doorB.TargetRoomId);
                Assert.Equal(door.Type, doorB.Type);
            }
        }
    }

    [Theory]
    [MemberData(nameof(Seeds))]
    public void Generate_PassesLayoutValidator(int seed)
    {
        var layout = DungeonGenerator.Generate(seed);
        var result = DungeonLayoutValidator.Validate(layout);
        Assert.True(result.IsValid, $"seed {seed}: " + string.Join("; ", result.Issues));
    }

    [Theory]
    [MemberData(nameof(Seeds))]
    public void Generate_RoomCountWithinDefaultBounds(int seed)
    {
        var defaults = new DungeonGeneratorOptions();
        var layout = DungeonGenerator.Generate(seed);
        Assert.InRange(layout.RoomCount, defaults.MinRoomCount, defaults.MaxRoomCount);
    }

    [Theory]
    [MemberData(nameof(Seeds))]
    public void Generate_AllRoomsReachableFromEntry(int seed)
    {
        var layout = DungeonGenerator.Generate(seed);
        var visited = new HashSet<string> { layout.EntryRoomId };
        var queue = new Queue<string>();
        queue.Enqueue(layout.EntryRoomId);
        while (queue.Count > 0)
        {
            var room = layout.GetRoom(queue.Dequeue());
            foreach (var (_, door) in room.Doors)
            {
                if (visited.Add(door.TargetRoomId))
                    queue.Enqueue(door.TargetRoomId);
            }
        }
        Assert.Equal(layout.RoomCount, visited.Count);
    }

    [Theory]
    [MemberData(nameof(Seeds))]
    public void Generate_NoTwoRoomsShareGridPosition(int seed)
    {
        var layout = DungeonGenerator.Generate(seed);
        var positions = LayoutPositions.ComputeGridPositions(layout);

        Assert.Equal(layout.RoomCount, positions.Count);

        var seen = new Dictionary<GridPosition, string>();
        foreach (var (id, pos) in positions)
        {
            Assert.False(seen.TryGetValue(pos, out var clash),
                $"seed {seed}: rooms '{id}' and '{clash}' share position {pos}");
            seen[pos] = id;
        }
    }

    [Theory]
    [MemberData(nameof(Seeds))]
    public void Generate_EntryRoomIsTypedAndAtOrigin(int seed)
    {
        var layout = DungeonGenerator.Generate(seed);
        Assert.Equal(DungeonGenerator.EntryRoomId, layout.EntryRoomId);

        var entry = layout.GetRoom(layout.EntryRoomId);
        Assert.Equal(RoomType.Entry, entry.Type);

        var positions = LayoutPositions.ComputeGridPositions(layout);
        Assert.Equal(new GridPosition(0, 0), positions[layout.EntryRoomId]);
    }

    [Theory]
    [MemberData(nameof(Seeds))]
    public void Generate_AllDoorsAreOpen(int seed)
    {
        // M5-1 doesn't place locks — that's M5-2's responsibility.
        var layout = DungeonGenerator.Generate(seed);
        foreach (var room in layout.Rooms)
            foreach (var (_, door) in room.Doors)
                Assert.Equal(DoorType.Open, door.Type);
    }

    [Theory]
    [MemberData(nameof(Seeds))]
    public void Generate_NonEntryRoomsAreEmptyType(int seed)
    {
        // M5-1 doesn't assign branch-room types — that's M5-2's responsibility.
        var layout = DungeonGenerator.Generate(seed);
        foreach (var room in layout.Rooms)
        {
            if (room.Id == layout.EntryRoomId) continue;
            Assert.Equal(RoomType.Empty, room.Type);
        }
    }

    [Fact]
    public void Generate_DifferentSeedsProduceDifferentLayouts()
    {
        // Stronger than ">1 distinct": across 50 sequential seeds, RNG should produce mostly-unique
        // structures. A regression that, say, ignores the RNG for some step would collapse this count.
        var distinctSignatures = Enumerable.Range(0, 50)
            .Select(s => LayoutSignature(DungeonGenerator.Generate(s)))
            .Distinct()
            .Count();
        Assert.True(distinctSignatures >= 30,
            $"only {distinctSignatures}/50 distinct layouts — RNG barely affecting structure");
    }

    [Fact]
    public void Generate_DefaultOptions_NeverFallsShortAcrossManySeeds()
    {
        // The growth loop has an `if (candidates.Count == 0) break;` escape hatch that would silently
        // ship a layout below MinRoomCount. With default options on an unbounded grid this should
        // never trip — pin that down across a wide seed sweep.
        var defaults = new DungeonGeneratorOptions();
        for (var seed = 0; seed < 500; seed++)
        {
            var layout = DungeonGenerator.Generate(seed);
            Assert.InRange(layout.RoomCount, defaults.MinRoomCount, defaults.MaxRoomCount);
        }
    }

    [Theory]
    [MemberData(nameof(Seeds))]
    public void Generate_DoorsConnectGridAdjacentRooms(int seed)
    {
        // For every door A.dir → B, B's grid position must be exactly one cell offset from A in `dir`.
        // Catches malformed back-edges that claim a direction the geometry doesn't support — BFS
        // placement alone wouldn't notice (it skips already-placed targets), but the cross-check does.
        var options = new DungeonGeneratorOptions(BackEdgeProbability: 1.0);
        var layout = DungeonGenerator.Generate(seed, options);
        var positions = LayoutPositions.ComputeGridPositions(layout);

        foreach (var room in layout.Rooms)
        {
            var pos = positions[room.Id];
            foreach (var (direction, door) in room.Doors)
            {
                var expected = pos.Offset(direction);
                var actual = positions[door.TargetRoomId];
                Assert.True(expected == actual,
                    $"seed {seed}: door {room.Id}.{direction} → {door.TargetRoomId}: expected target at {expected}, got {actual}");
            }
        }
    }

    [Fact]
    public void Generate_FixedRoomCount_HitsExactlyThatCount()
    {
        var options = new DungeonGeneratorOptions(MinRoomCount: 5, MaxRoomCount: 5, BackEdgeProbability: 0.0);
        for (var seed = 0; seed < 25; seed++)
        {
            var layout = DungeonGenerator.Generate(seed, options);
            Assert.Equal(5, layout.RoomCount);
        }
    }

    [Fact]
    public void Generate_NoBackEdges_ProducesTreeEdgeCount()
    {
        // Spanning tree on N rooms has exactly N-1 undirected edges → 2*(N-1) directed door entries.
        var options = new DungeonGeneratorOptions(MinRoomCount: 7, MaxRoomCount: 7, BackEdgeProbability: 0.0);
        for (var seed = 0; seed < 25; seed++)
        {
            var layout = DungeonGenerator.Generate(seed, options);
            var totalDoors = layout.Rooms.Sum(r => r.Doors.Count);
            Assert.Equal(2 * (layout.RoomCount - 1), totalDoors);
        }
    }

    [Fact]
    public void Generate_MaxBackEdges_ProducesAtLeastTreeEdges()
    {
        // With back-edges enabled, edge count must still be at least the spanning tree's.
        var options = new DungeonGeneratorOptions(MinRoomCount: 7, MaxRoomCount: 7, BackEdgeProbability: 1.0);
        for (var seed = 0; seed < 25; seed++)
        {
            var layout = DungeonGenerator.Generate(seed, options);
            var totalDoors = layout.Rooms.Sum(r => r.Doors.Count);
            Assert.True(totalDoors >= 2 * (layout.RoomCount - 1),
                $"seed {seed}: doors {totalDoors} < spanning-tree minimum {2 * (layout.RoomCount - 1)}");
        }
    }

    [Fact]
    public void Generate_BackEdgesActuallyAddCycles_AcrossManySeeds()
    {
        // Probabilistic: with p=1.0 across many seeds, at least one layout has more edges than a tree.
        var options = new DungeonGeneratorOptions(MinRoomCount: 7, MaxRoomCount: 7, BackEdgeProbability: 1.0);
        var foundCycle = false;
        for (var seed = 0; seed < 50 && !foundCycle; seed++)
        {
            var layout = DungeonGenerator.Generate(seed, options);
            var totalDoors = layout.Rooms.Sum(r => r.Doors.Count);
            if (totalDoors > 2 * (layout.RoomCount - 1)) foundCycle = true;
        }
        Assert.True(foundCycle, "no back-edges added across 50 seeds at probability 1.0 — back-edge logic is dead");
    }

    [Fact]
    public void Generate_RejectsMinRoomCountBelowTwo()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            DungeonGenerator.Generate(0, new DungeonGeneratorOptions(MinRoomCount: 1, MaxRoomCount: 5)));
    }

    [Fact]
    public void Generate_RejectsMaxBelowMin()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            DungeonGenerator.Generate(0, new DungeonGeneratorOptions(MinRoomCount: 6, MaxRoomCount: 4)));
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(1.01)]
    public void Generate_RejectsBackEdgeProbabilityOutOfRange(double probability)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            DungeonGenerator.Generate(0, new DungeonGeneratorOptions(BackEdgeProbability: probability)));
    }

    public static IEnumerable<object[]> Seeds() => SampleSeeds.Select(s => new object[] { s });

    private static string LayoutSignature(DungeonLayout layout)
    {
        var parts = layout.Rooms
            .OrderBy(r => r.Id, StringComparer.Ordinal)
            .Select(r =>
            {
                var doorPart = string.Join(",", r.Doors
                    .OrderBy(d => d.Key)
                    .Select(d => $"{d.Key}->{d.Value.TargetRoomId}:{d.Value.Type}"));
                return $"{r.Id}[{r.Type}|{doorPart}]";
            });
        return string.Join(";", parts);
    }
}
