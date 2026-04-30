using Godot;
using Stationfall.Core.Items;
using Stationfall.Core.Runs;
using Stationfall.Godot.Audio;
using Stationfall.Godot.Combat;
using Stationfall.Godot.Persistence;

namespace Stationfall.Godot.Items;

// One pedestal in an item room. Mirrors VendorPedestalNode's interaction
// shape (Area2D range trigger, prompt fade/pulse, SPACE to commit) but
// commit is "TryAdd to ActivePassives" — no credit cost.
//
// PassiveId picks the passive offered. For the M7 demo room the .tscn
// hard-codes Refrain / Pirouette / Curtain Call across three sibling
// pedestals; for normal item rooms (post-M7) DungeonInstantiator overwrites
// PassiveId via a roll against W5 weights at first room entry.
//
// 1-of-3 rule (PLANNING § Item Tagging / Acquisition): claiming any
// pedestal in the room voids the other two. Sibling pedestals listen to a
// "_room_passive_claimed" group broadcast and mark themselves Claimed too,
// so re-entry shows all three desaturated.
public partial class PassivePedestalNode : Node2D, IPersistentEntity
{
    public const string ClaimedGroup = "passive_room_claimed";

    [Signal] public delegate void ClaimedEventHandler(string passiveId);

    [Export] public string EntityId { get; set; } = "";
    [Export] public string PassiveId { get; set; } = PassiveCatalog.RefrainId;
    [Export] public NodePath SpritePath { get; set; } = "Sprite";
    [Export] public NodePath InteractAreaPath { get; set; } = "InteractArea";
    [Export] public NodePath PromptPath { get; set; } = "Prompt";

    private CanvasItem? _sprite;
    private Area2D? _interactArea;
    private Label? _prompt;
    private bool _claimed;
    private bool _playerInRange;
    private double _promptPulseTime;
    private ItemDefinition? _definition;

    public override void _Ready()
    {
        _sprite = GetNodeOrNull<CanvasItem>(SpritePath);
        _interactArea = GetNodeOrNull<Area2D>(InteractAreaPath);
        _prompt = GetNodeOrNull<Label>(PromptPath);

        if (_interactArea != null)
        {
            _interactArea.CollisionLayer = 0;
            _interactArea.CollisionMask = CollisionLayers.PlayerBody;
            _interactArea.BodyEntered += OnBodyEntered;
            _interactArea.BodyExited += OnBodyExited;
        }

        if (_prompt != null) _prompt.Visible = false;

        ResolveDefinition();
        ApplyVisualState();
        UpdatePromptText();
    }

    public override void _Process(double delta)
    {
        if (_prompt != null && _prompt.Visible)
        {
            _promptPulseTime += delta;
            float t = (float)System.Math.Sin(_promptPulseTime * System.Math.PI * 1.5);
            float amp = 1.275f + 0.425f * t;
            _prompt.Modulate = TintForTier(amp, _definition?.Tier ?? ItemTier.Common);
        }

        if (_claimed) return;
        if (!_playerInRange) return;
        if (Input.IsActionJustPressed("interact")) TryClaim();
    }

    private void OnBodyEntered(Node2D body)
    {
        if (!body.IsInGroup("player")) return;
        _playerInRange = true;
        _promptPulseTime = 0;
        if (_prompt != null && !_claimed) _prompt.Visible = true;
    }

    private void OnBodyExited(Node2D body)
    {
        if (!body.IsInGroup("player")) return;
        _playerInRange = false;
        if (_prompt != null) _prompt.Visible = false;
    }

    private void TryClaim()
    {
        if (_claimed) return;
        if (_definition == null) return;

        var service = PassivesService.Instance;
        if (service == null) return;

        if (!service.TryAdd(_definition))
        {
            // At stack cap — cheap denied cue. Pedestal stays open in case
            // the cap rules change at runtime (post-slice pool exhaustion).
            Sfx.Instance?.PlayDamageTaken();
            return;
        }

        // Picked up. Mark this pedestal claimed and broadcast to siblings —
        // 1-of-3 room rule. Siblings (same group, in same room) flip too.
        ClaimSelf();
        BroadcastClaimedToSiblings();
        Sfx.Instance?.PlayDodge();
        service.MarkM7DemoOfferingConsumed();
        EmitSignal(SignalName.Claimed, _definition.Id);
    }

    private void ClaimSelf()
    {
        _claimed = true;
        ApplyVisualState();
    }

    private void BroadcastClaimedToSiblings()
    {
        // Pedestals scope siblings to the same parent (the room scene). A
        // tree-wide group lookup would catch pedestals from a future
        // re-entered item room as well — wrong for the 1-of-3 rule.
        var parent = GetParent();
        if (parent == null) return;
        foreach (var child in parent.GetChildren())
        {
            if (child is PassivePedestalNode sibling && sibling != this)
                sibling.OnSiblingClaimed();
        }
    }

    public void OnSiblingClaimed()
    {
        if (_claimed) return;
        _claimed = true;
        ApplyVisualState();
    }

    private void ResolveDefinition()
    {
        _definition = PassiveCatalog.FindById(PassiveId);
        if (_definition == null)
            GD.PushWarning($"PassivePedestal: unknown PassiveId '{PassiveId}' — pedestal will be inert.");
    }

    private void ApplyVisualState()
    {
        if (_sprite != null)
        {
            _sprite.Modulate = _claimed
                ? new Color(0.45f, 0.45f, 0.5f, 0.55f)
                : ColorForTier(_definition?.Tier ?? ItemTier.Common);
        }
        if (_prompt != null && _claimed) _prompt.Visible = false;
    }

    private void UpdatePromptText()
    {
        if (_prompt == null) return;
        if (_definition == null)
        {
            _prompt.Text = "[unknown passive]";
            return;
        }
        _prompt.Text = $"[SPACE] {_definition.DisplayName}";
    }

    // Tier-coded coloring so the M7 demo reads as Common→Uncommon→Rare across
    // the three pedestals without a UI chip system landing first.
    private static Color ColorForTier(ItemTier tier) => tier switch
    {
        ItemTier.Common => new Color(0.85f, 0.88f, 0.92f),
        ItemTier.Uncommon => new Color(0.55f, 0.85f, 1.0f),
        ItemTier.Rare => new Color(1.0f, 0.78f, 0.30f),
        ItemTier.Cursed => new Color(0.85f, 0.30f, 0.65f),
        _ => Colors.White,
    };

    private static Color TintForTier(float amp, ItemTier tier) => tier switch
    {
        ItemTier.Common => new Color(amp, amp, amp),
        ItemTier.Uncommon => new Color(amp * 0.7f, amp * 0.9f, amp),
        ItemTier.Rare => new Color(amp, amp * 0.85f, amp * 0.5f),
        _ => new Color(amp, amp, amp),
    };

    public EntityState? CaptureState() => new PassivePedestalState(PassiveId, _claimed);

    public void RestoreState(EntityState state)
    {
        if (state is not PassivePedestalState s) return;
        PassiveId = s.PassiveId;
        _claimed = s.Claimed;
        ResolveDefinition();
        ApplyVisualState();
        UpdatePromptText();
    }
}
