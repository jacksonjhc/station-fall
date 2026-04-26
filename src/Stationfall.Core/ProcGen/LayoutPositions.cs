namespace Stationfall.Core.ProcGen;

public static class LayoutPositions
{
    // BFS from entry, placing each connected room one cell in the door's direction.
    // Used for minimap rendering and (later) for layout sanity-checking the generator output.
    public static IReadOnlyDictionary<string, GridPosition> ComputeGridPositions(DungeonLayout layout)
    {
        var result = new Dictionary<string, GridPosition>();
        if (!layout.ContainsRoom(layout.EntryRoomId)) return result;

        var queue = new Queue<string>();
        queue.Enqueue(layout.EntryRoomId);
        result[layout.EntryRoomId] = new GridPosition(0, 0);

        while (queue.Count > 0)
        {
            var roomId = queue.Dequeue();
            var room = layout.GetRoom(roomId);
            var pos = result[roomId];
            foreach (var (direction, door) in room.Doors)
            {
                if (result.ContainsKey(door.TargetRoomId)) continue;
                if (!layout.ContainsRoom(door.TargetRoomId)) continue;
                result[door.TargetRoomId] = pos.Offset(direction);
                queue.Enqueue(door.TargetRoomId);
            }
        }

        return result;
    }
}
