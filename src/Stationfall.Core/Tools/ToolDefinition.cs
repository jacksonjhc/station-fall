namespace Stationfall.Core.Tools;

// Discriminator for the tool slot. M6 ships only Magnetic Grapple; the other
// three slice tools (Slingshot, Stun Coil, Flashlight per W5 / PLANNING.md §
// Slice tool roster) get added once their resource models exist.
public enum ToolKind
{
    MagneticGrapple,
    Slingshot,    // post-M6
    StunCoil,     // post-M6
    Flashlight,   // post-M6
}

// Editor-authored tool metadata. The runtime config (cooldown, range, etc.)
// hangs off Kind-specific config records (e.g. MagneticGrappleConfig) so each
// tool keeps its own knob surface without a sprawling base class.
public record ToolDefinition(string Id, string DisplayName, ToolKind Kind);
