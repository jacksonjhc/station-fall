namespace Stationfall.Core.Combat;

public enum WeaponReach
{
    Short,
    Medium,
    Long,
}

public record WeaponDefinition(
    string Name,
    string Adjective,
    WeaponReach Reach,
    int ArcDegrees,
    IReadOnlyList<ComboStep> ComboSteps
)
{
    public int ComboLength => ComboSteps.Count;

    public ComboStep StepAt(int index) => ComboSteps[index % ComboSteps.Count];

    public static WeaponDefinition Sword { get; } = new(
        Name: "Sword",
        Adjective: "clean",
        Reach: WeaponReach.Medium,
        ArcDegrees: 90,
        ComboSteps: new ComboStep[]
        {
            new(WindupFrames: 4, ActiveFrames: 3, RecoveryFrames: 8,  Damage: 1, HitstopTargetMs: 60,  HitstopAttackerMs: 30, IsHeavy: false),
            new(WindupFrames: 4, ActiveFrames: 3, RecoveryFrames: 8,  Damage: 1, HitstopTargetMs: 60,  HitstopAttackerMs: 30, IsHeavy: false),
            new(WindupFrames: 8, ActiveFrames: 4, RecoveryFrames: 14, Damage: 2, HitstopTargetMs: 120, HitstopAttackerMs: 60, IsHeavy: true),
        }
    );

    // Slower variant for the action-adventure / horror cadence test pass —
    // total ~71 frames chained vs Sword's 56. Held in code so old vs slower
    // can be A/B'd live (debug console: `weapon sword_slow`) before either
    // becomes the design-doc default. Damage and hit-stop unchanged so the
    // comparison is purely about timing feel.
    public static WeaponDefinition SwordSlow { get; } = new(
        Name: "Sword (slow)",
        Adjective: "deliberate",
        Reach: WeaponReach.Medium,
        ArcDegrees: 90,
        ComboSteps: new ComboStep[]
        {
            new(WindupFrames: 5, ActiveFrames: 4, RecoveryFrames: 11, Damage: 1, HitstopTargetMs: 60,  HitstopAttackerMs: 30, IsHeavy: false),
            new(WindupFrames: 5, ActiveFrames: 4, RecoveryFrames: 11, Damage: 1, HitstopTargetMs: 60,  HitstopAttackerMs: 30, IsHeavy: false),
            new(WindupFrames: 9, ActiveFrames: 5, RecoveryFrames: 17, Damage: 2, HitstopTargetMs: 120, HitstopAttackerMs: 60, IsHeavy: true),
        }
    );
}
