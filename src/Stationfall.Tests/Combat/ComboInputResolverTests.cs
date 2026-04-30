using Stationfall.Core.Combat;
using Xunit;

namespace Stationfall.Tests.Combat;

// Pinning the "one press, one step" contract at the eligibility-check level.
// The live PlayerController holds bufferedFrame as an int with -1 meaning
// "no press captured this swing"; without an explicit guard, a generous
// bufferLeadFrames pushes windowStart below 0 and the sentinel value -1
// satisfies the range, auto-chaining with zero input.
public class ComboInputResolverTests
{
    // Sword L0 reference frames per WeaponDefinition.Sword: windup=4, active=3,
    // recovery=8 → recoveryStart=7, totalFrames=15.
    private const int SwordL0RecoveryStart = 7;
    private const int SwordL0TotalFrames = 15;

    [Theory]
    [InlineData(4)]
    [InlineData(12)]
    [InlineData(30)] // pathological: lead larger than the whole swing
    public void NoPressSentinel_NeverChains(int bufferLead)
    {
        // The bug: with leadFrames=12 on a 15-frame swing, windowStart goes to
        // -5 and the raw range check would accept -1. Guard rejects it.
        Assert.False(ComboInputResolver.ShouldChain(
            bufferedFrame: ComboInputResolver.NoPress,
            recoveryStart: SwordL0RecoveryStart,
            totalFrames: SwordL0TotalFrames,
            bufferLeadFrames: bufferLead));
    }

    [Fact]
    public void NoPressSentinel_NeverChains_AcrossManyShapes()
    {
        for (int recoveryStart = 1; recoveryStart < 40; recoveryStart++)
        for (int totalFrames = recoveryStart; totalFrames < 60; totalFrames += 7)
        for (int lead = 0; lead < 30; lead += 3)
        {
            Assert.False(ComboInputResolver.ShouldChain(-1, recoveryStart, totalFrames, lead));
            Assert.False(ComboInputResolver.ShouldChain(-100, recoveryStart, totalFrames, lead));
        }
    }

    [Theory]
    [InlineData(0, 12, true)]   // press at swing start, generous lead pulls window back to -5 → in
    [InlineData(2, 12, true)]   // mid-windup
    [InlineData(7, 12, true)]   // recovery start
    [InlineData(14, 12, true)]  // last recovery frame
    [InlineData(15, 12, false)] // past total — out
    [InlineData(2, 4, false)]   // tighter lead (4) shifts window to [3,15) — frame 2 too early
    [InlineData(3, 4, true)]    // tighter lead, frame 3 just inside
    [InlineData(6, 0, false)]   // zero lead → window starts at recovery → frame 6 too early
    [InlineData(7, 0, true)]
    public void Window_BoundaryCases(int bufferedFrame, int leadFrames, bool expected)
    {
        Assert.Equal(expected, ComboInputResolver.ShouldChain(
            bufferedFrame, SwordL0RecoveryStart, SwordL0TotalFrames, leadFrames));
    }

    [Fact]
    public void NegativeLead_ClampedToZero()
    {
        // Defensive: misconfigured negative lead must not invert the window.
        Assert.False(ComboInputResolver.ShouldChain(6, SwordL0RecoveryStart, SwordL0TotalFrames, -5));
        Assert.True(ComboInputResolver.ShouldChain(7, SwordL0RecoveryStart, SwordL0TotalFrames, -5));
    }

    [Fact]
    public void OnePressOneStep_BufferConsumptionIsExplicit()
    {
        // Caller contract: after consuming the buffer, reset to NoPress so
        // the next swing starts fresh. This test pins that resetting really
        // does block a re-fire.
        int buffered = ComboInputResolver.NoPress;
        Assert.False(ComboInputResolver.ShouldChain(buffered, 7, 15, 12));

        // Press lands at frame 5 — eligible.
        buffered = 5;
        Assert.True(ComboInputResolver.ShouldChain(buffered, 7, 15, 12));

        // Caller consumes and resets. Subsequent end-of-recovery checks must
        // not chain again until a new press lands.
        buffered = ComboInputResolver.NoPress;
        Assert.False(ComboInputResolver.ShouldChain(buffered, 7, 15, 12));
    }

    [Fact]
    public void HoldingAttack_ProducesNoChainViaThisChannel()
    {
        // Holding attack means Input.IsActionJustPressed returns true on
        // exactly one frame. After that frame the buffer is captured once
        // and not refreshed; consumption resets it to NoPress; subsequent
        // swings see NoPress and don't chain. This test models that flow.
        int buffered = ComboInputResolver.NoPress;
        bool chained;

        // Swing 1: caller captured a single press at frame 0 (the press
        // that started the swing — IsActionJustPressed returned true once).
        // For the *first* swing, that press is what triggered EnterAttack,
        // so the in-swing buffer was reset to NoPress immediately. Verify
        // no chain.
        chained = ComboInputResolver.ShouldChain(buffered, 7, 15, 12);
        Assert.False(chained);

        // Swing 2 wouldn't even start — there's no chain. But if it
        // somehow did, holding the button doesn't refresh the buffer
        // (JustPressed is edge-triggered), so the chain check still fails.
        chained = ComboInputResolver.ShouldChain(buffered, 7, 15, 12);
        Assert.False(chained);
    }
}
