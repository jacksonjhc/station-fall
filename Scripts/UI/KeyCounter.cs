using Godot;
using Stationfall.Godot.Items;

namespace Stationfall.Godot.UI;

// HUD readout for the player's generic-key count. Subscribes to
// KeysService.CountChanged on _Ready so the number redraws on every
// pickup and door-consume without per-frame polling.
//
// Visual mirrors CreditCounter — single Label child rendered with a
// "key icon glyph" prefix. Texture-based key icon will land when the HUD
// gets a proper visual pass; for now the unicode glyph carries the read.
public partial class KeyCounter : Control
{
    [Export] public Color TextColor { get; set; } = new Color(0.85f, 0.95f, 1.0f);
    [Export] public int FontSize { get; set; } = 32;
    [Export] public string Prefix { get; set; } = "⚷ ";

    private Label? _label;

    public override void _Ready()
    {
        _label = new Label
        {
            Text = Prefix + "0",
            AnchorRight = 1f,
            AnchorBottom = 1f,
        };
        _label.AddThemeColorOverride("font_color", TextColor);
        _label.AddThemeFontSizeOverride("font_size", FontSize);
        AddChild(_label);

        if (KeysService.Instance != null)
        {
            KeysService.Instance.CountChanged += OnCountChanged;
            OnCountChanged(KeysService.Instance.Count);
        }
        else
        {
            CallDeferred(nameof(LateBind));
        }
    }

    private void LateBind()
    {
        if (KeysService.Instance == null) return;
        KeysService.Instance.CountChanged += OnCountChanged;
        OnCountChanged(KeysService.Instance.Count);
    }

    private void OnCountChanged(int count)
    {
        if (_label != null) _label.Text = Prefix + count.ToString();
    }
}
