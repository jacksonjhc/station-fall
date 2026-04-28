using Godot;
using Stationfall.Godot.Combat;
using Stationfall.Godot.Player;

namespace Stationfall.Godot.Items;

// Pedestal that grants a tool to the player on overlap. Phase B uses this for
// the M6 tool-pedestal room (PLANNING.md § Magnetic Grapple — M6 deliverable
// scope). Authored with a ToolResource [Export] so designers swap tools by
// dragging a different .tres onto the pedestal.
//
// Pickup-when-occupied is a swap-prompt UX (W5) — deferred. For now if the
// player already has a tool, this pedestal silently rejects.
public partial class ToolPickupNode : Area2D
{
    [Signal] public delegate void EquippedEventHandler(string toolId);

    [Export] public ToolResource? Tool { get; set; }

    private bool _consumed;

    public override void _Ready()
    {
        CollisionLayer = 0;
        CollisionMask = CollisionLayers.PlayerBody;
        BodyEntered += OnBodyEntered;
    }

    private void OnBodyEntered(Node2D body)
    {
        if (_consumed) return;
        if (body is not PlayerController player) return;
        if (Tool == null)
        {
            GD.PushWarning($"ToolPickupNode '{Name}' has no Tool assigned");
            return;
        }
        if (!player.EquipTool(Tool)) return;

        _consumed = true;
        Visible = false;
        SetDeferred(Area2D.PropertyName.Monitoring, false);
        EmitSignal(SignalName.Equipped, Tool.Id);
    }
}
