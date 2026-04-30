using Godot;
using Stationfall.Core.Items;
using Stationfall.Core.Runs;

namespace Stationfall.Godot.Items;

// Scene-attached singleton that wraps RunState.Passives (Core) and fans
// changes out to Godot listeners — pedestals call TryAdd, PlayerController
// reads stack counts and resolves ComboModifiers off the bag, HUD chips
// (post-M7) subscribe to PassiveAdded.
//
// Mirrors CreditsService / KeysService 1:1; same lifecycle and binding rules.
// Resolving ComboModifiers happens at attack-enter time inside PlayerController
// so a passive picked up mid-room shows up on the very next swing without a
// service-layer cache to invalidate.
public partial class PassivesService : Node
{
    public const string Group = "passives_service";
    public static PassivesService? Instance { get; private set; }

    [Signal] public delegate void PassiveAddedEventHandler(string passiveId, int stackCount);

    private ActivePassives? _passives;
    private RunState? _runState;

    public ActivePassives Passives => _passives ?? new ActivePassives();
    public bool M7DemoOfferingConsumed => _runState?.M7DemoOfferingConsumed ?? false;

    public override void _Ready()
    {
        Instance = this;
        AddToGroup(Group);
    }

    public override void _ExitTree()
    {
        if (Instance == this) Instance = null;
    }

    public void Bind(RunState run)
    {
        _runState = run;
        _passives = run.Passives;
    }

    // Returns true if the pickup was consumed (count incremented). False at
    // stack cap — pedestal node uses this to keep the prompt available
    // (caller decides whether to mark itself sold-anyway or stay open).
    public bool TryAdd(ItemDefinition definition)
    {
        if (_passives == null) return false;
        bool added = _passives.TryAdd(definition);
        if (added)
        {
            int count = _passives.GetStackCount(definition.Id);
            EmitSignal(SignalName.PassiveAdded, definition.Id, count);
        }
        return added;
    }

    public int GetStackCount(string id) => _passives?.GetStackCount(id) ?? 0;

    public void MarkM7DemoOfferingConsumed() => _runState?.MarkM7DemoOfferingConsumed();
}
