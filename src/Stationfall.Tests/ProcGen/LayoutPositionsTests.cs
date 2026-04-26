using Stationfall.Core.ProcGen;
using Xunit;

namespace Stationfall.Tests.ProcGen;

public class LayoutPositionsTests
{
    [Fact]
    public void ComputeGridPositions_PlacesEntryAtOrigin()
    {
        var positions = LayoutPositions.ComputeGridPositions(HandBuiltLayouts.M2Sandbox());
        Assert.Equal(new GridPosition(0, 0), positions[HandBuiltLayouts.EntryRoomId]);
    }

    [Fact]
    public void ComputeGridPositions_FollowsDoorDirections_M2Sandbox()
    {
        var positions = LayoutPositions.ComputeGridPositions(HandBuiltLayouts.M2Sandbox());
        // entry → east → west_hall → east → far_room
        Assert.Equal(new GridPosition(1, 0), positions[HandBuiltLayouts.WestHallRoomId]);
        Assert.Equal(new GridPosition(2, 0), positions[HandBuiltLayouts.FarRoomId]);
    }

    [Fact]
    public void ComputeGridPositions_PlacesEveryReachableRoom()
    {
        var positions = LayoutPositions.ComputeGridPositions(HandBuiltLayouts.M2Sandbox());
        Assert.Equal(3, positions.Count);
    }
}
