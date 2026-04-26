namespace Stationfall.Core.Entities;

public enum DodgeKind
{
    Roll,
    ShoulderCharge,
}

public record DodgeProfile(
    DodgeKind Kind,
    int StartupFrames,
    int IFrameStartFrame,
    int IFrameEndFrame,
    int RecoveryFrames,
    float DistancePixels,
    float RechargeSeconds,
    int ContactDamage,
    bool KnockbackOnContact
)
{
    public int IFrameDurationFrames => Math.Max(0, IFrameEndFrame - IFrameStartFrame + 1);
    public int TotalFrames => StartupFrames + IFrameDurationFrames + RecoveryFrames;

    public bool HasIFramesAt(int frame) =>
        frame >= IFrameStartFrame && frame <= IFrameEndFrame;

    public static DodgeProfile Roll => new(
        Kind: DodgeKind.Roll,
        StartupFrames: 1,
        IFrameStartFrame: 2,
        IFrameEndFrame: 9,
        RecoveryFrames: 3,
        DistancePixels: 96f,
        RechargeSeconds: 0.6f,
        ContactDamage: 0,
        KnockbackOnContact: false
    );

    public static DodgeProfile ShoulderCharge => new(
        Kind: DodgeKind.ShoulderCharge,
        StartupFrames: 2,
        IFrameStartFrame: 3,
        IFrameEndFrame: 12,
        RecoveryFrames: 4,
        DistancePixels: 128f,
        RechargeSeconds: 1.0f,
        ContactDamage: 1,
        KnockbackOnContact: true
    );
}
