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
            Assert.Equal(ra.ContentTier, rb.ContentTier);
            Assert.Equal(ra.ContainsKey, rb.ContainsKey);
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
    public void Generate_LocksOnlyTheBossApproach(int seed)
    {
        // After M5-2, exactly one bidirectional door is KeyLocked (the boss
        // approach). Counting directed entries: that's 2 KeyLocked, rest Open.
        var layout = DungeonGenerator.Generate(seed);
        var locked = 0;
        foreach (var room in layout.Rooms)
            foreach (var (_, door) in room.Doors)
            {
                Assert.True(door.Type == DoorType.Open || door.Type == DoorType.KeyLocked,
                    $"seed {seed}: unexpected door type {door.Type}");
                if (door.Type == DoorType.KeyLocked) locked++;
            }
        Assert.Equal(2, locked);
    }

    [Theory]
    [MemberData(nameof(Seeds))]
    public void Generate_RoomTypesAreEntryBossItemVendorOrCombat(int seed)
    {
        // Default options should produce exactly one each of Entry/Boss/Item/Vendor;
        // every other room is Combat. Empty is reserved — M5-2 doesn't emit it.
        var layout = DungeonGenerator.Generate(seed);
        var counts = new Dictionary<RoomType, int>();
        foreach (var room in layout.Rooms)
            counts[room.Type] = counts.GetValueOrDefault(room.Type) + 1;

        Assert.Equal(1, counts.GetValueOrDefault(RoomType.Entry));
        Assert.Equal(1, counts.GetValueOrDefault(RoomType.Boss));
        Assert.Equal(1, counts.GetValueOrDefault(RoomType.Item));
        Assert.Equal(1, counts.GetValueOrDefault(RoomType.Vendor));
        Assert.Equal(0, counts.GetValueOrDefault(RoomType.Empty));
        Assert.Equal(0, counts.GetValueOrDefault(RoomType.Secret));
        Assert.Equal(0, counts.GetValueOrDefault(RoomType.MidBoss));
        Assert.Equal(0, counts.GetValueOrDefault(RoomType.Narrative));
        var combat = counts.GetValueOrDefault(RoomType.Combat);
        Assert.Equal(layout.RoomCount - 4, combat);
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

    [Theory]
    [MemberData(nameof(Seeds))]
    public void Generate_BossIsAtMaxBfsDistanceFromEntry(int seed)
    {
        var layout = DungeonGenerator.Generate(seed);
        var boss = layout.Rooms.Single(r => r.Type == RoomType.Boss);
        var distances = BfsDistancesIgnoringLocks(layout, layout.EntryRoomId);
        var maxDist = distances.Values.Max();
        Assert.Equal(maxDist, distances[boss.Id]);
    }

    [Theory]
    [MemberData(nameof(Seeds))]
    public void Generate_BossIsALeaf(int seed)
    {
        // The lock has to be a chokepoint — Boss must have exactly one door.
        var layout = DungeonGenerator.Generate(seed);
        var boss = layout.Rooms.Single(r => r.Type == RoomType.Boss);
        Assert.Single(boss.Doors);
    }

    [Theory]
    [MemberData(nameof(Seeds))]
    public void Generate_BossApproachDoorIsKeyLocked(int seed)
    {
        var layout = DungeonGenerator.Generate(seed);
        var boss = layout.Rooms.Single(r => r.Type == RoomType.Boss);
        var (_, approachDoor) = boss.Doors.Single();
        Assert.Equal(DoorType.KeyLocked, approachDoor.Type);
    }

    [Theory]
    [MemberData(nameof(Seeds))]
    public void Generate_ExactlyOneRoomContainsTheKey(int seed)
    {
        var layout = DungeonGenerator.Generate(seed);
        Assert.Equal(1, layout.Rooms.Count(r => r.ContainsKey));
    }

    [Theory]
    [MemberData(nameof(Seeds))]
    public void Generate_KeyRoomIsReachableWithoutCrossingTheLock(int seed)
    {
        // BFS that refuses to traverse KeyLocked doors must still reach the
        // key-bearing room. Otherwise the player can't pick up the key without
        // already having spent it — soft-lock.
        var layout = DungeonGenerator.Generate(seed);
        var keyRoom = layout.Rooms.Single(r => r.ContainsKey);
        var reachable = BfsReachableSkippingLocks(layout, layout.EntryRoomId);
        Assert.Contains(keyRoom.Id, reachable);
    }

    [Theory]
    [MemberData(nameof(Seeds))]
    public void Generate_ItemRoomReachableWithoutCrossingTheLock(int seed)
    {
        var layout = DungeonGenerator.Generate(seed);
        var itemRoom = layout.Rooms.Single(r => r.Type == RoomType.Item);
        var reachable = BfsReachableSkippingLocks(layout, layout.EntryRoomId);
        Assert.Contains(itemRoom.Id, reachable);
    }

    [Theory]
    [MemberData(nameof(Seeds))]
    public void Generate_VendorRoomReachableWithoutCrossingTheLock(int seed)
    {
        var layout = DungeonGenerator.Generate(seed);
        var vendorRoom = layout.Rooms.Single(r => r.Type == RoomType.Vendor);
        var reachable = BfsReachableSkippingLocks(layout, layout.EntryRoomId);
        Assert.Contains(vendorRoom.Id, reachable);
    }

    [Theory]
    [MemberData(nameof(Seeds))]
    public void Generate_BossIsUnreachableWithoutCrossingTheLock(int seed)
    {
        // The whole point of the lock: Boss must be on the far side. If a
        // back-edge slipped through to make Boss reachable via open doors,
        // IsolateBoss is broken.
        var layout = DungeonGenerator.Generate(seed);
        var boss = layout.Rooms.Single(r => r.Type == RoomType.Boss);
        var reachable = BfsReachableSkippingLocks(layout, layout.EntryRoomId);
        Assert.DoesNotContain(boss.Id, reachable);
    }

    [Theory]
    [InlineData(ContentTier.Onboarding)]
    [InlineData(ContentTier.Standard)]
    [InlineData(ContentTier.Escalated)]
    [InlineData(ContentTier.TruePath)]
    public void Generate_TagsEveryRoomWithRequestedContentTier(ContentTier tier)
    {
        var options = new DungeonGeneratorOptions(ContentTier: tier);
        for (var seed = 0; seed < 20; seed++)
        {
            var layout = DungeonGenerator.Generate(seed, options);
            foreach (var room in layout.Rooms)
                Assert.Equal(tier, room.ContentTier);
        }
    }

    [Fact]
    public void Generate_PlaceBossKeyLockFalse_ShipsLockless()
    {
        var options = new DungeonGeneratorOptions(PlaceBossKeyLock: false);
        for (var seed = 0; seed < 20; seed++)
        {
            var layout = DungeonGenerator.Generate(seed, options);
            foreach (var room in layout.Rooms)
                foreach (var (_, door) in room.Doors)
                    Assert.Equal(DoorType.Open, door.Type);
        }
    }

    [Fact]
    public void Generate_DefaultPool_AssignsPerTypeTemplateName()
    {
        var layout = DungeonGenerator.Generate(0);
        foreach (var room in layout.Rooms)
        {
            var expected = TemplatePool.Default.For(room.Type);
            Assert.Contains(room.TemplateName, expected);
        }
    }

    [Fact]
    public void Generate_CustomPoolWithSingleEntry_UsesItForEveryRoomOfThatType()
    {
        var pool = new TemplatePool(new Dictionary<RoomType, IReadOnlyList<string>>
        {
            [RoomType.Entry] = new[] { "Custom_Entry" },
            [RoomType.Combat] = new[] { "Custom_Combat" },
            [RoomType.Item] = new[] { "Custom_Item" },
            [RoomType.Vendor] = new[] { "Custom_Vendor" },
            [RoomType.Boss] = new[] { "Custom_Boss" },
        });
        var options = new DungeonGeneratorOptions { TemplatePool = pool };
        var layout = DungeonGenerator.Generate(7, options);
        foreach (var room in layout.Rooms)
        {
            var expected = "Custom_" + room.Type;
            Assert.Equal(expected, room.TemplateName);
        }
    }

    [Fact]
    public void Generate_CustomPoolWithMultipleEntries_VariesTemplatesAcrossSeeds()
    {
        var pool = new TemplatePool(new Dictionary<RoomType, IReadOnlyList<string>>
        {
            [RoomType.Entry] = new[] { "EntryRoom" },
            [RoomType.Combat] = new[] { "Combat_A", "Combat_B", "Combat_C", "Combat_D" },
            [RoomType.Item] = new[] { "Item_Generic" },
            [RoomType.Vendor] = new[] { "Vendor_Generic" },
            [RoomType.Boss] = new[] { "Boss_Generic" },
        });
        var options = new DungeonGeneratorOptions { TemplatePool = pool };
        var seenCombatTemplates = new HashSet<string>();
        for (var seed = 0; seed < 50; seed++)
        {
            var layout = DungeonGenerator.Generate(seed, options);
            foreach (var room in layout.Rooms)
                if (room.Type == RoomType.Combat) seenCombatTemplates.Add(room.TemplateName);
        }
        // 50 seeds × ~3 combat rooms each = ~150 picks across 4 templates.
        // If RNG drives the choice, we should see all 4 represented.
        Assert.Equal(4, seenCombatTemplates.Count);
    }

    [Fact]
    public void Generate_PoolMissingRequiredType_Throws()
    {
        var pool = new TemplatePool(new Dictionary<RoomType, IReadOnlyList<string>>
        {
            [RoomType.Entry] = new[] { "EntryRoom" },
            // Combat / Item / Vendor / Boss intentionally missing — generation will hit Combat first.
        });
        var options = new DungeonGeneratorOptions { TemplatePool = pool };
        Assert.Throws<InvalidOperationException>(() => DungeonGenerator.Generate(0, options));
    }

    [Fact]
    public void Generate_PoolWithEmptyListForType_Throws()
    {
        var pool = new TemplatePool(new Dictionary<RoomType, IReadOnlyList<string>>
        {
            [RoomType.Entry] = new[] { "EntryRoom" },
            [RoomType.Combat] = Array.Empty<string>(),
            [RoomType.Item] = new[] { "Item_Generic" },
            [RoomType.Vendor] = new[] { "Vendor_Generic" },
            [RoomType.Boss] = new[] { "Boss_Generic" },
        });
        var options = new DungeonGeneratorOptions { TemplatePool = pool };
        Assert.Throws<InvalidOperationException>(() => DungeonGenerator.Generate(0, options));
    }

    [Fact]
    public void Generate_TemplateAssignment_IsDeterministicAcrossPools()
    {
        // Two layouts with the same seed but different multi-entry pools must
        // pick the same INDEX into their respective pools. That keeps template
        // assignment a function of (seed, room order, pool size), not name.
        var poolA = new TemplatePool(new Dictionary<RoomType, IReadOnlyList<string>>
        {
            [RoomType.Entry] = new[] { "E" },
            [RoomType.Combat] = new[] { "C0", "C1", "C2" },
            [RoomType.Item] = new[] { "I" },
            [RoomType.Vendor] = new[] { "V" },
            [RoomType.Boss] = new[] { "B" },
        });
        var poolB = new TemplatePool(new Dictionary<RoomType, IReadOnlyList<string>>
        {
            [RoomType.Entry] = new[] { "E" },
            [RoomType.Combat] = new[] { "X0", "X1", "X2" },
            [RoomType.Item] = new[] { "I" },
            [RoomType.Vendor] = new[] { "V" },
            [RoomType.Boss] = new[] { "B" },
        });
        var a = DungeonGenerator.Generate(42, new DungeonGeneratorOptions { TemplatePool = poolA });
        var b = DungeonGenerator.Generate(42, new DungeonGeneratorOptions { TemplatePool = poolB });
        for (var i = 0; i < a.Rooms.Count; i++)
        {
            if (a.Rooms[i].Type != RoomType.Combat) continue;
            var idxA = poolA.For(RoomType.Combat).ToList().IndexOf(a.Rooms[i].TemplateName);
            var idxB = poolB.For(RoomType.Combat).ToList().IndexOf(b.Rooms[i].TemplateName);
            Assert.Equal(idxA, idxB);
        }
    }

    [Fact]
    public void Generate_RejectsNegativeBranchCounts()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            DungeonGenerator.Generate(0, new DungeonGeneratorOptions(ItemRoomCount: -1)));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            DungeonGenerator.Generate(0, new DungeonGeneratorOptions(VendorRoomCount: -1)));
    }

    public static IEnumerable<object[]> Seeds() => SampleSeeds.Select(s => new object[] { s });

    private static Dictionary<string, int> BfsDistancesIgnoringLocks(DungeonLayout layout, string fromId)
    {
        // Distances over the full door graph — used for the Boss-depth check,
        // where we want to know how far Boss sits in the underlying graph
        // including the locked approach door.
        var dist = new Dictionary<string, int> { [fromId] = 0 };
        var queue = new Queue<string>();
        queue.Enqueue(fromId);
        while (queue.Count > 0)
        {
            var id = queue.Dequeue();
            var room = layout.GetRoom(id);
            foreach (var (_, door) in room.Doors)
            {
                if (dist.ContainsKey(door.TargetRoomId)) continue;
                dist[door.TargetRoomId] = dist[id] + 1;
                queue.Enqueue(door.TargetRoomId);
            }
        }
        return dist;
    }

    private static HashSet<string> BfsReachableSkippingLocks(DungeonLayout layout, string fromId)
    {
        // Reachability over Open doors only — models "what can the player
        // visit before they have a key?".
        var seen = new HashSet<string> { fromId };
        var queue = new Queue<string>();
        queue.Enqueue(fromId);
        while (queue.Count > 0)
        {
            var room = layout.GetRoom(queue.Dequeue());
            foreach (var (_, door) in room.Doors)
            {
                if (door.Type != DoorType.Open) continue;
                if (seen.Add(door.TargetRoomId)) queue.Enqueue(door.TargetRoomId);
            }
        }
        return seen;
    }

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
