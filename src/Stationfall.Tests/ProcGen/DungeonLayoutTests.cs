using Stationfall.Core.ProcGen;
using Xunit;

namespace Stationfall.Tests.ProcGen;

public class DungeonLayoutTests
{
    [Fact]
    public void GetRoom_ReturnsRoomById()
    {
        var layout = HandBuiltLayouts.M2Sandbox();
        Assert.Equal(HandBuiltLayouts.EntryRoomId, layout.GetRoom(HandBuiltLayouts.EntryRoomId).Id);
    }

    [Fact]
    public void TryGetRoom_ReturnsFalse_WhenIdMissing()
    {
        var layout = HandBuiltLayouts.M2Sandbox();
        Assert.False(layout.TryGetRoom("does_not_exist", out _));
    }

    [Fact]
    public void RoomCount_ReflectsConstructor()
    {
        var layout = HandBuiltLayouts.M2Sandbox();
        Assert.Equal(4, layout.RoomCount);
    }
}

public class CardinalDirectionTests
{
    [Theory]
    [InlineData(CardinalDirection.North, CardinalDirection.South)]
    [InlineData(CardinalDirection.South, CardinalDirection.North)]
    [InlineData(CardinalDirection.East, CardinalDirection.West)]
    [InlineData(CardinalDirection.West, CardinalDirection.East)]
    public void Opposite_ReturnsExpected(CardinalDirection input, CardinalDirection expected)
    {
        Assert.Equal(expected, input.Opposite());
    }
}
