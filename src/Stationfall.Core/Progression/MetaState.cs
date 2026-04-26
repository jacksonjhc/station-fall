namespace Stationfall.Core.Progression;

// Persistent across runs and across game sessions. Written to disk by
// SaveSerializer; reloaded on launch.
//
// M3 scope: narrative flags only. The save pipe is the point — there's
// nothing else worth persisting yet. Vessel unlocks, mirror upgrades,
// meta-currency balance, and difficulty tier all land in M8 and will
// add fields here. Bumping SaveSchema.Current is required when the
// shape changes incompatibly.
public class MetaState
{
    public HashSet<string> NarrativeFlags { get; init; } = new();

    public bool HasFlag(string flag) => NarrativeFlags.Contains(flag);
    public bool AddFlag(string flag) => NarrativeFlags.Add(flag);
}
