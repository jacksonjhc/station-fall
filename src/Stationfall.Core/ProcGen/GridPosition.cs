namespace Stationfall.Core.ProcGen;

public readonly record struct GridPosition(int X, int Y)
{
    public GridPosition Offset(CardinalDirection direction) => direction switch
    {
        CardinalDirection.North => this with { Y = Y - 1 },
        CardinalDirection.South => this with { Y = Y + 1 },
        CardinalDirection.East => this with { X = X + 1 },
        CardinalDirection.West => this with { X = X - 1 },
        _ => this,
    };
}
