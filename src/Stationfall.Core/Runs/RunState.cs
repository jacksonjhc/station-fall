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
    public ActivePassives Passives { get; } = new();
    // M7 demo gate: the first item room of the run offers the locked
    // Refrain → Pirouette → Curtain Call chain (one passive per pedestal).
    // Once a chain pedestal is consumed in that room, the gate flips and
    // subsequent item rooms route through real W5 roll weights.
    public bool M7DemoOfferingConsumed { get; private set; }
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
    public void MarkM7DemoOfferingConsumed() => M7DemoOfferingConsumed = true;
}
