namespace Stationfall.Core.Combat;

public record ComboStep(
    int WindupFrames,
    int ActiveFrames,
    int RecoveryFrames,
    int Damage,
    int HitstopTargetMs,
    int HitstopAttackerMs,
    bool IsHeavy
)
{
    public int TotalFrames => WindupFrames + ActiveFrames + RecoveryFrames;

    public const int CancelWindowFrames = 6;
}
