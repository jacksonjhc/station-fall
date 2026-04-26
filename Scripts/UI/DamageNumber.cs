using Godot;

namespace Stationfall.Godot.UI;

// Floating number that drifts up and fades out. Spawned by HitboxComponent connections.
public partial class DamageNumber : Node2D
{
    [Export] public float DriftPixels { get; set; } = 28f;
    [Export] public float LifetimeSeconds { get; set; } = 0.6f;
    [Export] public Color NormalColor { get; set; } = Colors.White;
    [Export] public Color HeavyColor { get; set; } = new Color(1f, 0.7f, 0.2f);

    private Label? _label;
    private float _elapsed;
    private Vector2 _origin;

    public override void _Ready()
    {
        _label = new Label
        {
            Text = "",
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            ZIndex = 100,
        };
        AddChild(_label);
        _origin = Position;
    }

    public void Show(int amount, bool heavy)
    {
        if (_label == null) return;
        _label.Text = amount.ToString();
        _label.AddThemeColorOverride("font_color", heavy ? HeavyColor : NormalColor);
        _label.AddThemeFontSizeOverride("font_size", heavy ? 24 : 18);
        _elapsed = 0;
    }

    public override void _Process(double delta)
    {
        _elapsed += (float)delta;
        float t = Mathf.Clamp(_elapsed / LifetimeSeconds, 0f, 1f);
        Position = _origin + new Vector2(0, -DriftPixels * t);
        if (_label != null) _label.Modulate = new Color(1, 1, 1, 1f - t);
        if (t >= 1f) QueueFree();
    }
}
