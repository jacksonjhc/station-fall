using Stationfall.Core.Combat;
using Stationfall.Core.Items;
using Xunit;

namespace Stationfall.Tests.Items;

// Test obligations from ROADMAP M7 / PLANNING § M7 synergy chain:
//   - Each passive in isolation
//   - Each pairwise interaction (R+P, R+C, P+C)
//   - Full-stack interaction (R+P+C)
//   - Refrain stack cap
//   - Curtain Call uses existing Slow refresh / no-magnitude-stack rules
//   - Tags remain metadata and do not directly execute behavior
public class M7PassivesTests
{
    private static readonly WeaponDefinition Sword = WeaponDefinition.Sword;

    // -----------------------------------------------------------------
    // Isolation
    // -----------------------------------------------------------------

    [Fact]
    public void NoPassives_LeavesComboModifiersAtWeaponDefault()
    {
        var passives = new ActivePassives();
        var mods = ComboModifiers.Resolve(Sword, passives);

        Assert.Equal(Sword.ComboLength, mods.FinalComboLength);
        Assert.Equal(0, mods.ExtraComboSteps);
        Assert.Equal(ComboFinisherShape.Default, mods.FinisherShape);
        Assert.Equal(1.0f, mods.FinisherDamageMultiplier);
        Assert.Null(mods.FinisherStatus);
    }

    [Fact]
    public void Refrain_Alone_AddsOneComboStep()
    {
        var passives = new ActivePassives();
        passives.TryAdd(PassiveCatalog.Refrain);

        var mods = ComboModifiers.Resolve(Sword, passives);

        Assert.Equal(Sword.ComboLength + 1, mods.FinalComboLength);
        Assert.Equal(1, mods.ExtraComboSteps);
        // Finisher shape / damage / status untouched.
        Assert.Equal(ComboFinisherShape.Default, mods.FinisherShape);
        Assert.Equal(1.0f, mods.FinisherDamageMultiplier);
        Assert.Null(mods.FinisherStatus);
    }

    [Fact]
    public void Pirouette_Alone_SetsFinisher360_ButLeavesLengthAndDamage()
    {
        var passives = new ActivePassives();
        passives.TryAdd(PassiveCatalog.Pirouette);

        var mods = ComboModifiers.Resolve(Sword, passives);

        Assert.Equal(Sword.ComboLength, mods.FinalComboLength);
        Assert.Equal(ComboFinisherShape.Surround360, mods.FinisherShape);
        Assert.Equal(1.0f, mods.FinisherDamageMultiplier);
        Assert.Null(mods.FinisherStatus);
    }

    [Fact]
    public void CurtainCall_Alone_BoostsFinisherDamageAndAppliesSlow()
    {
        var passives = new ActivePassives();
        passives.TryAdd(PassiveCatalog.CurtainCall);

        var mods = ComboModifiers.Resolve(Sword, passives);

        Assert.Equal(Sword.ComboLength, mods.FinalComboLength);
        Assert.Equal(ComboFinisherShape.Default, mods.FinisherShape);
        Assert.Equal(1.5f, mods.FinisherDamageMultiplier);

        Assert.NotNull(mods.FinisherStatus);
        var slow = mods.FinisherStatus!;
        Assert.Equal(StatusKind.Slowed, slow.Kind);
        Assert.Equal(1.5f, slow.DurationSeconds);
        // Sedative Dart profile: −35% move, −20% dodge distance, no attack-rate effect.
        Assert.Equal(0.65f, slow.MoveSpeedMultiplier);
        Assert.Equal(0.80f, slow.DodgeDistanceMultiplier);
        Assert.Equal(1.0f, slow.AttackRateMultiplier);
    }

    // -----------------------------------------------------------------
    // Refrain stack cap
    // -----------------------------------------------------------------

    [Fact]
    public void Refrain_StacksToCapTwo_ButNotBeyond()
    {
        var passives = new ActivePassives();
        Assert.True(passives.TryAdd(PassiveCatalog.Refrain));
        Assert.True(passives.TryAdd(PassiveCatalog.Refrain));
        // 3rd attempt rejected at cap.
        Assert.False(passives.TryAdd(PassiveCatalog.Refrain));

        Assert.Equal(2, passives.GetStackCount(PassiveCatalog.RefrainId));

        var mods = ComboModifiers.Resolve(Sword, passives);
        Assert.Equal(2, mods.ExtraComboSteps);
        Assert.Equal(Sword.ComboLength + 2, mods.FinalComboLength);
    }

    [Fact]
    public void Pirouette_AndCurtainCall_AreNotStackable()
    {
        var passives = new ActivePassives();
        Assert.True(passives.TryAdd(PassiveCatalog.Pirouette));
        Assert.False(passives.TryAdd(PassiveCatalog.Pirouette));
        Assert.True(passives.TryAdd(PassiveCatalog.CurtainCall));
        Assert.False(passives.TryAdd(PassiveCatalog.CurtainCall));
    }

    // -----------------------------------------------------------------
    // Pairwise
    // -----------------------------------------------------------------

    [Fact]
    public void RefrainPlusPirouette_LongerComboWith360Finisher()
    {
        var passives = new ActivePassives();
        passives.TryAdd(PassiveCatalog.Refrain);
        passives.TryAdd(PassiveCatalog.Pirouette);

        var mods = ComboModifiers.Resolve(Sword, passives);

        Assert.Equal(Sword.ComboLength + 1, mods.FinalComboLength);
        Assert.Equal(ComboFinisherShape.Surround360, mods.FinisherShape);
        Assert.Equal(1.0f, mods.FinisherDamageMultiplier);
        Assert.Null(mods.FinisherStatus);
    }

    [Fact]
    public void RefrainPlusCurtainCall_LongerComboThenPayoffFinisher()
    {
        var passives = new ActivePassives();
        passives.TryAdd(PassiveCatalog.Refrain);
        passives.TryAdd(PassiveCatalog.CurtainCall);

        var mods = ComboModifiers.Resolve(Sword, passives);

        Assert.Equal(Sword.ComboLength + 1, mods.FinalComboLength);
        Assert.Equal(ComboFinisherShape.Default, mods.FinisherShape);
        Assert.Equal(1.5f, mods.FinisherDamageMultiplier);
        Assert.NotNull(mods.FinisherStatus);
        Assert.Equal(StatusKind.Slowed, mods.FinisherStatus!.Kind);
    }

    [Fact]
    public void PirouettePlusCurtainCall_RingedEnemiesAllGetPayoffAndSlow()
    {
        // The "load-bearing" pair per PLANNING § M7 synergy chain:
        // 360° finisher + +50% damage + Slow on every ringed enemy. We assert
        // the modifiers carry both finisher upgrades; per-enemy application is
        // a Godot-side broadcast that reads these modifiers.
        var passives = new ActivePassives();
        passives.TryAdd(PassiveCatalog.Pirouette);
        passives.TryAdd(PassiveCatalog.CurtainCall);

        var mods = ComboModifiers.Resolve(Sword, passives);

        Assert.Equal(Sword.ComboLength, mods.FinalComboLength);
        Assert.Equal(ComboFinisherShape.Surround360, mods.FinisherShape);
        Assert.Equal(1.5f, mods.FinisherDamageMultiplier);
        Assert.NotNull(mods.FinisherStatus);
    }

    // -----------------------------------------------------------------
    // Full stack
    // -----------------------------------------------------------------

    [Fact]
    public void AllThree_LongComboWith360PayoffAndSlow()
    {
        var passives = new ActivePassives();
        passives.TryAdd(PassiveCatalog.Refrain);
        passives.TryAdd(PassiveCatalog.Pirouette);
        passives.TryAdd(PassiveCatalog.CurtainCall);

        var mods = ComboModifiers.Resolve(Sword, passives);

        Assert.Equal(Sword.ComboLength + 1, mods.FinalComboLength);
        Assert.Equal(ComboFinisherShape.Surround360, mods.FinisherShape);
        Assert.Equal(1.5f, mods.FinisherDamageMultiplier);
        Assert.NotNull(mods.FinisherStatus);
        Assert.Equal(StatusKind.Slowed, mods.FinisherStatus!.Kind);
        Assert.Equal(1.5f, mods.FinisherStatus.DurationSeconds);
    }

    [Fact]
    public void AllThree_WithRefrainStacked_FiveStepCombo()
    {
        // PLANNING table row "all three": with Refrain at cap, 5-hit combo,
        // 360° finisher, +50% / Slow on every ringed enemy.
        var passives = new ActivePassives();
        passives.TryAdd(PassiveCatalog.Refrain);
        passives.TryAdd(PassiveCatalog.Refrain);
        passives.TryAdd(PassiveCatalog.Pirouette);
        passives.TryAdd(PassiveCatalog.CurtainCall);

        var mods = ComboModifiers.Resolve(Sword, passives);

        Assert.Equal(Sword.ComboLength + 2, mods.FinalComboLength); // 5
        Assert.Equal(ComboFinisherShape.Surround360, mods.FinisherShape);
        Assert.Equal(1.5f, mods.FinisherDamageMultiplier);
        Assert.NotNull(mods.FinisherStatus);
    }

    // -----------------------------------------------------------------
    // ComboResolver — finisher slot honors extended combos
    // -----------------------------------------------------------------

    [Fact]
    public void ComboResolver_FinisherSlot_AlwaysReturnsHeavyStep()
    {
        var passives = new ActivePassives();
        passives.TryAdd(PassiveCatalog.Refrain);
        var mods = ComboModifiers.Resolve(Sword, passives);

        // Body indices 0..2 are light; index 3 (the new finisher) is heavy.
        var step0 = ComboResolver.StepAt(Sword, 0, mods);
        var step1 = ComboResolver.StepAt(Sword, 1, mods);
        var step2 = ComboResolver.StepAt(Sword, 2, mods);
        var stepFinal = ComboResolver.StepAt(Sword, mods.FinisherIndex, mods);

        Assert.False(step0.IsHeavy);
        Assert.False(step1.IsHeavy);
        Assert.False(step2.IsHeavy);
        Assert.True(stepFinal.IsHeavy);
    }

    [Fact]
    public void ComboResolver_NoModifiers_MatchesWeaponSteps()
    {
        var mods = ComboModifiers.Default(Sword);
        for (int i = 0; i < Sword.ComboLength; i++)
        {
            Assert.Equal(Sword.StepAt(i), ComboResolver.StepAt(Sword, i, mods));
        }
    }

    // -----------------------------------------------------------------
    // Slow status pipeline — refresh, no magnitude stack, expiry
    // -----------------------------------------------------------------

    [Fact]
    public void Slow_AppliedOnce_IsActiveUntilExpiryThenClears()
    {
        var tracker = new StatusTracker();
        var slow = StatusEffect.Slow(durationSeconds: 1.5f);

        tracker.Apply(slow, now: 0.0);

        Assert.True(tracker.IsActive(StatusKind.Slowed, now: 0.0));
        Assert.True(tracker.IsActive(StatusKind.Slowed, now: 1.4));
        Assert.False(tracker.IsActive(StatusKind.Slowed, now: 1.5));
        Assert.False(tracker.IsActive(StatusKind.Slowed, now: 5.0));

        Assert.Equal(0.65f, tracker.MoveSpeedMultiplier(now: 0.5));
        Assert.Equal(0.80f, tracker.DodgeDistanceMultiplier(now: 0.5));
        Assert.Equal(1.0f, tracker.AttackRateMultiplier(now: 0.5));
        // After expiry, multipliers return to 1.0.
        Assert.Equal(1.0f, tracker.MoveSpeedMultiplier(now: 2.0));
        Assert.Equal(1.0f, tracker.DodgeDistanceMultiplier(now: 2.0));
    }

    [Fact]
    public void Slow_Reapplied_RefreshesDurationAndDoesNotMagnitudeStack()
    {
        var tracker = new StatusTracker();
        var slow = StatusEffect.Slow(durationSeconds: 1.5f);

        tracker.Apply(slow, now: 0.0);
        // Halfway through, re-apply.
        tracker.Apply(slow, now: 1.0);

        // Refresh: still active at t=2.0 (would have expired at 1.5 without
        // refresh; new ExpireAt = 1.0 + 1.5 = 2.5).
        Assert.True(tracker.IsActive(StatusKind.Slowed, now: 2.0));
        Assert.True(tracker.IsActive(StatusKind.Slowed, now: 2.4));
        Assert.False(tracker.IsActive(StatusKind.Slowed, now: 2.5));

        // No magnitude stack — multipliers stay at single-application values,
        // never multiplied with themselves.
        Assert.Equal(0.65f, tracker.MoveSpeedMultiplier(now: 1.5));
        Assert.Equal(0.80f, tracker.DodgeDistanceMultiplier(now: 1.5));
    }

    [Fact]
    public void Slow_PruneExpired_RemovesDeadEntriesFromActiveDictionary()
    {
        var tracker = new StatusTracker();
        tracker.Apply(StatusEffect.Slow(1.5f), now: 0.0);

        tracker.PruneExpired(now: 5.0);

        Assert.Empty(tracker.Active);
        Assert.False(tracker.IsActive(StatusKind.Slowed, now: 5.0));
    }

    // -----------------------------------------------------------------
    // Tagging — metadata only, never the source of behavior
    // -----------------------------------------------------------------

    [Fact]
    public void M7Passives_CarryDocumentedTags()
    {
        // Per PLANNING § "Tag matrix (slice passives)".
        Assert.Contains(DeliveryTag.Melee, PassiveCatalog.Refrain.DeliveryTags);
        Assert.Contains(DeliveryTag.Combo, PassiveCatalog.Refrain.DeliveryTags);
        Assert.Contains(TriggerTag.Always, PassiveCatalog.Refrain.TriggerTags);
        Assert.DoesNotContain(TriggerTag.OnComboFinisher, PassiveCatalog.Refrain.TriggerTags);

        Assert.Contains(DeliveryTag.Aoe, PassiveCatalog.Pirouette.DeliveryTags);
        Assert.Contains(TriggerTag.OnComboFinisher, PassiveCatalog.Pirouette.TriggerTags);

        Assert.Contains(StatusTag.Slowed, PassiveCatalog.CurtainCall.StatusTags);
        Assert.Contains(TriggerTag.OnComboFinisher, PassiveCatalog.CurtainCall.TriggerTags);
        Assert.Contains(EffectScope.Enemy, PassiveCatalog.CurtainCall.Scopes);
    }

    [Fact]
    public void TagsAlone_DoNotDriveBehavior()
    {
        // Synthetic item that *advertises* every relevant tag (slowed status,
        // melee/combo/aoe delivery, on_combo_finisher trigger) but ships zero
        // ItemEffect. ComboModifiers.Resolve must return weapon defaults — the
        // tag set must not retro-fit Pirouette / Curtain Call behavior. This
        // is the architectural invariant: behavior comes from ItemEffect, not
        // from string-matching against tags.
        var tagOnlyImposter = ItemDefinition.Create(
            id: "test_tags_no_effects",
            displayName: "Tag-Only Imposter",
            description: "All the chips, none of the bite.",
            tier: ItemTier.Common,
            stackCap: 1,
            effects: Array.Empty<ItemEffect>(),
            statusTags: new[] { StatusTag.Slowed },
            deliveryTags: new[] { DeliveryTag.Melee, DeliveryTag.Combo, DeliveryTag.Aoe },
            triggerTags: new[] { TriggerTag.OnComboFinisher },
            roleTags: new[] { RoleTag.Offense },
            scopes: new[] { EffectScope.Enemy, EffectScope.Weapon });

        var passives = new ActivePassives();
        passives.TryAdd(tagOnlyImposter);

        var mods = ComboModifiers.Resolve(Sword, passives);

        Assert.Equal(Sword.ComboLength, mods.FinalComboLength);
        Assert.Equal(ComboFinisherShape.Default, mods.FinisherShape);
        Assert.Equal(1.0f, mods.FinisherDamageMultiplier);
        Assert.Null(mods.FinisherStatus);
    }

    // -----------------------------------------------------------------
    // M7 demo offering
    // -----------------------------------------------------------------

    [Fact]
    public void M7DemoOffering_IsThePassiveChainInOrder()
    {
        Assert.Equal(3, PassiveCatalog.M7DemoOffering.Count);
        Assert.Same(PassiveCatalog.Refrain, PassiveCatalog.M7DemoOffering[0]);
        Assert.Same(PassiveCatalog.Pirouette, PassiveCatalog.M7DemoOffering[1]);
        Assert.Same(PassiveCatalog.CurtainCall, PassiveCatalog.M7DemoOffering[2]);
    }

    [Fact]
    public void FindById_RoundTripsCatalog()
    {
        Assert.Same(PassiveCatalog.Refrain, PassiveCatalog.FindById(PassiveCatalog.RefrainId));
        Assert.Same(PassiveCatalog.Pirouette, PassiveCatalog.FindById(PassiveCatalog.PirouetteId));
        Assert.Same(PassiveCatalog.CurtainCall, PassiveCatalog.FindById(PassiveCatalog.CurtainCallId));
        Assert.Null(PassiveCatalog.FindById("nope"));
    }
}
