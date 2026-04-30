namespace Stationfall.Core.Items;

// Run-scoped passive inventory. Tracks which ItemDefinitions the player has
// picked up and how many stacks of each (capped at definition.StackCap).
//
// EnumerateEffects yields each effect once per stack so callers can sum
// magnitudes without re-implementing the cap rule (the cap is applied at
// pickup time via TryAdd).
//
// Mutable like CreditWallet / KeyPouch — Godot pickup nodes call TryAdd as
// the side effect of consumption, no separate "commit" step.
public class ActivePassives
{
    private readonly Dictionary<string, int> _stacks = new();
    private readonly Dictionary<string, ItemDefinition> _definitions = new();

    public IReadOnlyDictionary<string, int> Stacks => _stacks;

    public int GetStackCount(string id) =>
        _stacks.TryGetValue(id, out var count) ? count : 0;

    public bool Contains(string id) => GetStackCount(id) > 0;

    // Returns true if the pickup was consumed (count incremented). Returns
    // false at cap — pool exhaustion / weight transfer is W5's job, not the
    // passive bag's; for M7 the call site treats false as "pedestal stays
    // sold but had no effect."
    public bool TryAdd(ItemDefinition definition)
    {
        int current = GetStackCount(definition.Id);
        if (current >= definition.StackCap) return false;
        _stacks[definition.Id] = current + 1;
        _definitions[definition.Id] = definition;
        return true;
    }

    public IEnumerable<ItemDefinition> Definitions => _definitions.Values;

    // Yield each effect once per stack. AddComboStep with stack count 2
    // shows up twice; SetComboFinisherShape on a non-stackable item shows
    // up once. Callers (ComboModifiers.Resolve) sum / fold accordingly.
    public IEnumerable<ItemEffect> EnumerateEffects()
    {
        foreach (var (id, stacks) in _stacks)
        {
            if (!_definitions.TryGetValue(id, out var def)) continue;
            for (int s = 0; s < stacks; s++)
            {
                foreach (var effect in def.Effects) yield return effect;
            }
        }
    }
}
