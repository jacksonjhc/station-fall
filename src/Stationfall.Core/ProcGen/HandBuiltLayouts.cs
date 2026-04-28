namespace Stationfall.Core.ProcGen;

public static class HandBuiltLayouts
{
    public const string EntryRoomId = "entry";
    public const string WestHallRoomId = "west_hall";
    public const string FarRoomId = "far_room";
    public const string VaultRoomId = "vault_room";
    public const string RewardRoomId = "reward_room";
    public const string VendorRoomId = "vendor_room";

    // M4-4 fixture:
    //                                            RewardRoom (chest)
    //                                                │
    //                                              [open]
    //                                                │
    //   Entry ─[open]─ WestHall ─[enemy-locked]─ FarRoom ─[key-locked]─ VaultRoom
    //                                                │
    //                                              [open]
    //                                                │
    //                                            VendorRoom (consumables)
    //
    // Three distinct reward beats off the FarRoom hub: vault (static
    // credits behind a key door), reward (chest behind a free door),
    // vendor (spend credits on consumables behind a free door).
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
                [CardinalDirection.North] = new DoorDescriptor(RewardRoomId, DoorType.Open),
                [CardinalDirection.South] = new DoorDescriptor(VendorRoomId, DoorType.Open),
            });

        var vaultRoom = new RoomDescriptor(
            Id: VaultRoomId,
            Type: RoomType.Empty,
            TemplateName: "VaultRoom",
            Doors: new Dictionary<CardinalDirection, DoorDescriptor>
            {
                [CardinalDirection.West] = new DoorDescriptor(FarRoomId, DoorType.KeyLocked),
            });

        var rewardRoom = new RoomDescriptor(
            Id: RewardRoomId,
            Type: RoomType.Empty,
            TemplateName: "RewardRoom",
            Doors: new Dictionary<CardinalDirection, DoorDescriptor>
            {
                [CardinalDirection.South] = new DoorDescriptor(FarRoomId, DoorType.Open),
            });

        var vendorRoom = new RoomDescriptor(
            Id: VendorRoomId,
            Type: RoomType.Vendor,
            TemplateName: "VendorRoom",
            Doors: new Dictionary<CardinalDirection, DoorDescriptor>
            {
                [CardinalDirection.North] = new DoorDescriptor(FarRoomId, DoorType.Open),
            });

        return new DungeonLayout(
            Rooms: new[] { entry, westHall, farRoom, vaultRoom, rewardRoom, vendorRoom },
            EntryRoomId: EntryRoomId);
    }
}
