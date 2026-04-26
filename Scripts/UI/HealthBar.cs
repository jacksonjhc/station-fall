using Godot;

namespace Stationfall.Godot.UI;

// Alundra-style icons: small = 1 HP, big = 10 HP. Auto-promotes when MaxHp ≥ 10.
// Placeholder shapes for M1; real icon art lands later.
public partial class HealthBar : Control
{
    [Export] public Color FilledColor { get; set; } = new Color(0.85f, 0.20f, 0.25f);
    [Export] public Color EmptyColor { get; set; } = new Color(0.20f, 0.05f, 0.05f);
    [Export] public Color OutlineColor { get; set; } = new Color(0, 0, 0, 0.6f);
    [Export] public int SmallIconSize { get; set; } = 16;
    [Export] public int BigIconSize { get; set; } = 32;
    [Export] public int IconSpacing { get; set; } = 4;

    private int _hp;
    private int _maxHp;

    public void SetHealth(int hp, int maxHp)
    {
        _hp = Mathf.Max(0, hp);
        _maxHp = Mathf.Max(0, maxHp);
        QueueRedraw();
    }

    public override void _Draw()
    {
        if (_maxHp <= 0) return;

        int bigIcons = _maxHp / 10;
        int smallIcons = _maxHp % 10;
        int hpRemaining = _hp;

        var origin = new Vector2(0, 0);
        for (int i = 0; i < bigIcons; i++)
        {
            int filled = Mathf.Clamp(hpRemaining, 0, 10);
            DrawIcon(origin, BigIconSize, filled / 10f);
            origin.X += BigIconSize + IconSpacing;
            hpRemaining -= filled;
        }
        for (int i = 0; i < smallIcons; i++)
        {
            float fill = hpRemaining > 0 ? 1f : 0f;
            DrawIcon(origin, SmallIconSize, fill);
            origin.X += SmallIconSize + IconSpacing;
            if (hpRemaining > 0) hpRemaining--;
        }
    }

    private void DrawIcon(Vector2 pos, int size, float fillRatio)
    {
        var rect = new Rect2(pos, new Vector2(size, size));
        DrawRect(rect, EmptyColor, filled: true);
        if (fillRatio > 0)
        {
            float h = size * fillRatio;
            var fillRect = new Rect2(new Vector2(pos.X, pos.Y + size - h), new Vector2(size, h));
            DrawRect(fillRect, FilledColor, filled: true);
        }
        DrawRect(rect, OutlineColor, filled: false, width: 2f);
    }
}
