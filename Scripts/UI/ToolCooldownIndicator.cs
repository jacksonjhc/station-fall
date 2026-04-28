using Godot;
using Stationfall.Godot.Items;
using Stationfall.Godot.Player;

namespace Stationfall.Godot.UI;

// Top-right HUD indicator showing tool readiness + cooldown remaining.
// Hidden until the player equips a tool; afterwards renders an icon that's
// bright cyan when ready and dim red while cooling, with a remaining-seconds
// label.
//
// Polls the player's MagneticGrappleTool each _Process frame rather than
// signal-binding because the tool spawns dynamically when the player picks
// up the pickup — there's no pre-_Ready handle the HUD can subscribe to.
// Polling cost is one node lookup + a float compare; cheap.
public partial class ToolCooldownIndicator : Control
{
    [Export] public Color ReadyColor { get; set; } = new Color(0.85f, 0.95f, 1.0f);
    [Export] public Color CoolingColor { get; set; } = new Color(0.55f, 0.30f, 0.30f);
    [Export] public int FontSize { get; set; } = 22;

    private PlayerController? _player;
    private Polygon2D? _icon;
    private Label? _label;

    public override void _Ready()
    {
        // Diamond-shaped icon mirrors the GrappleAnchor / ToolPickup palette
        // — same "thing the grapple talks to" visual family.
        _icon = new Polygon2D
        {
            Polygon = new Vector2[]
            {
                new(-14, -4), new(-6, -14), new(6, -14), new(14, -4),
                new(14, 4),   new(6, 14),   new(-6, 14), new(-14, 4),
            },
            Position = new Vector2(20, 24),
            Color = ReadyColor,
        };
        AddChild(_icon);

        _label = new Label
        {
            Text = "",
            Position = new Vector2(44, 8),
            Size = new Vector2(120, 40),
        };
        _label.AddThemeColorOverride("font_color", ReadyColor);
        _label.AddThemeFontSizeOverride("font_size", FontSize);
        AddChild(_label);

        Visible = false;
    }

    public override void _Process(double delta)
    {
        var tool = ResolveTool();
        if (tool == null)
        {
            Visible = false;
            return;
        }
        Visible = true;

        bool ready = tool.IsReady;
        var color = ready ? ReadyColor : CoolingColor;
        if (_icon != null) _icon.Color = color;
        if (_label != null)
        {
            _label.AddThemeColorOverride("font_color", color);
            _label.Text = ready ? "READY [RB]" : $"{tool.CooldownSecondsRemaining:0.0}s";
        }
    }

    private MagneticGrappleTool? ResolveTool()
    {
        if (_player == null || !IsInstanceValid(_player))
            _player = GetTree().GetFirstNodeInGroup("player") as PlayerController;
        return _player?.GrappleTool;
    }
}
