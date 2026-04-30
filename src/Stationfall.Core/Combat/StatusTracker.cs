namespace Stationfall.Core.Combat;

// Per-entity bag of currently-applied statuses. Owned by enemies (and later
// the player, for buffs) and queried each tick to compute speed / dodge
// multipliers.
//
// Refresh / no-magnitude-stack rule (PLANNING § Sedative Dart, applied to all
// Slow sources): re-applying the same StatusKind overwrites the existing
// instance — duration resets to "now + new.DurationSeconds", magnitudes are
// taken from the new application, but they never sum. M7 tests pin this for
// Curtain Call; future "longer Slow" passives slot in by passing a longer
// DurationSeconds, not by stacking.
public class StatusTracker
{
    private readonly Dictionary<StatusKind, ActiveStatus> _active = new();

    public IReadOnlyDictionary<StatusKind, ActiveStatus> Active => _active;

    public void Apply(StatusEffect effect, double now)
    {
        // Refresh: replace any existing entry of the same Kind. Magnitudes
        // come from the incoming application, never summed. Duration resets
        // to now + new.DurationSeconds (ignores remaining time on the old
        // application — refresh, not extend).
        _active[effect.Kind] = new ActiveStatus(
            Effect: effect,
            ExpireAt: now + effect.DurationSeconds);
    }

    public bool IsActive(StatusKind kind, double now)
    {
        if (!_active.TryGetValue(kind, out var status)) return false;
        return now < status.ExpireAt;
    }

    public ActiveStatus? Get(StatusKind kind, double now) =>
        IsActive(kind, now) ? _active[kind] : null;

    // Multiplicative: if more status kinds land, their move multipliers
    // compose. Slow alone yields 0.65; a future Frozen at 0.50 stacked with
    // Slow yields 0.325. Each individual kind still cannot magnitude-stack
    // with itself (that's enforced by Apply replacing same-kind entries).
    public float MoveSpeedMultiplier(double now)
    {
        float m = 1.0f;
        foreach (var (kind, status) in _active)
        {
            if (now < status.ExpireAt) m *= status.Effect.MoveSpeedMultiplier;
        }
        return m;
    }

    public float DodgeDistanceMultiplier(double now)
    {
        float m = 1.0f;
        foreach (var (kind, status) in _active)
        {
            if (now < status.ExpireAt) m *= status.Effect.DodgeDistanceMultiplier;
        }
        return m;
    }

    public float AttackRateMultiplier(double now)
    {
        float m = 1.0f;
        foreach (var (kind, status) in _active)
        {
            if (now < status.ExpireAt) m *= status.Effect.AttackRateMultiplier;
        }
        return m;
    }

    // Drop expired entries. Cheap to skip — IsActive / multipliers already
    // gate on now < ExpireAt — but callers that snapshot Active for save or
    // UI should call this first so the dictionary doesn't carry dead rows.
    public void PruneExpired(double now)
    {
        if (_active.Count == 0) return;
        var dead = new List<StatusKind>();
        foreach (var (kind, status) in _active)
        {
            if (now >= status.ExpireAt) dead.Add(kind);
        }
        foreach (var kind in dead) _active.Remove(kind);
    }
}

public record ActiveStatus(StatusEffect Effect, double ExpireAt);
