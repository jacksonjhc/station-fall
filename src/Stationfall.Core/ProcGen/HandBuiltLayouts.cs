namespace Stationfall.Core.ProcGen;

public static class HandBuiltLayouts
{
    public const string EntryRoomId = "entry";
    public const string WestHallRoomId = "west_hall";
    public const string FarRoomId = "far_room";
    public const string VaultRoomId = "vault_room";

    // M4-2 fixture: Entry —[open]→ WestHall —[enemy-locked]→ FarRoom —[key-locked]→ VaultRoom.
    // Combat in WestHall drops a key from the patient; that key opens the
    // FarRoom→VaultRoom door. VaultRoom is the reward beat (a few credits +
    // a corpse for narrative seed).
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
                [CardinalDirection.East] = new DoorDescriptor(VaultRoomId, DoorType.KeyLocked),
            });

        var vaultRoom = new RoomDescriptor(
            Id: VaultRoomId,
            Type: RoomType.Empty,
            TemplateName: "VaultRoom",
            Doors: new Dictionary<CardinalDirection, DoorDescriptor>
            {
                [CardinalDirection.West] = new DoorDescriptor(FarRoomId, DoorType.KeyLocked),
            });

        return new DungeonLayout(
            Rooms: new[] { entry, westHall, farRoom, vaultRoom },
            EntryRoomId: EntryRoomId);
    }
}
