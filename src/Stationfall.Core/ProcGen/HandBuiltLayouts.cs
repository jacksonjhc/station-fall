namespace Stationfall.Core.ProcGen;

public static class HandBuiltLayouts
{
    public const string EntryRoomId = "entry";
    public const string WestHallRoomId = "west_hall";
    public const string FarRoomId = "far_room";

    // M2 fixture: Entry —[open]→ WestHall —[enemy-locked]→ FarRoom.
    // Enemy-locked door uses the M2 PressurePlate prop as its clear condition until M3 lands real enemies.
    public static DungeonLayout M2Sandbox()
    {
        var entry = new RoomDescriptor(
            Id: EntryRoomId,
            Type: RoomType.Entry,
            TemplateName: "EntryRoom",
            Doors: new Dictionary<CardinalDirection, DoorDescriptor>
            {
                [CardinalDirection.East] = new DoorDescriptor(WestHallRoomId, DoorType.Open),
            });

        var westHall = new RoomDescriptor(
            Id: WestHallRoomId,
            Type: RoomType.Combat,
            TemplateName: "WestHall",
            Doors: new Dictionary<CardinalDirection, DoorDescriptor>
            {
                [CardinalDirection.West] = new DoorDescriptor(EntryRoomId, DoorType.Open),
                [CardinalDirection.East] = new DoorDescriptor(FarRoomId, DoorType.EnemyLocked),
            });

        var farRoom = new RoomDescriptor(
            Id: FarRoomId,
            Type: RoomType.Empty,
            TemplateName: "FarRoom",
            Doors: new Dictionary<CardinalDirection, DoorDescriptor>
            {
                [CardinalDirection.West] = new DoorDescriptor(WestHallRoomId, DoorType.EnemyLocked),
            });

        return new DungeonLayout(
            Rooms: new[] { entry, westHall, farRoom },
            EntryRoomId: EntryRoomId);
    }
}
