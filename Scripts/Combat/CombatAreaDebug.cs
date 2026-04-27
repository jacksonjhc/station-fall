using Godot;

namespace Stationfall.Godot.Combat;

// Shared scaffolding for hitbox/hurtbox debug visualization. Both components
// register themselves in CombatAreaDebug.Group on _Ready and lazily build a
// debug polygon mirroring their first RectangleShape2D when the overlay
// asks for visibility.
internal static class CombatAreaDebug
{
    public const string Group = "combat_areas";

    // Mirrors-on-toggle: when DebugOverlay flips the global flag, components
    // already in the tree iterate the group and call SetDebugVisible. New
    // components (e.g. spawned via the debug console) read this on _Ready
    // so they pick up the current state without a re-toggle.
    public static bool DefaultVisible { get; set; } = false;

    public static Polygon2D? BuildPolygon(Area2D area, Color color)
    {
        foreach (var child in area.GetChildren())
        {
            if (child is CollisionShape2D shape && shape.Shape is RectangleShape2D rect)
            {
                float hx = rect.Size.X * 0.5f;
                float hy = rect.Size.Y * 0.5f;
                return new Polygon2D
                {
                    Polygon = new[]
                    {
                        new Vector2(-hx, -hy),
                        new Vector2( hx, -hy),
                        new Vector2( hx,  hy),
                        new Vector2(-hx,  hy),
                    },
                    Color = color,
                    Position = shape.Position,
                    ZIndex = 100,
                };
            }
        }
        return null;
    }
}
