namespace Stationfall.Core.ProcGen;

public record DungeonGeneratorOptions(
    int MinRoomCount = 6,
    int MaxRoomCount = 8,
    double BackEdgeProbability = 0.2,
    int ItemRoomCount = 1,
    int VendorRoomCount = 1,
    bool PlaceBossKeyLock = true,
    ContentTier ContentTier = ContentTier.Onboarding)
{
    // Init-only so existing positional construction stays valid; sectors with
    // their own template pools (Medical Wing, etc.) override via `with` syntax.
    public TemplatePool TemplatePool { get; init; } = TemplatePool.Default;

    public void Validate()
    {
        if (MinRoomCount < 2)
            throw new ArgumentOutOfRangeException(nameof(MinRoomCount), MinRoomCount, "MinRoomCount must be >= 2 (need entry + at least one connected room).");
        if (MaxRoomCount < MinRoomCount)
            throw new ArgumentOutOfRangeException(nameof(MaxRoomCount), MaxRoomCount, "MaxRoomCount must be >= MinRoomCount.");
        if (BackEdgeProbability < 0.0 || BackEdgeProbability > 1.0)
            throw new ArgumentOutOfRangeException(nameof(BackEdgeProbability), BackEdgeProbability, "BackEdgeProbability must be in [0, 1].");
        if (ItemRoomCount < 0)
            throw new ArgumentOutOfRangeException(nameof(ItemRoomCount), ItemRoomCount, "ItemRoomCount must be >= 0.");
        if (VendorRoomCount < 0)
            throw new ArgumentOutOfRangeException(nameof(VendorRoomCount), VendorRoomCount, "VendorRoomCount must be >= 0.");
        // Locking the boss approach requires somewhere to put the key. Smallest
        // viable lockable layout: entry + key room + boss = 3 rooms.
        if (PlaceBossKeyLock && MinRoomCount < 3)
            throw new ArgumentOutOfRangeException(nameof(MinRoomCount), MinRoomCount, "MinRoomCount must be >= 3 when PlaceBossKeyLock is true (entry + key room + boss).");
    }
}
