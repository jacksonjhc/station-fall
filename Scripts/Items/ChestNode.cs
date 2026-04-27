using Godot;
using Stationfall.Core.Items;
using Stationfall.Core.Rng;
using Stationfall.Core.Runs;
using Stationfall.Godot.Combat;
using Stationfall.Godot.Persistence;

namespace Stationfall.Godot.Items;

// Interactable loot chest. Player walks into range, prompt fades in, SPACE
// (interact action) rolls a LootTable and fans pickups outward via the
// existing CreditPickupNode/KeyPickupNode.Spawn paths. One-shot per run —
// IPersistentEntity captures Opened so re-entering the room shows the chest
// already open with no loot. The dropped pickups themselves persist via
// their own PickupState entries; opened-but-uncollected coins still sit on
// the floor when the player returns.
//
// Input: reads InputAction "interact" (mapped to SPACE in project.godot).
// Reading directly here keeps PlayerController out of "things in range"
// dispatch — chest owns its own range, prompt, and trigger logic. Vendors
// and terminals will re-use the same shape when they land.
//
// Range detection: child Area2D with collision_mask = PlayerBody. body_entered
// flips _playerInRange on; body_exited flips it off.
public partial class ChestNode : Node2D, IPersistentEntity
{
    [Signal] public delegate void OpenedEventHandler();

    [Export] public string EntityId { get; set; } = "";
    [Export] public NodePath SpritePath { get; set; } = "Sprite";
    [Export] public NodePath InteractAreaPath { get; set; } = "InteractArea";
    [Export] public NodePath PromptPath { get; set; } = "Prompt";
    [Export] public Texture2D? ClosedTexture { get; set; }
    [Export] public Texture2D? OpenTexture { get; set; }

    // Loot table parameters — kept on the node for now so each chest can
    // tune its drops without a per-chest .tres. Promote to a LootTableResource
    // once the second chest type lands and we want shared definitions.
    [Export] public int MinPicks { get; set; } = 4;
    [Export] public int MaxPicks { get; set; } = 6;
    [Export] public int CreditWeight { get; set; } = 80;
    [Export] public int KeyWeight { get; set; } = 20;

    private Sprite2D? _sprite;
    private Area2D? _interactArea;
    private CanvasItem? _prompt;
    private bool _opened;
    private bool _playerInRange;
    private double _promptPulseTime;
    private RngService? _rng;

    public override void _Ready()
    {
        _sprite = GetNodeOrNull<Sprite2D>(SpritePath);
        _interactArea = GetNodeOrNull<Area2D>(InteractAreaPath);
        _prompt = GetNodeOrNull<CanvasItem>(PromptPath);

        if (_interactArea != null)
        {
            _interactArea.CollisionLayer = 0;
            _interactArea.CollisionMask = CollisionLayers.PlayerBody;
            _interactArea.BodyEntered += OnBodyEntered;
            _interactArea.BodyExited += OnBodyExited;
        }

        if (_prompt != null) _prompt.Visible = false;

        // Per-instance RNG seeded from instance id — same convention enemies
        // use for now. Run-level seeding lands in M5.
        _rng = new RngService(unchecked((int)GetInstanceId()));

        ApplyVisualState();
    }

    public override void _Process(double delta)
    {
        // Prompt only animates while visible (player in range, chest closed).
        // Pulse mirrors the door's "armed" tell so the player gets a
        // consistent "this is interactable now" cue across the codebase.
        if (_prompt != null && _prompt.Visible)
        {
            _promptPulseTime += delta;
            float t = (float)System.Math.Sin(_promptPulseTime * System.Math.PI * 1.5);
            float amp = 1.275f + 0.425f * t;
            _prompt.Modulate = new Color(amp, amp, amp * 0.9f);
        }

        if (_opened) return;
        if (!_playerInRange) return;
        if (Input.IsActionJustPressed("interact")) Open();
    }

    private void OnBodyEntered(Node2D body)
    {
        if (!body.IsInGroup("player")) return;
        _playerInRange = true;
        _promptPulseTime = 0;
        if (_prompt != null && !_opened) _prompt.Visible = true;
    }

    private void OnBodyExited(Node2D body)
    {
        if (!body.IsInGroup("player")) return;
        _playerInRange = false;
        if (_prompt != null) _prompt.Visible = false;
    }

    private void Open()
    {
        if (_opened) return;
        _opened = true;
        ApplyVisualState();
        DropLoot();
        EmitSignal(SignalName.Opened);
    }

    private void DropLoot()
    {
        if (_rng == null) return;

        int min = System.Math.Max(0, MinPicks);
        int max = System.Math.Max(min, MaxPicks);
        if (max <= 0) return;

        // Build the table inline — two entries, weighted, single-unit drops.
        // Each pick spawns one pickup so the visual fan-out reads as
        // "this chest gave N items," matching the credit-drop convention.
        var table = new LootTable(
            new LootEntry(CreditPickupNode.ItemKey, Weight: CreditWeight, MinAmount: 1, MaxAmount: 1),
            new LootEntry(KeyPickupNode.ItemKey, Weight: KeyWeight, MinAmount: 1, MaxAmount: 1));

        int picks = min == max ? min : _rng.NextInt(min, max + 1);
        var parent = GetParent();
        if (parent == null) return;

        for (int i = 0; i < picks; i++)
        {
            var roll = table.Roll(_rng);
            if (roll == null) continue;
            switch (roll.ItemKey)
            {
                case CreditPickupNode.ItemKey:
                    CreditPickupNode.Spawn(parent, GlobalPosition, roll.Amount, _rng);
                    break;
                case KeyPickupNode.ItemKey:
                    KeyPickupNode.Spawn(parent, GlobalPosition, roll.Amount, _rng);
                    break;
            }
        }
    }

    private void ApplyVisualState()
    {
        if (_sprite != null)
        {
            var tex = _opened ? OpenTexture : ClosedTexture;
            if (tex != null) _sprite.Texture = tex;
        }
        if (_prompt != null)
        {
            // Hide the prompt the moment the chest opens — even if the player
            // is still standing on it. Re-shows would be misleading (nothing
            // to do).
            if (_opened) _prompt.Visible = false;
        }
    }

    public EntityState? CaptureState() => new ChestState(_opened);

    public void RestoreState(EntityState state)
    {
        if (state is not ChestState s) return;
        _opened = s.Opened;
        ApplyVisualState();
    }
}
