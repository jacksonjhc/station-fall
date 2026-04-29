using Stationfall.Core.ProcGen;
using Xunit;

namespace Stationfall.Tests.ProcGen;

// M5-3: full-invariant battery at scale. Single-seed tests in
// DungeonGeneratorTests stay as targeted fast-fail signals; this suite is the
// wide net that catches drift across the seed space and option combinations.
//
// Covers the PLANNING.md § Dungeon Generation invariants plus the M5-2
// typing/locking guarantees:
//
//   - Validator passes (bidirectional doors, no orphans, type symmetry)
//   - Room count within [MinRoomCount, MaxRoomCount]
//   - Every room reachable from Entry
//   - No two rooms share a grid position
//   - Every door's target sits exactly one cell in the door's direction
//   - Exactly one Entry (at origin) and one Boss
//   - Boss is a leaf at max BFS depth from Entry
//   - When PlaceBossKeyLock: boss approach is the only KeyLocked door
//   - When PlaceBossKeyLock: Item / Vendor / key rooms reachable via Open doors;
//     Boss UN-reachable via Open doors (chokepoint enforced)
//   - Item / Vendor counts match options (capped at available pool)
//   - Exactly one ContainsKey room when locks placed, zero otherwise
//   - No RoomType.Empty emitted (M5-2 promotes them all to Combat)
//   - All rooms tagged with options.ContentTier
public class DungeonGeneratorInvariantsTests
{
    [Fact]
    public void DefaultOptions_HoldsInvariants_Across1000Seeds()
    {
        AssertInvariantSweep(seedCount: 1000, new DungeonGeneratorOptions());
    }

    [Theory]
    [InlineData(4, 4)]
    [InlineData(5, 5)]
    [InlineData(6, 8)]
    [InlineData(10, 12)]
    [InlineData(8, 16)]
    public void RoomCountVariations_HoldInvariants(int min, int max)
    {
        AssertInvariantSweep(seedCount: 200,
            new DungeonGeneratorOptions(MinRoomCount: min, MaxRoomCount: max));
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(0.25)]
    [InlineData(0.5)]
    [InlineData(0.75)]
    [InlineData(1.0)]
    public void BackEdgeProbabilityVariations_HoldInvariants(double p)
    {
        AssertInvariantSweep(seedCount: 200,
            new DungeonGeneratorOptions(BackEdgeProbability: p));
    }

    [Fact]
    public void LocksDisabled_HoldsInvariants_Across500Seeds()
    {
        // PlaceBossKeyLock=false relaxes MinRoomCount>=3, so re-test the smallest
        // viable layout (entry + boss) in this mode too.
        AssertInvariantSweep(seedCount: 500,
            new DungeonGeneratorOptions(MinRoomCount: 2, MaxRoomCount: 6, PlaceBossKeyLock: false));
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(2, 1)]
    [InlineData(1, 2)]
    [InlineData(3, 3)]
    public void BranchCountVariations_HoldInvariants(int items, int vendors)
    {
        // Branch counts that may exceed the non-reserved pool — invariant code
        // expects the generator to cap silently rather than fail.
        AssertInvariantSweep(seedCount: 200,
            new DungeonGeneratorOptions(ItemRoomCount: items, VendorRoomCount: vendors));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void ToolPedestalCountVariations_HoldInvariants(int pedestals)
    {
        // Sector 1 ships pedestals=1 (grapple grant); Sector 2+ may set 0
        // when the run already carries the grapple. Generator caps silently
        // when pedestals exceed the non-reserved pool.
        AssertInvariantSweep(seedCount: 200,
            new DungeonGeneratorOptions(ToolPedestalRoomCount: pedestals));
    }

    [Fact]
    public void Validate_RejectsMinRoomCountBelowThreeWhenLockEnabled()
    {
        // The lock chokepoint needs entry + key room + boss = 3 rooms minimum.
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            DungeonGenerator.Generate(0, new DungeonGeneratorOptions(MinRoomCount: 2, MaxRoomCount: 5)));
    }

    // ----- Sweep harness -----

    private const int FailureCap = 50;

    private static void AssertInvariantSweep(int seedCount, DungeonGeneratorOptions options)
    {
        var failures = new List<string>();
        for (var seed = 0; seed < seedCount; seed++)
        {
            DungeonLayout layout;
            try
            {
                layout = DungeonGenerator.Generate(seed, options);
            }
            catch (Exception ex)
            {
                failures.Add($"seed {seed}: threw {ex.GetType().Name}: {ex.Message}");
                if (failures.Count >= FailureCap) break;
                continue;
            }

            foreach (var failure in EnumerateInvariantFailures(layout, options))
            {
                failures.Add($"seed {seed}: {failure}");
                if (failures.Count >= FailureCap) break;
            }
            if (failures.Count >= FailureCap) break;
        }

        Assert.True(failures.Count == 0,
            $"{failures.Count} invariant failure(s) across {seedCount} seeds:\n  "
            + string.Join("\n  ", failures));
    }

    // ----- Invariant battery -----

    private static IEnumerable<string> EnumerateInvariantFailures(DungeonLayout layout, DungeonGeneratorOptions options)
    {
        var validation = DungeonLayoutValidator.Validate(layout);
        if (!validation.IsValid)
            yield return "validator: " + string.Join("; ", validation.Issues);

        if (layout.RoomCount < options.MinRoomCount)
            yield return $"room count {layout.RoomCount} below MinRoomCount {options.MinRoomCount}";
        if (layout.RoomCount > options.MaxRoomCount)
            yield return $"room count {layout.RoomCount} above MaxRoomCount {options.MaxRoomCount}";

        var allReachable = BfsReachable(layout, layout.EntryRoomId, keylessOnly: false);
        if (allReachable.Count != layout.RoomCount)
            yield return $"only {allReachable.Count}/{layout.RoomCount} rooms reachable from entry";

        var positions = LayoutPositions.ComputeGridPositions(layout);
        var seenPositions = new Dictionary<GridPosition, string>();
        foreach (var (id, pos) in positions)
        {
            if (seenPositions.TryGetValue(pos, out var clash))
                yield return $"rooms '{id}' and '{clash}' share grid position {pos}";
            else seenPositions[pos] = id;
        }

        foreach (var room in layout.Rooms)
        {
            if (!positions.TryGetValue(room.Id, out var pos)) continue;
            foreach (var (dir, door) in room.Doors)
            {
                if (!positions.TryGetValue(door.TargetRoomId, out var tpos)) continue;
                var expected = pos.Offset(dir);
                if (expected != tpos)
                    yield return $"door {room.Id}.{dir} → {door.TargetRoomId}: target at {tpos}, expected {expected}";
            }
        }

        var entries = layout.Rooms.Where(r => r.Type == RoomType.Entry).ToList();
        if (entries.Count != 1) yield return $"expected 1 Entry room, got {entries.Count}";
        if (entries.Count == 1
            && positions.TryGetValue(entries[0].Id, out var entryPos)
            && entryPos != new GridPosition(0, 0))
            yield return $"entry not at origin (at {entryPos})";

        var bosses = layout.Rooms.Where(r => r.Type == RoomType.Boss).ToList();
        if (bosses.Count != 1)
        {
            yield return $"expected 1 Boss room, got {bosses.Count}";
            // Skip the rest — boss-derived invariants need exactly one boss to mean anything.
            yield break;
        }

        var boss = bosses[0];
        if (boss.Doors.Count != 1)
            yield return $"boss '{boss.Id}' has {boss.Doors.Count} door(s), expected 1 (leaf)";

        var distances = BfsDistances(layout, layout.EntryRoomId);
        if (distances.TryGetValue(boss.Id, out var bossDist))
        {
            var maxDist = distances.Values.Max();
            if (bossDist != maxDist)
                yield return $"boss '{boss.Id}' at depth {bossDist}, max depth in layout is {maxDist}";
        }

        var lockedDirected = layout.Rooms.SelectMany(r => r.Doors.Values).Count(d => d.Type == DoorType.KeyLocked);

        if (options.PlaceBossKeyLock)
        {
            if (boss.Doors.Count == 1)
            {
                var (_, approach) = boss.Doors.First();
                if (approach.Type != DoorType.KeyLocked)
                    yield return $"boss approach door type is {approach.Type}, expected KeyLocked";
            }
            if (lockedDirected != 2)
                yield return $"expected exactly 2 KeyLocked door entries (one bidirectional door), got {lockedDirected}";

            var keyless = BfsReachable(layout, layout.EntryRoomId, keylessOnly: true);
            if (keyless.Contains(boss.Id))
                yield return $"boss '{boss.Id}' reachable from entry without crossing the lock — chokepoint missing";

            foreach (var item in layout.Rooms.Where(r => r.Type == RoomType.Item))
                if (!keyless.Contains(item.Id))
                    yield return $"Item room '{item.Id}' not reachable without crossing the lock";
            foreach (var vendor in layout.Rooms.Where(r => r.Type == RoomType.Vendor))
                if (!keyless.Contains(vendor.Id))
                    yield return $"Vendor room '{vendor.Id}' not reachable without crossing the lock";
            foreach (var pedestal in layout.Rooms.Where(r => r.Type == RoomType.ToolPedestal))
                if (!keyless.Contains(pedestal.Id))
                    yield return $"ToolPedestal room '{pedestal.Id}' not reachable without crossing the lock";
            foreach (var keyHolder in layout.Rooms.Where(r => r.ContainsKey))
                if (!keyless.Contains(keyHolder.Id))
                    yield return $"Key room '{keyHolder.Id}' not reachable without crossing the lock";
        }
        else
        {
            if (lockedDirected != 0)
                yield return $"PlaceBossKeyLock=false but {lockedDirected} KeyLocked door entries present";
        }

        // ToolPedestal claims first (pedestal is the M6 grant), then Item,
        // then Vendor. Each cap is silently truncated by remaining pool size.
        var nonReservedPool = Math.Max(0, layout.RoomCount - 2); // entry + boss
        var expectedPedestals = Math.Min(options.ToolPedestalRoomCount, nonReservedPool);
        var afterPedestals = Math.Max(0, nonReservedPool - expectedPedestals);
        var expectedItems = Math.Min(options.ItemRoomCount, afterPedestals);
        var afterItems = Math.Max(0, afterPedestals - expectedItems);
        var expectedVendors = Math.Min(options.VendorRoomCount, afterItems);
        var actualPedestals = layout.Rooms.Count(r => r.Type == RoomType.ToolPedestal);
        var actualItems = layout.Rooms.Count(r => r.Type == RoomType.Item);
        var actualVendors = layout.Rooms.Count(r => r.Type == RoomType.Vendor);
        if (actualPedestals != expectedPedestals)
            yield return $"expected {expectedPedestals} ToolPedestal rooms, got {actualPedestals}";
        if (actualItems != expectedItems)
            yield return $"expected {expectedItems} Item rooms, got {actualItems}";
        if (actualVendors != expectedVendors)
            yield return $"expected {expectedVendors} Vendor rooms, got {actualVendors}";

        var keyHolders = layout.Rooms.Count(r => r.ContainsKey);
        var expectedKeys = options.PlaceBossKeyLock && nonReservedPool > 0 ? 1 : 0;
        if (keyHolders != expectedKeys)
            yield return $"expected {expectedKeys} ContainsKey rooms, got {keyHolders}";

        var emptyRooms = layout.Rooms.Count(r => r.Type == RoomType.Empty);
        if (emptyRooms != 0)
            yield return $"expected 0 RoomType.Empty rooms after M5-2 typing, got {emptyRooms}";

        foreach (var room in layout.Rooms)
            if (room.ContentTier != options.ContentTier)
                yield return $"room '{room.Id}' tagged ContentTier.{room.ContentTier}, expected {options.ContentTier}";
    }

    // ----- BFS helpers -----

    private static HashSet<string> BfsReachable(DungeonLayout layout, string fromId, bool keylessOnly)
    {
        // keylessOnly=true: model "what can the player visit before they
        // have a key?". EnemyLocked doors are kill-clear gates, not key
        // gates, so they remain passable — the player just clears the
        // adjacent combat room.
        var seen = new HashSet<string> { fromId };
        var queue = new Queue<string>();
        queue.Enqueue(fromId);
        while (queue.Count > 0)
        {
            var room = layout.GetRoom(queue.Dequeue());
            foreach (var (_, door) in room.Doors)
            {
                if (keylessOnly && IsKeyOrConditionGate(door.Type)) continue;
                if (seen.Add(door.TargetRoomId)) queue.Enqueue(door.TargetRoomId);
            }
        }
        return seen;
    }

    private static bool IsKeyOrConditionGate(DoorType type) =>
        type == DoorType.KeyLocked
        || type == DoorType.ConditionLocked
        || type == DoorType.Secret;

    private static Dictionary<string, int> BfsDistances(DungeonLayout layout, string fromId)
    {
        var dist = new Dictionary<string, int> { [fromId] = 0 };
        var queue = new Queue<string>();
        queue.Enqueue(fromId);
        while (queue.Count > 0)
        {
            var id = queue.Dequeue();
            foreach (var (_, door) in layout.GetRoom(id).Doors)
            {
                if (dist.ContainsKey(door.TargetRoomId)) continue;
                dist[door.TargetRoomId] = dist[id] + 1;
                queue.Enqueue(door.TargetRoomId);
            }
        }
        return dist;
    }
}
