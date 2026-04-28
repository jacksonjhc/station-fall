using Godot;
using Stationfall.Core.Tools;

namespace Stationfall.Godot.Items;

// Editor-authorable tool data. One .tres per tool under Assets/Data/Tools/.
// Mirrors the EnemyResource pattern: flat schema, designer-readable, projects
// back into a Core ToolDefinition record so Core never sees the engine type.
//
// Currently authors all four slice tools' shared metadata + Magnetic Grapple's
// knob set. When Slingshot/StunCoil/Flashlight land, split per-kind subclasses
// (or fold their knobs in here behind a Kind switch — TBD when a second tool
// actually exists).
[GlobalClass]
public partial class ToolResource : Resource
{
    [Export] public string Id { get; set; } = "";
    [Export] public string DisplayName { get; set; } = "";
    [Export] public ToolKind Kind { get; set; } = ToolKind.MagneticGrapple;

    // Pickup scene granted into the world (pedestal, vendor stock, etc.).
    [Export] public PackedScene? PickupScene { get; set; }

    // Magnetic Grapple knobs — defaults match PLANNING.md § Magnetic Grapple.
    // Kept on the base resource to avoid a per-kind subclass before a second
    // tool exists; ignored by the runtime when Kind != MagneticGrapple.
    [Export] public float GrappleRangePx { get; set; } = 220f;
    [Export] public float GrappleProjectileSpeedPxPerSec { get; set; } = 520f;
    [Export] public int GrappleWindupFrames { get; set; } = 6;
    [Export] public float GrappleCooldownSeconds { get; set; } = 2.5f;
    [Export] public int GrappleLightStaggerFrames { get; set; } = 8;
    [Export] public float GrappleConeSnapHalfAngleRadians { get; set; } = 0.17453293f;

    public ToolDefinition ToCoreDefinition() => new(Id: Id, DisplayName: DisplayName, Kind: Kind);

    public MagneticGrappleConfig ToGrappleConfig() => new(
        RangePx: GrappleRangePx,
        ProjectileSpeedPxPerSec: GrappleProjectileSpeedPxPerSec,
        WindupFrames: GrappleWindupFrames,
        CooldownSeconds: GrappleCooldownSeconds,
        LightStaggerFrames: GrappleLightStaggerFrames,
        ConeSnapHalfAngleRadians: GrappleConeSnapHalfAngleRadians);
}
