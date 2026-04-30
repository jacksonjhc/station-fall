namespace Stationfall.Core.Items;

// Authored, immutable description of an item the player can pick up.
//
// Tags (StatusTags / DeliveryTags / TriggerTags / RoleTags / Scopes) are
// metadata only — see ItemEffect for the behavior surface. ActivePassives
// reads Effects, never Tags, when computing modifiers.
//
// StackCap > 1 means duplicate pickups stack up to that count. StackCap = 1
// means non-stackable (a duplicate pickup is treated as exhausted by W5's
// pool rules — out-of-scope for M7's deterministic demo offering).
public record ItemDefinition(
    string Id,
    string DisplayName,
    string Description,
    ItemTier Tier,
    int StackCap,
    IReadOnlyList<ItemEffect> Effects,
    IReadOnlyList<StatusTag> StatusTags,
    IReadOnlyList<DeliveryTag> DeliveryTags,
    IReadOnlyList<TriggerTag> TriggerTags,
    IReadOnlyList<RoleTag> RoleTags,
    IReadOnlyList<EffectScope> Scopes)
{
    public static ItemDefinition Create(
        string id,
        string displayName,
        string description,
        ItemTier tier,
        int stackCap,
        IReadOnlyList<ItemEffect> effects,
        IReadOnlyList<StatusTag>? statusTags = null,
        IReadOnlyList<DeliveryTag>? deliveryTags = null,
        IReadOnlyList<TriggerTag>? triggerTags = null,
        IReadOnlyList<RoleTag>? roleTags = null,
        IReadOnlyList<EffectScope>? scopes = null) =>
        new(
            Id: id,
            DisplayName: displayName,
            Description: description,
            Tier: tier,
            StackCap: stackCap,
            Effects: effects,
            StatusTags: statusTags ?? Array.Empty<StatusTag>(),
            DeliveryTags: deliveryTags ?? Array.Empty<DeliveryTag>(),
            TriggerTags: triggerTags ?? Array.Empty<TriggerTag>(),
            RoleTags: roleTags ?? Array.Empty<RoleTag>(),
            Scopes: scopes ?? Array.Empty<EffectScope>());
}
