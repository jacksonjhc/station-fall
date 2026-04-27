using Godot;
using Stationfall.Core.Currency;
using Stationfall.Core.Runs;

namespace Stationfall.Godot.Items;

// Scene-attached singleton that wraps the run's CreditWallet (Core) and
// fans changes out to Godot listeners. Mirrors the Sfx.Instance / HitBurstPool
// pattern: one node sits in Dungeon.tscn, _Ready wires the Instance, child
// systems (HUD, pickups, vendors) reach in by static accessor.
//
// Why a Godot wrapper at all: the wallet is pure C# and has no signals.
// Pickups and the HUD are Godot nodes that need to fire/listen for change
// events — those events live here, not in Core. Add/TrySpend forward to the
// wallet and emit BalanceChanged on success.
public partial class CreditsService : Node
{
    public const string Group = "credits_service";
    public static CreditsService? Instance { get; private set; }

    [Signal] public delegate void BalanceChangedEventHandler(int balance);

    private CreditWallet? _wallet;

    public int Balance => _wallet?.Balance ?? 0;

    public override void _Ready()
    {
        Instance = this;
        AddToGroup(Group);
    }

    public override void _ExitTree()
    {
        if (Instance == this) Instance = null;
    }

    // Called by DungeonRoot._Ready once the RunState exists. Re-binding is
    // allowed (run restart re-uses the singleton) — emit so the HUD redraws
    // against the new wallet's balance.
    public void Bind(RunState run)
    {
        _wallet = run.Credits;
        EmitSignal(SignalName.BalanceChanged, _wallet.Balance);
    }

    public void Add(int amount)
    {
        if (_wallet == null || amount <= 0) return;
        _wallet.Add(amount);
        EmitSignal(SignalName.BalanceChanged, _wallet.Balance);
    }

    public bool TrySpend(int amount)
    {
        if (_wallet == null) return false;
        if (!_wallet.TrySpend(amount)) return false;
        EmitSignal(SignalName.BalanceChanged, _wallet.Balance);
        return true;
    }

    public bool CanAfford(int amount) => _wallet?.CanAfford(amount) ?? false;
}
