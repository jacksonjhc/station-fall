namespace Stationfall.Core.ProcGen;

public enum CardinalDirection
{
    North,
    East,
    South,
    West,
}

public static class CardinalDirectionExtensions
{
    public static CardinalDirection Opposite(this CardinalDirection direction) => direction switch
    {
        CardinalDirection.North => CardinalDirection.South,
        CardinalDirection.South => CardinalDirection.North,
        CardinalDirection.East => CardinalDirection.West,
        CardinalDirection.West => CardinalDirection.East,
        _ => throw new ArgumentOutOfRangeException(nameof(direction)),
    };
}
