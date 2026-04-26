using Stationfall.Core.Entities;
using Xunit;

namespace Stationfall.Tests.Entities;

public class EntityStatsTests
{
    private static EntityStats Sample(int hp = 5, int maxHp = 5) =>
        new(MaxHp: maxHp, Hp: hp, MoveSpeed: 120f, AttackPower: 1, AttackRate: 1.5f, Reach: 32, Luck: 0, Armor: 0);

    [Fact]
    public void IsAlive_True_WhenHpAboveZero()
    {
        Assert.True(Sample(hp: 1).IsAlive);
    }

    [Fact]
    public void IsAlive_False_WhenHpZero()
    {
        Assert.False(Sample(hp: 0).IsAlive);
    }

    [Fact]
    public void WithHp_ClampsBelowZeroToZero()
    {
        var stats = Sample().WithHp(-50);
        Assert.Equal(0, stats.Hp);
        Assert.False(stats.IsAlive);
    }

    [Fact]
    public void WithHp_ClampsAboveMaxToMax()
    {
        var stats = Sample(maxHp: 5).WithHp(500);
        Assert.Equal(5, stats.Hp);
    }

    [Fact]
    public void WithHp_LeavesValueUnchanged_WhenWithinRange()
    {
        var stats = Sample(maxHp: 10, hp: 10).WithHp(7);
        Assert.Equal(7, stats.Hp);
    }

    [Fact]
    public void WithMaxHp_ClampsCurrentHpDown_WhenNewMaxIsLower()
    {
        var stats = Sample(maxHp: 10, hp: 8).WithMaxHp(5);
        Assert.Equal(5, stats.MaxHp);
        Assert.Equal(5, stats.Hp);
    }

    [Fact]
    public void WithArmor_FloorsAtZero()
    {
        var stats = Sample().WithArmor(-3);
        Assert.Equal(0, stats.Armor);
    }

    [Theory]
    [InlineData(5, 5, 1.0f)]
    [InlineData(2, 5, 0.4f)]
    [InlineData(0, 5, 0.0f)]
    public void HpRatio_IsCurrentOverMax(int hp, int maxHp, float expected)
    {
        Assert.Equal(expected, Sample(hp: hp, maxHp: maxHp).HpRatio, 3);
    }

    [Fact]
    public void HpRatio_ReturnsZero_WhenMaxHpZero()
    {
        var stats = new EntityStats(MaxHp: 0, Hp: 0, MoveSpeed: 0, AttackPower: 0, AttackRate: 0, Reach: 0, Luck: 0, Armor: 0);
        Assert.Equal(0f, stats.HpRatio);
    }
}
