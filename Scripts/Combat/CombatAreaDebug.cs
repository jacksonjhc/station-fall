using Godot;

namespace Stationfall.Godot.Combat;

// Shared scaffolding for hitbox/hurtbox debug visualization. Both components
// register themselves in CombatAreaDebug.Group on _Ready and lazily build a
// debug polygon mirroring their first CollisionShape2D when the overlay
// asks for visibility.
//
// Supports RectangleShape2D and CircleShape2D — circle is approximated by a
// 24-segment polygon. Pirouette's runtime CircleShape2D swap re-builds via
// HitboxComponent.SetRadialMode so the visualization always reflects the
// active shape.
internal static class CombatAreaDebug
{
    public const string Group = "combat_areas";
    private const int CircleSegments = 24;

    // Mirrors-on-toggle: when DebugOverlay flips the global flag, components
    // already in the tree iterate the group and call SetDebugVisible. New
    // components (e.g. spawned via the debug console) read this on _Ready
    // so they pick up the current state without a re-toggle.
    public static bool DefaultVisible { get; set; } = false;

    public static Polygon2D? BuildPolygon(Area2D area, Color color)
    {
        foreach (var child in area.GetChildren())
        {
            if (child is CollisionShape2D shape && shape.Shape != null)
            {
                var verts = BuildVerts(shape.Shape);
                if (verts == null) continue;
                return new Polygon2D
                {
                    Polygon = verts,
                    Color = color,
                    Position = shape.Position,
                    ZIndex = 100,
                };
            }
        }
        return null;
    }

    private static Vector2[]? BuildVerts(Shape2D shape) => shape switch
    {
        RectangleShape2D rect => RectangleVerts(rect.Size),
        CircleShape2D circle => CircleVerts(circle.Radius),
        _ => null,
    };

    private static Vector2[] RectangleVerts(Vector2 size)
    {
        float hx = size.X * 0.5f;
        float hy = size.Y * 0.5f;
        return new[]
        {
            new Vector2(-hx, -hy),
            new Vector2( hx, -hy),
            new Vector2( hx,  hy),
            new Vector2(-hx,  hy),
        };
    }

    private static Vector2[] CircleVerts(float radius)
    {
        var verts = new Vector2[CircleSegments];
        for (int i = 0; i < CircleSegments; i++)
        {
            float angle = Mathf.Tau * i / CircleSegments;
            verts[i] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
        }
        return verts;
    }
}
