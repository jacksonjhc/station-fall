namespace Stationfall.Core.Combat;

// Maps a combo step index in the modified (Refrain-extended) combo to the
// ComboStep the weapon should execute on that index.
//
// Pattern: a base 3-step combo of (Light, Light, Heavy) extended by Refrain
// inserts more body steps between the existing body and the heavy finisher.
//
//   Base (3):       L0 L1 H
//   +1 step (4):    L0 L1 L1 H     (last body step repeats)
//   +2 steps (5):   L0 L1 L1 L1 H
//
// The finisher slot is always the weapon's last authored step. Body indexes
// past the weapon's last body step clamp to the last body step rather than
// wrapping back to L0 — wrapping would let Refrain spam the wind-up of L0
// repeatedly, which W2 doesn't intend.
public static class ComboResolver
{
    public static ComboStep StepAt(WeaponDefinition weapon, int comboIndex, ComboModifiers modifiers)
    {
        if (modifiers.IsFinisher(comboIndex))
            return weapon.ComboSteps[weapon.ComboLength - 1];

        // Body step. Clamp to the last body index (ComboLength - 2) so any
        // extra steps reuse the final body step rather than wrapping.
        int lastBodyIndex = Math.Max(0, weapon.ComboLength - 2);
        int bodyIndex = Math.Min(comboIndex, lastBodyIndex);
        return weapon.ComboSteps[bodyIndex];
    }
}
