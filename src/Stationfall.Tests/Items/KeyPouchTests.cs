using Stationfall.Core.Items;
using Xunit;

namespace Stationfall.Tests.Items;

public class KeyPouchTests
{
    [Fact]
    public void NewPouch_StartsEmpty()
    {
        var pouch = new KeyPouch();
        Assert.Equal(0, pouch.Count);
    }

    [Fact]
    public void Add_AccumulatesPositiveAmounts()
    {
        var pouch = new KeyPouch();
        pouch.Add(2);
        pouch.Add(3);
        Assert.Equal(5, pouch.Count);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Add_IgnoresZeroOrNegative(int amount)
    {
        var pouch = new KeyPouch();
        pouch.Add(2);
        pouch.Add(amount);
        Assert.Equal(2, pouch.Count);
    }

    [Fact]
    public void TryConsume_DefaultsToOne()
    {
        var pouch = new KeyPouch();
        pouch.Add(3);
        Assert.True(pouch.TryConsume());
        Assert.Equal(2, pouch.Count);
    }

    [Fact]
    public void TryConsume_AllowsMultiSpend()
    {
        var pouch = new KeyPouch();
        pouch.Add(5);
        Assert.True(pouch.TryConsume(3));
        Assert.Equal(2, pouch.Count);
    }

    [Fact]
    public void TryConsume_LeavesCountUntouched_WhenInsufficient()
    {
        var pouch = new KeyPouch();
        pouch.Add(1);
        Assert.False(pouch.TryConsume(2));
        Assert.Equal(1, pouch.Count);
    }

    [Fact]
    public void TryConsume_ZeroIsValidNoOp()
    {
        var pouch = new KeyPouch();
        pouch.Add(2);
        Assert.True(pouch.TryConsume(0));
        Assert.Equal(2, pouch.Count);
    }

    [Fact]
    public void TryConsume_RejectsNegative()
    {
        var pouch = new KeyPouch();
        pouch.Add(2);
        Assert.False(pouch.TryConsume(-1));
        Assert.Equal(2, pouch.Count);
    }

    [Fact]
    public void TryConsume_BlocksAtZero()
    {
        var pouch = new KeyPouch();
        Assert.False(pouch.TryConsume());
        Assert.Equal(0, pouch.Count);
    }

    [Fact]
    public void CanConsume_MatchesTryConsumeOutcome()
    {
        var pouch = new KeyPouch();
        pouch.Add(2);
        Assert.True(pouch.CanConsume(2));
        Assert.True(pouch.CanConsume(0));
        Assert.False(pouch.CanConsume(3));
        Assert.False(pouch.CanConsume(-1));
    }
}
