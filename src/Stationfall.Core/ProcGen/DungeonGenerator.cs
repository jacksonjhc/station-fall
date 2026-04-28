using Stationfall.Core.Rng;

namespace Stationfall.Core.ProcGen;

// M5-1: graph generation only. Produces a connected room graph on a 2D grid via
// spanning-tree growth + optional back-edges. Room typing, key placement, and
// content tier come in M5-2.
//
// All output rooms are RoomType.Empty (entry is RoomType.Entry); all doors are
// DoorType.Open. Templates use placeholder names that M5-4 / M5-2 will replace.
public static class DungeonGenerator
{
    public const string EntryRoomId = "entry";
    private const string EntryTemplate = "EntryRoom";
    private const string GenericTemplate = "GeneratedRoom";

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
        var targetCount = options.MinRoomCount == options.MaxRoomCount
            ? options.MinRoomCount
            : rng.NextInt(options.MinRoomCount, options.MaxRoomCount + 1);

        var orderedIds = new List<string> { EntryRoomId };
        var positions = new Dictionary<string, GridPosition>
        {
            [EntryRoomId] = new GridPosition(0, 0),
        };
        var occupied = new Dictionary<GridPosition, string>
        {
            [new GridPosition(0, 0)] = EntryRoomId,
        };
        var adjacency = new Dictionary<string, Dictionary<CardinalDirection, string>>
        {
            [EntryRoomId] = new Dictionary<CardinalDirection, string>(),
        };

        var roomCounter = 0;
        while (orderedIds.Count < targetCount)
        {
            var candidates = CollectGrowthCandidates(orderedIds, positions, occupied, adjacency);
            if (candidates.Count == 0) break;

            var pick = candidates[rng.NextInt(0, candidates.Count)];
            roomCounter++;
            var newId = $"room_{roomCounter}";

            orderedIds.Add(newId);
            positions[newId] = pick.Position;
            occupied[pick.Position] = newId;
            adjacency[newId] = new Dictionary<CardinalDirection, string>();
            adjacency[pick.SourceRoom][pick.Direction] = newId;
            adjacency[newId][pick.Direction.Opposite()] = pick.SourceRoom;
        }

        AddBackEdges(orderedIds, positions, occupied, adjacency, rng, options.BackEdgeProbability);

        var rooms = new List<RoomDescriptor>(orderedIds.Count);
        foreach (var roomId in orderedIds)
        {
            var doors = new Dictionary<CardinalDirection, DoorDescriptor>();
            foreach (var (dir, targetId) in adjacency[roomId])
                doors[dir] = new DoorDescriptor(targetId, DoorType.Open);

            var (type, template) = roomId == EntryRoomId
                ? (RoomType.Entry, EntryTemplate)
                : (RoomType.Empty, GenericTemplate);

            rooms.Add(new RoomDescriptor(roomId, type, template, doors));
        }

        return new DungeonLayout(rooms, EntryRoomId);
    }

    private readonly record struct GrowthCandidate(string SourceRoom, CardinalDirection Direction, GridPosition Position);

    private static List<GrowthCandidate> CollectGrowthCandidates(
        List<string> orderedIds,
        Dictionary<string, GridPosition> positions,
        Dictionary<GridPosition, string> occupied,
        Dictionary<string, Dictionary<CardinalDirection, string>> adjacency)
    {
        var candidates = new List<GrowthCandidate>();
        foreach (var roomId in orderedIds)
        {
            var pos = positions[roomId];
            var doors = adjacency[roomId];
            foreach (var dir in AllDirections)
            {
                if (doors.ContainsKey(dir)) continue;
                var nextPos = pos.Offset(dir);
                if (occupied.ContainsKey(nextPos)) continue;
                candidates.Add(new GrowthCandidate(roomId, dir, nextPos));
            }
        }
        return candidates;
    }

    private static void AddBackEdges(
        List<string> orderedIds,
        Dictionary<string, GridPosition> positions,
        Dictionary<GridPosition, string> occupied,
        Dictionary<string, Dictionary<CardinalDirection, string>> adjacency,
        RngService rng,
        double backEdgeProbability)
    {
        if (backEdgeProbability <= 0.0) return;

        foreach (var aId in orderedIds)
        {
            var aPos = positions[aId];
            var aDoors = adjacency[aId];
            foreach (var dir in BackEdgeDirections)
            {
                if (aDoors.ContainsKey(dir)) continue;
                var neighborPos = aPos.Offset(dir);
                if (!occupied.TryGetValue(neighborPos, out var bId)) continue;

                if (rng.NextDouble() < backEdgeProbability)
                {
                    aDoors[dir] = bId;
                    adjacency[bId][dir.Opposite()] = aId;
                }
            }
        }
    }
}
