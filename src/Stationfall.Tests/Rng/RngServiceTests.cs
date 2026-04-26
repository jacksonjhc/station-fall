using Stationfall.Core.Rng;
using Xunit;

namespace Stationfall.Tests.Rng;

public class RngServiceTests
{
    [Fact]
    public void SameSeed_ProducesSameSequence()
    {
        var a = new RngService(42);
        var b = new RngService(42);

        for (var i = 0; i < 100; i++)
        {
            Assert.Equal(a.NextInt(0, 1000), b.NextInt(0, 1000));
        }
    }

    [Fact]
    public void DifferentSeeds_ProduceDifferentSequences()
    {
        var a = new RngService(1);
        var b = new RngService(2);

        var aValues = Enumerable.Range(0, 20).Select(_ => a.NextInt(0, 1_000_000)).ToArray();
        var bValues = Enumerable.Range(0, 20).Select(_ => b.NextInt(0, 1_000_000)).ToArray();

        Assert.NotEqual(aValues, bValues);
    }
}
