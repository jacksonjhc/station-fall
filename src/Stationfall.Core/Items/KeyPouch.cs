namespace Stationfall.Core.Items;

// Run-scoped generic-key inventory. Keys stack as a single integer (per W7
// door taxonomy: "consumed on use; stack count visible on HUD"). Mutable in
// the same way CreditWallet is — Godot-side events (pickup, door-cross)
// drive the count up or down.
//
// TryConsume is the only spend path. Single-key consumption is the common
// case (one door = one key), but multi-consume is supported for future
// "skeleton key cluster" or "double-locked door" uses.
//
// Color-coded unique keys (sector keycards, Doom-style) are deferred to M5
// alongside procgen — they live in their own type because they don't stack.
public class KeyPouch
{
    public int Count { get; private set; }

    public void Add(int amount)
    {
        if (amount <= 0) return;
        Count += amount;
    }

    public bool CanConsume(int amount) => amount >= 0 && Count >= amount;

    public bool TryConsume(int amount = 1)
    {
        if (amount < 0) return false;
        if (Count < amount) return false;
        Count -= amount;
        return true;
    }
}
