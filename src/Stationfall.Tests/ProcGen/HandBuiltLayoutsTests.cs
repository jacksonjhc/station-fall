using Stationfall.Core.ProcGen;
using Xunit;

namespace Stationfall.Tests.ProcGen;

public class HandBuiltLayoutsTests
{
    [Fact]
    public void M2Sandbox_HasThreeRooms()
    {
        var layout = HandBuiltLayouts.M2Sandbox();
        Assert.Equal(3, layout.RoomCount);
    }

    [Fact]
    public void M2Sandbox_EntryIsEntryType()
    {
        var layout = HandBuiltLayouts.M2Sandbox();
        var entry = layout.GetRoom(layout.EntryRoomId);
        Assert.Equal(RoomType.Entry, entry.Type);
    }

    [Fact]
    public void M2Sandbox_PassesValidation()
    {
        var result = DungeonLayoutValidator.Validate(HandBuiltLayouts.M2Sandbox());
        Assert.True(result.IsValid, string.Join("; ", result.Issues));
    }

    [Fact]
    public void M2Sandbox_EntryToWestHall_IsOpen()
    {
        var layout = HandBuiltLayouts.M2Sandbox();
        var entry = layout.GetRoom(HandBuiltLayouts.EntryRoomId);
        Assert.True(entry.TryGetDoor(CardinalDirection.East, out var door));
        Assert.Equal(HandBuiltLayouts.WestHallRoomId, door.TargetRoomId);
        Assert.Equal(DoorType.Open, door.Type);
    }

    [Fact]
    public void M2Sandbox_WestHallToFarRoom_IsEnemyLocked()
    {
        var layout = HandBuiltLayouts.M2Sandbox();
        var westHall = layout.GetRoom(HandBuiltLayouts.WestHallRoomId);
        Assert.True(westHall.TryGetDoor(CardinalDirection.East, out var door));
        Assert.Equal(HandBuiltLayouts.FarRoomId, door.TargetRoomId);
        Assert.Equal(DoorType.EnemyLocked, door.Type);
    }
}

public class DungeonLayoutValidatorTests
{
    private static RoomDescriptor Room(string id, params (CardinalDirection dir, string target, DoorType type)[] doors)
    {
        var dict = new Dictionary<CardinalDirection, DoorDescriptor>();
        foreach (var (dir, target, type) in doors)
            dict[dir] = new DoorDescriptor(target, type);
        return new RoomDescriptor(id, RoomType.Empty, "Template_" + id, dict);
    }

    [Fact]
    public void Validate_FailsWhenEntryRoomIdMissing()
    {
        var layout = new DungeonLayout(
            Rooms: new[] { Room("a") },
            EntryRoomId: "ghost");
        var result = DungeonLayoutValidator.Validate(layout);
        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_FailsWhenDoorTargetMissing()
    {
        var layout = new DungeonLayout(
            Rooms: new[] { Room("a", (CardinalDirection.East, "b", DoorType.Open)) },
            EntryRoomId: "a");
        var result = DungeonLayoutValidator.Validate(layout);
        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_FailsWhenDoorIsNotBidirectional()
    {
        // a → b (east) but b has no west-door back to a.
        var layout = new DungeonLayout(
            Rooms: new[]
            {
                Room("a", (CardinalDirection.East, "b", DoorType.Open)),
                Room("b"),
            },
            EntryRoomId: "a");
        var result = DungeonLayoutValidator.Validate(layout);
        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_FailsWhenReturnDoorTypeDiffers()
    {
        var layout = new DungeonLayout(
            Rooms: new[]
            {
                Room("a", (CardinalDirection.East, "b", DoorType.Open)),
                Room("b", (CardinalDirection.West, "a", DoorType.EnemyLocked)),
            },
            EntryRoomId: "a");
        var result = DungeonLayoutValidator.Validate(layout);
        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_PassesValidPair()
    {
        var layout = new DungeonLayout(
            Rooms: new[]
            {
                Room("a", (CardinalDirection.East, "b", DoorType.Open)),
                Room("b", (CardinalDirection.West, "a", DoorType.Open)),
            },
            EntryRoomId: "a");
        var result = DungeonLayoutValidator.Validate(layout);
        Assert.True(result.IsValid, string.Join("; ", result.Issues));
    }
}
