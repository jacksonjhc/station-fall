using Stationfall.Core.Rng;

namespace Stationfall.Core.ProcGen;

// M5-2 pipeline:
//   1. BuildGraph        — spanning-tree growth + back-edges (M5-1)
//   2. SelectBoss        — deepest BFS room → RoomType.Boss
//   3. IsolateBoss       — strip Boss back-edges so the parent door is the
//                          only approach (otherwise a back-edge defeats the lock)
//   4. LockBossApproach  — flip the parent door to KeyLocked on both sides
//   5. AssignBranchTypes — pick Item / Vendor rooms by RNG; rest become Combat
//   6. PlaceKey          — pick a non-Boss room to hold the run's key pickup
//   7. Materialize       — emit immutable RoomDescriptors
//
// All phases stay pure: deterministic functions of (seed, options).
public static class DungeonGenerator
{
    public const string EntryRoomId = "entry";

    private static readonly CardinalDirection[] AllDirections =
    {
        CardinalDirection.North,
        CardinalDirection.East,
        CardinalDirection.South,
        CardinalDirection.West,
    };

    // Iterating only East/South when adding back-edges visits each adjacent
    // grid pair exactly once, so the RNG draw order is deterministic.
    private static readonly CardinalDirection[] BackEdgeDirections =
    {
        CardinalDirection.East,
        CardinalDirection.South,
    };

    public static DungeonLayout Generate(int seed, DungeonGeneratorOptions? options = null)
    {
        options ??= new DungeonGeneratorOptions();
        options.Validate();

        var rng = new RngService(seed);
        var graph = BuildGraph(rng, options);
        var bossId = SelectBoss(graph);
        graph.Types[bossId] = RoomType.Boss;
        IsolateBoss(graph, bossId);
        if (options.PlaceBossKeyLock) LockBossApproach(graph, bossId);
        AssignBranchTypes(graph, bossId, rng, options);
        // No lock means no key — placing one would orphan a pickup the run
        // never needs. Keeps key count and lock count in lockstep.
        if (options.PlaceBossKeyLock) PlaceKey(graph, bossId, rng);
        AssignTemplates(graph, rng, options);
        return Materialize(graph, options);
    }

    // ----- Internal graph state -----

    private sealed class GraphState
    {
        public List<string> OrderedIds { get; } = new();
        public Dictionary<string, GridPosition> Positions { get; } = new();
        public Dictionary<GridPosition, string> Occupied { get; } = new();
        public Dictionary<string, Dictionary<CardinalDirection, EdgeRef>> Doors { get; } = new();
        public Dictionary<string, RoomType> Types { get; } = new();
        public HashSet<string> KeyRooms { get; } = new();
        public Dictionary<string, string> Templates { get; } = new();
    }

    private readonly record struct EdgeRef(string TargetId, DoorType Type);

    // ----- Phase 1: graph -----

    private static GraphState BuildGraph(RngService rng, DungeonGeneratorOptions options)
    {
        var graph = new GraphState();
        var entryPos = new GridPosition(0, 0);
        graph.OrderedIds.Add(EntryRoomId);
        graph.Positions[EntryRoomId] = entryPos;
        graph.Occupied[entryPos] = EntryRoomId;
        graph.Doors[EntryRoomId] = new Dictionary<CardinalDirection, EdgeRef>();
        graph.Types[EntryRoomId] = RoomType.Entry;

        var targetCount = options.MinRoomCount == options.MaxRoomCount
            ? options.MinRoomCount
            : rng.NextInt(options.MinRoomCount, options.MaxRoomCount + 1);

        var roomCounter = 0;
        while (graph.OrderedIds.Count < targetCount)
        {
            var candidates = CollectGrowthCandidates(graph);
            if (candidates.Count == 0) break;

            var pick = candidates[rng.NextInt(0, candidates.Count)];
            roomCounter++;
            var newId = $"room_{roomCounter}";

            graph.OrderedIds.Add(newId);
            graph.Positions[newId] = pick.Position;
            graph.Occupied[pick.Position] = newId;
            graph.Doors[newId] = new Dictionary<CardinalDirection, EdgeRef>();
            graph.Types[newId] = RoomType.Empty;
            graph.Doors[pick.SourceRoom][pick.Direction] = new EdgeRef(newId, DoorType.Open);
            graph.Doors[newId][pick.Direction.Opposite()] = new EdgeRef(pick.SourceRoom, DoorType.Open);
        }

        AddBackEdges(graph, rng, options.BackEdgeProbability);
        return graph;
    }

    private readonly record struct GrowthCandidate(string SourceRoom, CardinalDirection Direction, GridPosition Position);

    private static List<GrowthCandidate> CollectGrowthCandidates(GraphState graph)
    {
        var candidates = new List<GrowthCandidate>();
        foreach (var roomId in graph.OrderedIds)
        {
            var pos = graph.Positions[roomId];
            var doors = graph.Doors[roomId];
            foreach (var dir in AllDirections)
            {
                if (doors.ContainsKey(dir)) continue;
                var nextPos = pos.Offset(dir);
                if (graph.Occupied.ContainsKey(nextPos)) continue;
                candidates.Add(new GrowthCandidate(roomId, dir, nextPos));
            }
        }
        return candidates;
    }

    private static void AddBackEdges(GraphState graph, RngService rng, double backEdgeProbability)
    {
        if (backEdgeProbability <= 0.0) return;

        foreach (var aId in graph.OrderedIds)
        {
            var aPos = graph.Positions[aId];
            var aDoors = graph.Doors[aId];
            foreach (var dir in BackEdgeDirections)
            {
                if (aDoors.ContainsKey(dir)) continue;
                var neighborPos = aPos.Offset(dir);
                if (!graph.Occupied.TryGetValue(neighborPos, out var bId)) continue;

                if (rng.NextDouble() < backEdgeProbability)
                {
                    aDoors[dir] = new EdgeRef(bId, DoorType.Open);
                    graph.Doors[bId][dir.Opposite()] = new EdgeRef(aId, DoorType.Open);
                }
            }
        }
    }

    // ----- Phase 2: boss selection -----

    // BFS from Entry; pick the room with maximum distance. Tie-break by
    // ordinal-smallest id so the choice is deterministic per seed.
    private static string SelectBoss(GraphState graph)
    {
        var distances = BfsDistances(graph, EntryRoomId);
        string bossId = EntryRoomId;
        int bossDist = -1;
        foreach (var roomId in graph.OrderedIds)
        {
            if (!distances.TryGetValue(roomId, out var d)) continue;
            if (d > bossDist || (d == bossDist && string.CompareOrdinal(roomId, bossId) < 0))
            {
                bossId = roomId;
                bossDist = d;
            }
        }
        return bossId;
    }

    private static Dictionary<string, int> BfsDistances(GraphState graph, string fromId)
    {
        var dist = new Dictionary<string, int> { [fromId] = 0 };
        var queue = new Queue<string>();
        queue.Enqueue(fromId);
        while (queue.Count > 0)
        {
            var id = queue.Dequeue();
            var here = dist[id];
            foreach (var (_, edge) in graph.Doors[id])
            {
                if (dist.ContainsKey(edge.TargetId)) continue;
                dist[edge.TargetId] = here + 1;
                queue.Enqueue(edge.TargetId);
            }
        }
        return dist;
    }

    // ----- Phase 3: boss isolation -----

    // Make Boss a leaf: keep the BFS-parent door, drop everything else.
    // Removing back-edges is safe — the spanning tree alone connects the
    // graph, and the parent door is preserved.
    private static void IsolateBoss(GraphState graph, string bossId)
    {
        var parent = BfsParent(graph, EntryRoomId, bossId);
        if (parent == null) return; // boss == entry, nothing to do

        var bossDoors = graph.Doors[bossId];
        var keepDir = FindDoorDirection(bossDoors, parent);
        if (keepDir == null) return;

        var doorsToCut = new List<CardinalDirection>();
        foreach (var (dir, _) in bossDoors)
            if (dir != keepDir.Value) doorsToCut.Add(dir);

        foreach (var dir in doorsToCut)
        {
            var edge = bossDoors[dir];
            bossDoors.Remove(dir);
            graph.Doors[edge.TargetId].Remove(dir.Opposite());
        }
    }

    // BFS from `fromId`, returning the discovery-parent of `targetId` (or null
    // if target is unreachable / equals from). The first parent found by BFS
    // is the one along the shortest path.
    private static string? BfsParent(GraphState graph, string fromId, string targetId)
    {
        if (fromId == targetId) return null;
        var parent = new Dictionary<string, string?> { [fromId] = null };
        var queue = new Queue<string>();
        queue.Enqueue(fromId);
        while (queue.Count > 0)
        {
            var id = queue.Dequeue();
            foreach (var (_, edge) in graph.Doors[id])
            {
                if (parent.ContainsKey(edge.TargetId)) continue;
                parent[edge.TargetId] = id;
                if (edge.TargetId == targetId) return id;
                queue.Enqueue(edge.TargetId);
            }
        }
        return null;
    }

    private static CardinalDirection? FindDoorDirection(
        Dictionary<CardinalDirection, EdgeRef> doors,
        string targetId)
    {
        foreach (var (dir, edge) in doors)
            if (edge.TargetId == targetId) return dir;
        return null;
    }

    // ----- Phase 4: lock boss approach -----

    private static void LockBossApproach(GraphState graph, string bossId)
    {
        var bossDoors = graph.Doors[bossId];
        if (bossDoors.Count == 0) return;

        // After IsolateBoss the boss has exactly one door (the parent edge).
        var (dirToParent, edgeToParent) = bossDoors.First();
        var parentId = edgeToParent.TargetId;

        bossDoors[dirToParent] = edgeToParent with { Type = DoorType.KeyLocked };
        var parentDoors = graph.Doors[parentId];
        var dirFromParent = dirToParent.Opposite();
        parentDoors[dirFromParent] = parentDoors[dirFromParent] with { Type = DoorType.KeyLocked };
    }

    // ----- Phase 5: branch room types -----

    // Item then Vendor, drawn without replacement from non-Entry / non-Boss
    // rooms. Ordering by OrderedIds keeps the sample space stable per seed.
    // Falls through silently if there aren't enough rooms — small layouts
    // may legitimately ship without a vendor or item room.
    private static void AssignBranchTypes(GraphState graph, string bossId, RngService rng, DungeonGeneratorOptions options)
    {
        var pool = new List<string>();
        foreach (var id in graph.OrderedIds)
            if (id != EntryRoomId && id != bossId) pool.Add(id);

        AssignType(pool, RoomType.Item, options.ItemRoomCount, rng, graph);
        AssignType(pool, RoomType.Vendor, options.VendorRoomCount, rng, graph);

        // Anything still typed Empty becomes Combat. Hub rooms / off-path
        // dead-ends get the same default; M5-4's template pool decides what
        // a Combat room looks like.
        foreach (var id in pool)
            if (graph.Types[id] == RoomType.Empty) graph.Types[id] = RoomType.Combat;
    }

    private static void AssignType(List<string> pool, RoomType type, int count, RngService rng, GraphState graph)
    {
        for (var i = 0; i < count && pool.Count > 0; i++)
        {
            var idx = rng.NextInt(0, pool.Count);
            graph.Types[pool[idx]] = type;
            pool.RemoveAt(idx);
        }
    }

    // ----- Phase 6: key placement -----

    // The locked door is the boss approach (the only locked door in M5-2),
    // so any non-Boss room is by construction reachable without the key.
    // Picking deterministically from the non-Entry / non-Boss pool keeps
    // the key out of the trivial start room.
    private static void PlaceKey(GraphState graph, string bossId, RngService rng)
    {
        var pool = new List<string>();
        foreach (var id in graph.OrderedIds)
            if (id != EntryRoomId && id != bossId) pool.Add(id);
        if (pool.Count == 0) return;
        var pick = pool[rng.NextInt(0, pool.Count)];
        graph.KeyRooms.Add(pick);
    }

    // ----- Phase 7: template assignment -----

    // Each room picks its template name from the pool entry for its RoomType.
    // Pulling RNG draws here (after typing/locking) keeps structural decisions
    // independent of pool size — adding a template to a pool changes scene
    // selection but not the underlying graph.
    private static void AssignTemplates(GraphState graph, RngService rng, DungeonGeneratorOptions options)
    {
        foreach (var roomId in graph.OrderedIds)
        {
            var pool = options.TemplatePool.For(graph.Types[roomId]);
            graph.Templates[roomId] = pool.Count == 1 ? pool[0] : pool[rng.NextInt(0, pool.Count)];
        }
    }

    // ----- Phase 8: materialize -----

    private static DungeonLayout Materialize(GraphState graph, DungeonGeneratorOptions options)
    {
        var rooms = new List<RoomDescriptor>(graph.OrderedIds.Count);
        foreach (var roomId in graph.OrderedIds)
        {
            var doors = new Dictionary<CardinalDirection, DoorDescriptor>();
            foreach (var (dir, edge) in graph.Doors[roomId])
                doors[dir] = new DoorDescriptor(edge.TargetId, edge.Type);

            rooms.Add(new RoomDescriptor(roomId, graph.Types[roomId], graph.Templates[roomId], doors)
            {
                ContentTier = options.ContentTier,
                ContainsKey = graph.KeyRooms.Contains(roomId),
            });
        }
        return new DungeonLayout(rooms, EntryRoomId);
    }
}
