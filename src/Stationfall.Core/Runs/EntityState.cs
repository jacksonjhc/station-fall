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
