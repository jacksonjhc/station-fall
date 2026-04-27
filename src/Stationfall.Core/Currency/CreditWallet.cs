namespace Stationfall.Core.Currency;

// Run-scoped credit balance. Mutable like RunState — Godot drives the
// add/spend events (enemy death, chest open, vendor purchase) and reads
// Balance into the HUD. Resets to zero on death because RunState itself is
// reconstructed; nothing here persists across runs.
//
// TrySpend is the only spend path. Callers that need a "can afford?" check
// before committing a UI action should call CanAfford — TrySpend is the
// commit and must not be probed speculatively.
public class CreditWallet
{
    public int Balance { get; private set; }

    public void Add(int amount)
    {
        if (amount <= 0) return;
        Balance += amount;
    }

    public bool CanAfford(int amount) => amount >= 0 && Balance >= amount;

    public bool TrySpend(int amount)
    {
        if (amount < 0) return false;
        if (Balance < amount) return false;
        Balance -= amount;
        return true;
    }
}
