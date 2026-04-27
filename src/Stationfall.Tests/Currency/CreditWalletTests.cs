using Stationfall.Core.Currency;
using Xunit;

namespace Stationfall.Tests.Currency;

public class CreditWalletTests
{
    [Fact]
    public void NewWallet_StartsAtZero()
    {
        var wallet = new CreditWallet();
        Assert.Equal(0, wallet.Balance);
    }

    [Fact]
    public void Add_AccumulatesPositiveAmounts()
    {
        var wallet = new CreditWallet();
        wallet.Add(3);
        wallet.Add(5);
        Assert.Equal(8, wallet.Balance);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Add_IgnoresZeroOrNegative(int amount)
    {
        var wallet = new CreditWallet();
        wallet.Add(10);
        wallet.Add(amount);
        Assert.Equal(10, wallet.Balance);
    }

    [Fact]
    public void TrySpend_DeductsAndReturnsTrue_WhenAffordable()
    {
        var wallet = new CreditWallet();
        wallet.Add(20);
        Assert.True(wallet.TrySpend(15));
        Assert.Equal(5, wallet.Balance);
    }

    [Fact]
    public void TrySpend_LeavesBalanceUntouched_OnOverdraft()
    {
        var wallet = new CreditWallet();
        wallet.Add(5);
        Assert.False(wallet.TrySpend(10));
        Assert.Equal(5, wallet.Balance);
    }

    [Fact]
    public void TrySpend_AllowsExactBalance()
    {
        var wallet = new CreditWallet();
        wallet.Add(7);
        Assert.True(wallet.TrySpend(7));
        Assert.Equal(0, wallet.Balance);
    }

    [Fact]
    public void TrySpend_ZeroIsValidNoOp()
    {
        var wallet = new CreditWallet();
        wallet.Add(3);
        Assert.True(wallet.TrySpend(0));
        Assert.Equal(3, wallet.Balance);
    }

    [Fact]
    public void TrySpend_RejectsNegative()
    {
        var wallet = new CreditWallet();
        wallet.Add(3);
        Assert.False(wallet.TrySpend(-1));
        Assert.Equal(3, wallet.Balance);
    }

    [Fact]
    public void CanAfford_MatchesTrySpendOutcome()
    {
        var wallet = new CreditWallet();
        wallet.Add(10);
        Assert.True(wallet.CanAfford(10));
        Assert.True(wallet.CanAfford(0));
        Assert.False(wallet.CanAfford(11));
        Assert.False(wallet.CanAfford(-1));
    }
}
