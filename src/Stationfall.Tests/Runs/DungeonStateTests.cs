using Stationfall.Core.Runs;
using Xunit;

namespace Stationfall.Tests.Runs;

public class DungeonStateTests
{
    [Fact]
    public void GetOrCreateRoom_StablyReturnsSameInstance()
    {
        var d = new DungeonState();
        var first = d.GetOrCreateRoom("a");
        var second = d.GetOrCreateRoom("a");
        Assert.Same(first, second);
    }

    [Fact]
    public void GetOrCreateRoom_StoresNewlyCreatedRoom()
    {
        var d = new DungeonState();
        d.GetOrCreateRoom("a");
        Assert.True(d.Rooms.ContainsKey("a"));
    }

    [Fact]
    public void IsRoomCleared_DefaultsToFalseForUnknownRoom()
    {
        var d = new DungeonState();
        Assert.False(d.IsRoomCleared("ghost"));
    }

    [Fact]
    public void IsRoomCleared_ReflectsRoomStateFlag()
    {
        var d = new DungeonState();
        d.GetOrCreateRoom("a").IsCleared = true;
        Assert.True(d.IsRoomCleared("a"));
    }

    [Fact]
    public void RoomState_RoundTripsEntityState()
    {
        var rs = new RoomState();
        rs.Entities["plate_1"] = new PressurePlateState(Triggered: true);
        rs.Entities["crate_north"] = new BreakableCrateState(Hp: 1, Destroyed: false);

        Assert.True(rs.Entities.ContainsKey("plate_1"));
        var plate = (PressurePlateState)rs.Entities["plate_1"];
        Assert.True(plate.Triggered);
        var crate = (BreakableCrateState)rs.Entities["crate_north"];
        Assert.Equal(1, crate.Hp);
        Assert.False(crate.Destroyed);
    }

    [Fact]
    public void EntityState_RecordEqualityWorks()
    {
        // Records must compare structurally — the M3 save loader will rely on
        // this when diffing in-memory state against deserialized save state.
        Assert.Equal(new PressurePlateState(true), new PressurePlateState(true));
        Assert.NotEqual(new PressurePlateState(true), new PressurePlateState(false));
        Assert.Equal(new BreakableCrateState(2, false), new BreakableCrateState(2, false));
    }

    [Fact]
    public void EntityState_KindDiscriminatorIsStable()
    {
        // The Kind string is the JSON discriminator. If anyone renames it,
        // existing saves stop loading. Pin the values.
        Assert.Equal("PressurePlate", new PressurePlateState(false).Kind);
        Assert.Equal("BreakableCrate", new BreakableCrateState(0, true).Kind);
    }
}
