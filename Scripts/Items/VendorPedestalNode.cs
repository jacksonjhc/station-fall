using Godot;
using Stationfall.Core.Items;
using Stationfall.Core.Runs;
using Stationfall.Godot.Audio;
using Stationfall.Godot.Combat;
using Stationfall.Godot.Persistence;
using Stationfall.Godot.Player;

namespace Stationfall.Godot.Items;

// One vendor stock slot. Mirrors ChestNode's interaction shape — Area2D
// range trigger, prompt fade/pulse, SPACE to commit — but the commit path
// is "spend credits → apply consumable effect" instead of "drop loot".
//
// ConsumableId picks the SKU. For M4 the .tscn sets it directly; once M5
// procgen lands and VendorRoom is template-driven, DungeonInstantiator
// will overwrite ConsumableId from VendorStockGenerator output before
// _Ready resolves the definition.
//
// Sold state is one-shot per run and persists via IPersistentEntity:
// re-entering the vendor room shows already-sold pedestals as empty and
// rejects further interaction. Stock IDs persist too — a re-entered
// VendorRoom shows the same SKUs the player saw before, not re-rolled.
public partial class VendorPedestalNode : Node2D, IPersistentEntity
{
    [Signal] public delegate void PurchasedEventHandler(string consumableId);

    [Export] public string EntityId { get; set; } = "";
    [Export] public string ConsumableId { get; set; } = ConsumableCatalog.MedkitSmallId;
    [Export] public NodePath SpritePath { get; set; } = "Sprite";
    [Export] public NodePath InteractAreaPath { get; set; } = "InteractArea";
    [Export] public NodePath PromptPath { get; set; } = "Prompt";

    // Stored as CanvasItem so the .tscn can swap Sprite2D / Polygon2D /
    // ColorRect without script changes — Modulate is what we drive for
    // sold-state desaturation, and that's defined on CanvasItem.
    private CanvasItem? _sprite;
    private Area2D? _interactArea;
    private Label? _prompt;
    private bool _sold;
    private bool _playerInRange;
    private double _promptPulseTime;
    private ConsumableDefinition? _definition;

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
            // Tint the prompt toward gold when affordable, red when broke,
            // so the player reads "you can/can't buy this" without a menu.
            bool canAfford = _definition != null
                && CreditsService.Instance != null
                && CreditsService.Instance.CanAfford(_definition.PriceCredits);
            _prompt.Modulate = canAfford
                ? new Color(amp, amp, amp * 0.85f)
                : new Color(amp * 0.95f, amp * 0.55f, amp * 0.55f);
        }

        if (_sold) return;
        if (!_playerInRange) return;
        if (Input.IsActionJustPressed("interact")) TryPurchase();
    }

    private void OnBodyEntered(Node2D body)
    {
        if (!body.IsInGroup("player")) return;
        _playerInRange = true;
        _promptPulseTime = 0;
        if (_prompt != null && !_sold) _prompt.Visible = true;
    }

    private void OnBodyExited(Node2D body)
    {
        if (!body.IsInGroup("player")) return;
        _playerInRange = false;
        if (_prompt != null) _prompt.Visible = false;
    }

    private void TryPurchase()
    {
        if (_sold) return;
        if (_definition == null) return;

        var credits = CreditsService.Instance;
        if (credits == null) return;
        if (!credits.TrySpend(_definition.PriceCredits))
        {
            // Broke. Cheap "denied" cue — reuse the damage-taken hit so the
            // player has audible feedback without a vendor-specific SFX
            // landing yet. Sound design will pass real cues over this in M9.
            Sfx.Instance?.PlayDamageTaken();
            return;
        }

        ApplyConsumable(_definition);
        _sold = true;
        ApplyVisualState();
        EmitSignal(SignalName.Purchased, _definition.Id);
    }

    private void ApplyConsumable(ConsumableDefinition def)
    {
        if (def.HealAmount > 0)
        {
            var player = GetTree().GetFirstNodeInGroup("player") as PlayerController;
            player?.Heal(def.HealAmount);
        }
        // Non-heal consumables (shield, buff, utility) land alongside W5;
        // wire their effects into this branch when they exist rather than
        // expanding the record into a free-form action bag.
    }

    private void ResolveDefinition()
    {
        _definition = ConsumableCatalog.FindById(ConsumableId);
        if (_definition == null)
            GD.PushWarning($"VendorPedestal: unknown ConsumableId '{ConsumableId}' — pedestal will be inert.");
    }

    private void ApplyVisualState()
    {
        if (_sprite != null)
        {
            // Sold pedestals stay visible as a "this slot existed" marker
            // but read clearly as empty — heavy desaturation + dim alpha.
            _sprite.Modulate = _sold
                ? new Color(0.45f, 0.45f, 0.5f, 0.55f)
                : Colors.White;
        }
        if (_prompt != null && _sold) _prompt.Visible = false;
    }

    private void UpdatePromptText()
    {
        if (_prompt == null) return;
        if (_definition == null)
        {
            _prompt.Text = "[unknown SKU]";
            return;
        }
        _prompt.Text = $"[SPACE] {_definition.DisplayName} — {_definition.PriceCredits}c";
    }

    public EntityState? CaptureState() => new VendorPedestalState(ConsumableId, _sold);

    public void RestoreState(EntityState state)
    {
        if (state is not VendorPedestalState s) return;
        // The captured ConsumableId is authoritative — if procgen rolled a
        // SKU at first entry, that SKU sticks across re-entries even if the
        // .tscn export later changes. ResolveDefinition() rebuilds against
        // the restored id so price/name are correct.
        ConsumableId = s.ConsumableId;
        _sold = s.Sold;
        ResolveDefinition();
        ApplyVisualState();
        UpdatePromptText();
    }
}
