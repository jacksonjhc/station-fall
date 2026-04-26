namespace Stationfall.Core.ProcGen;

public record LayoutValidationResult(bool IsValid, IReadOnlyList<string> Issues)
{
    public static LayoutValidationResult Valid { get; } = new(true, Array.Empty<string>());
}

public static class DungeonLayoutValidator
{
    public static LayoutValidationResult Validate(DungeonLayout layout)
    {
        var issues = new List<string>();

        if (!layout.ContainsRoom(layout.EntryRoomId))
            issues.Add($"entry room '{layout.EntryRoomId}' not in rooms");

        var seenIds = new HashSet<string>();
        foreach (var room in layout.Rooms)
        {
            if (!seenIds.Add(room.Id)) issues.Add($"duplicate room id '{room.Id}'");

            foreach (var (direction, door) in room.Doors)
            {
                if (!layout.TryGetRoom(door.TargetRoomId, out var target))
                {
                    issues.Add($"door {room.Id}.{direction} → '{door.TargetRoomId}' not in rooms");
                    continue;
                }

                // Bidirectionality: target's matching cardinal door must point back to this room.
                if (!target.Doors.TryGetValue(direction.Opposite(), out var back))
                {
                    issues.Add($"door {room.Id}.{direction} → {target.Id} has no return door at {target.Id}.{direction.Opposite()}");
                    continue;
                }
                if (back.TargetRoomId != room.Id)
                    issues.Add($"door {room.Id}.{direction} → {target.Id}: return door points to '{back.TargetRoomId}'");
                if (back.Type != door.Type)
                    issues.Add($"door {room.Id}.{direction} type {door.Type} ≠ {target.Id}.{direction.Opposite()} type {back.Type}");
            }
        }

        return issues.Count == 0
            ? LayoutValidationResult.Valid
            : new LayoutValidationResult(false, issues);
    }
}
