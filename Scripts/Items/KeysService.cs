using Godot;
using Stationfall.Core.Items;
using Stationfall.Core.Runs;

namespace Stationfall.Godot.Items;

// Scene-attached singleton that wraps the run's KeyPouch (Core) and fans
// changes out to Godot listeners — pickups call Add, doors call TryConsume,
// the HUD counter and KeyLocked DoorNodes subscribe to CountChanged so
// their visual state stays current.
//
// Mirrors CreditsService 1:1 because the lifecycle and concerns are
// identical. When unique color-coded keys land in M5+, they live in their
// own service (they don't stack and have per-color identity), not as a
// second field on this one.
public partial class KeysService : Node
{
    public const string Group = "keys_service";
    public static KeysService? Instance { get; private set; }

    [Signal] public delegate void CountChangedEventHandler(int count);

    private KeyPouch? _pouch;

    public int Count => _pouch?.Count ?? 0;

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
        _pouch = run.GenericKeys;
        EmitSignal(SignalName.CountChanged, _pouch.Count);
    }

    public void Add(int amount)
    {
        if (_pouch == null || amount <= 0) return;
        _pouch.Add(amount);
        EmitSignal(SignalName.CountChanged, _pouch.Count);
    }

    public bool TryConsume(int amount = 1)
    {
        if (_pouch == null) return false;
        if (!_pouch.TryConsume(amount)) return false;
        EmitSignal(SignalName.CountChanged, _pouch.Count);
        return true;
    }

    public bool CanConsume(int amount = 1) => _pouch?.CanConsume(amount) ?? false;
}
