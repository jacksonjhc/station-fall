using Stationfall.Core.Currency;
using Stationfall.Core.Entities;
using Stationfall.Core.Items;

namespace Stationfall.Core.Runs;

public class RunState
{
    public Guid RunId { get; } = Guid.NewGuid();
    public PlayerVessel Vessel { get; }
    public int Floor { get; private set; } = 1;
    public int Seed { get; }
    public DungeonState Dungeon { get; } = new();
    public CreditWallet Credits { get; } = new();
    public KeyPouch GenericKeys { get; } = new();
    public IReadOnlyList<string> NarrativeFlags => _narrativeFlags.AsReadOnly();

    private readonly List<string> _narrativeFlags = [];

    // Default ctor picks a fresh per-run seed; explicit ctor is for tests
    // and (later) seed-pinned debug runs from the console.
    public RunState(PlayerVessel vessel) : this(vessel, Random.Shared.Next()) { }
    public RunState(PlayerVessel vessel, int seed)
    {
        Vessel = vessel;
        Seed = seed;
    }

    public void DescendFloor() => Floor++;
    public void AddNarrativeFlag(string flag) => _narrativeFlags.Add(flag);
    public bool HasFlag(string flag) => _narrativeFlags.Contains(flag);
}
