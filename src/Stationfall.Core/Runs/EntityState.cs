namespace Stationfall.Core.Runs;

// Per-entity persistent state captured/restored across room teardowns.
// Concrete records add fields for whatever the entity needs to round-trip.
// Kind is the discriminator we'll need when M3 lands JSON serialization.
public abstract record EntityState
{
    public abstract string Kind { get; }
}

public record PressurePlateState(bool Triggered) : EntityState
{
    public override string Kind => "PressurePlate";
}

public record BreakableCrateState(int Hp, bool Destroyed) : EntityState
{
    public override string Kind => "BreakableCrate";
}

// Per-pickup snapshot. ItemKey discriminates pickup type (currently
// "credit" / "key_generic"). Position uses raw floats so Core stays free of
// Godot's Vector2. Collected=true entries are kept across snapshots so a
// once-collected authored pickup (e.g., a vault credit) doesn't respawn on
// every re-entry; for dynamic drops the orphan-restore pass simply skips
// collected entries.
public record PickupState(string ItemKey, int Value, float PositionX, float PositionY, bool Collected) : EntityState
{
    public override string Kind => "Pickup";
}

// Per-spawner record of which spawn markers have already produced a kill
// this run. EnemySpawner reads this to skip dead markers when re-spawning
// after a room re-entry, so partially-cleared rooms don't reset the kill
// count. Live markers are not tracked — they re-spawn at full HP.
public record EnemySpawnerState(IReadOnlyList<string> DeadMarkerIds) : EntityState
{
    public override string Kind => "EnemySpawner";
}

// One-shot chest state. Once opened, stays opened for the rest of the run
// (W7 sticky-open rule applied to props): re-entering the room shows the
// chest in its open state with no loot to give. Only Opened is tracked —
// the loot itself was rolled and emitted as separate pickups, which carry
// their own PickupState entries.
public record ChestState(bool Opened) : EntityState
{
    public override string Kind => "Chest";
}

// Vendor pedestal state. ConsumableId pins the SKU assigned to this
// pedestal at first room entry (vendor stock is generated once per visit
// and never re-rolled). Sold flips on purchase; sold pedestals show empty
// art and reject further interaction. Both fields persist so re-entering
// the vendor room sees the same stock and the same sold/unsold state.
public record VendorPedestalState(string ConsumableId, bool Sold) : EntityState
{
    public override string Kind => "VendorPedestal";
}

// Tool pickup state. Equipped flips on first contact (the player took the
// tool). Persists across room exits so the pedestal stays empty on
// re-entry — the M6 grapple pedestal is one-shot (off-pool, guaranteed via
// generator integration in Phase C).
public record ToolPickupState(bool Equipped) : EntityState
{
    public override string Kind => "ToolPickup";
}
