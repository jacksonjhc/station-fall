using Stationfall.Core.Items;

namespace Stationfall.Core.Combat;

// Resolved combo decisions for the current run state. Pure function of
// (weapon, active passives) — no time, no per-hit context. Callers build one
// per attack (cheap; small arrays) so the same passive stack drives the
// hitbox shape, finisher damage, and finisher status pipeline consistently.
//
// Build by calling Resolve. Direct construction is kept available for tests
// that want to pin specific combinations.
public record ComboModifiers(
    int FinalComboLength,
    int ExtraComboSteps,
    ComboFinisherShape FinisherShape,
    float FinisherDamageMultiplier,
    StatusEffect? FinisherStatus)
{
    public int FinisherIndex => FinalComboLength - 1;

    public bool IsFinisher(int comboStepIndex) => comboStepIndex == FinisherIndex;

    public static ComboModifiers Default(WeaponDefinition weapon) => new(
        FinalComboLength: weapon.ComboLength,
        ExtraComboSteps: 0,
        FinisherShape: ComboFinisherShape.Default,
        FinisherDamageMultiplier: 1.0f,
        FinisherStatus: null);

    public static ComboModifiers Resolve(WeaponDefinition weapon, ActivePassives passives)
    {
        int extraSteps = 0;
        var finisherShape = ComboFinisherShape.Default;
        float finisherDamageMultiplier = 1.0f;
        StatusEffect? finisherStatus = null;

        // Sum AddComboStep across stacks (each stack yields one effect entry,
        // so the sum is naturally capped by StackCap × Steps). Multiple
        // ComboFinisherDamageMultiplier entries compose multiplicatively
        // (no current source ships more than one, but the rule is uniform).
        // Surround360 wins over Default if any source asks for it.
        foreach (var effect in passives.EnumerateEffects())
        {
            switch (effect)
            {
                case ItemEffect.AddComboStep add:
                    extraSteps += add.Steps;
                    break;
                case ItemEffect.SetComboFinisherShape shape:
                    if (shape.Shape == ComboFinisherShape.Surround360)
                        finisherShape = ComboFinisherShape.Surround360;
                    break;
                case ItemEffect.ComboFinisherDamageMultiplier mult:
                    finisherDamageMultiplier *= mult.Multiplier;
                    break;
                case ItemEffect.ComboFinisherApplyStatus status:
                    // Multiple status effects on the finisher would compose
                    // (Slow + Bleed + Poison etc.) once W3 / W6 land. M7
                    // ships only Curtain Call, so a single slot holds the
                    // outcome. When more arrive, promote to a list.
                    finisherStatus = status.Status;
                    break;
            }
        }

        return new ComboModifiers(
            FinalComboLength: weapon.ComboLength + extraSteps,
            ExtraComboSteps: extraSteps,
            FinisherShape: finisherShape,
            FinisherDamageMultiplier: finisherDamageMultiplier,
            FinisherStatus: finisherStatus);
    }
}
