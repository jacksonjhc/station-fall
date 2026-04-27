using Godot;
using Stationfall.Godot.Items;

namespace Stationfall.Godot.UI;

// HUD readout for the player's credit balance. Subscribes to
// CreditsService.BalanceChanged on _Ready so the number redraws on every
// pickup/spend without per-frame polling.
//
// Owns its own label so the scene only needs to drop in this Control —
// matches the HealthBar pattern of being a single Control responsible for
// its own _Draw / child layout.
public partial class CreditCounter : Control
{
    [Export] public Color TextColor { get; set; } = new Color(1.0f, 0.85f, 0.25f);
    [Export] public int FontSize { get; set; } = 24;
    [Export] public string Prefix { get; set; } = "¢ ";

    private Label? _label;

    public override void _Ready()
    {
        _label = new Label
        {
            Text = Prefix + "0",
            // Anchor flush to top-left of this Control; sizing is owned by parent.
            AnchorRight = 1f,
            AnchorBottom = 1f,
        };
        _label.AddThemeColorOverride("font_color", TextColor);
        _label.AddThemeFontSizeOverride("font_size", FontSize);
        AddChild(_label);

        if (CreditsService.Instance != null)
        {
            CreditsService.Instance.BalanceChanged += OnBalanceChanged;
            OnBalanceChanged(CreditsService.Instance.Balance);
        }
        else
        {
            // _Ready ordering: if the service hasn't initialized yet, retry once
            // on the next idle frame. Mirrors the deferred-bind pattern used
            // elsewhere when scene-attached singletons race with their consumers.
            CallDeferred(nameof(LateBind));
        }
    }

    private void LateBind()
    {
        if (CreditsService.Instance == null) return;
        CreditsService.Instance.BalanceChanged += OnBalanceChanged;
        OnBalanceChanged(CreditsService.Instance.Balance);
    }

    private void OnBalanceChanged(int balance)
    {
        if (_label != null) _label.Text = Prefix + balance.ToString();
    }
}
