namespace Stationfall.Core.Rng;

public class RngService
{
    private readonly Random _random;

    public int Seed { get; }

    public RngService(int seed)
    {
        Seed = seed;
        _random = new Random(seed);
    }

    public int NextInt(int minInclusive, int maxExclusive) => _random.Next(minInclusive, maxExclusive);
    public double NextDouble() => _random.NextDouble();
    public bool NextBool() => _random.Next(2) == 0;
}
