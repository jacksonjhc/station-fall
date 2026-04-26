using Stationfall.Core.Entities;

namespace Stationfall.Core.Combat;

// Clone signature: on the hit that drops HP to ≤ threshold, gain a timed buff.
// Won't trigger while staggered; queues until stagger ends. Cooldown counts from buff end.
public static class AdrenalineRushRule
{
    public static AdrenalineRushState OnDamageTaken(
        AdrenalineRushState state,
        EntityStats statsAfterDamage,
        bool isStaggered,
        double nowSeconds,
        AdrenalineRushConfig config)
    {
        if (state.BuffActive) return state;
        if (nowSeconds < state.CooldownEndsAtSeconds) return state;
        if (statsAfterDamage.HpRatio > config.HpThresholdRatio) return state;
        if (statsAfterDamage.Hp <= 0) return state;

        return isStaggered
            ? state with { Queued = true }
            : Activate(state, nowSeconds, config);
    }

    public static AdrenalineRushState OnStaggerEnd(
        AdrenalineRushState state,
        double nowSeconds,
        AdrenalineRushConfig config)
    {
        if (!state.Queued) return state;
        var cleared = state with { Queued = false };
        if (cleared.BuffActive) return cleared;
        if (nowSeconds < cleared.CooldownEndsAtSeconds) return cleared;
        return Activate(cleared, nowSeconds, config);
    }

    public static AdrenalineRushState Tick(
        AdrenalineRushState state,
        double nowSeconds,
        AdrenalineRushConfig config)
    {
        if (state.BuffActive && nowSeconds >= state.BuffEndsAtSeconds)
        {
            return state with
            {
                BuffActive = false,
                BuffEndsAtSeconds = 0,
                CooldownEndsAtSeconds = nowSeconds + config.CooldownSeconds,
            };
        }
        return state;
    }

    private static AdrenalineRushState Activate(
        AdrenalineRushState state,
        double nowSeconds,
        AdrenalineRushConfig config) =>
        state with
        {
            BuffActive = true,
            BuffEndsAtSeconds = nowSeconds + config.BuffDurationSeconds,
            Queued = false,
        };
}
