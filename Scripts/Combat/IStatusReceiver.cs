using Stationfall.Core.Combat;

namespace Stationfall.Godot.Combat;

// Implemented by Godot nodes (enemies, the player when buffed) whose
// gameplay state can be modulated by an applied status. The status pipeline
// (Curtain Call's Slow today; future Bleed / Poison / Stun) hands the
// receiver a StatusEffect; the receiver is responsible for routing it
// through its own StatusTracker and any visual feedback.
public interface IStatusReceiver
{
    void ApplyStatus(StatusEffect effect);
}
