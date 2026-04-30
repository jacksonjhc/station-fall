namespace Stationfall.Core.Combat;

// Pure decision function for "should this swing chain into the next combo
// step at end-of-recovery." Pulled out of PlayerController so the rule —
// and especially the no-press sentinel — is unit-testable.
//
// Window: [recoveryStart - bufferLeadFrames, totalFrames). The entire
// recovery counts; bufferLeadFrames extends the window backwards into
// late-active so a press just before recovery still chains.
//
// Sentinel (and the bug this fixes): the live caller keeps the latest
// in-swing attack-press frame in a single int, with -1 meaning "no press
// captured this swing." If bufferLeadFrames pushes windowStart below 0
// (e.g. 12-frame lead on a 15-frame Sword swing → windowStart = -5), the
// raw range check `bufferedFrame >= windowStart && < windowEnd` accepts
// the sentinel value -1 — every swing then auto-chains with no input.
// ShouldChain explicitly rejects bufferedFrame < 0 first to keep that
// from happening regardless of how generous the lead grows.
public static class ComboInputResolver
{
    public const int NoPress = -1;

    public static bool ShouldChain(int bufferedFrame, int recoveryStart, int totalFrames, int bufferLeadFrames)
    {
        if (bufferedFrame < 0) return false;
        int windowStart = recoveryStart - Math.Max(0, bufferLeadFrames);
        int windowEnd = totalFrames;
        return bufferedFrame >= windowStart && bufferedFrame < windowEnd;
    }
}
