using Godot;

namespace Stationfall.Godot.UI;

// Trauma-based screen shake. Trauma in [0,1] accumulates from impact events;
// shake offset = MaxShakePx * trauma * unit-vec (linear, not squared).
//
// We started with the canonical trauma² (Squirrel Eiserloh's GDC model), but
// our trauma values are calibrated low (0.05 dodge, 0.15 light hit, 0.25
// damage taken). Squaring those produces sub-pixel shake — invisible. Linear
// scaling at MaxShakePx=40 gives 2px/6px/10px/20px/32px across the trauma
// range, all clearly readable on a 1280×720 frame.
//
// PLANNING § Game Feel locks the per-event trauma values; this script owns
// the kinematics (max amplitude, decay rate). Both [Export] so playtest can
// retune feel without touching code.
[GlobalClass]
public partial class GameCamera : Camera2D
{
    [Export] public float MaxShakePx { get; set; } = 40f;
    // Trauma units per second. 1.5 → trauma 0.3 fades in 0.2s, 0.8 in ~0.53s.
    [Export] public float DecayPerSecond { get; set; } = 1.5f;

    private float _trauma;
    private readonly RandomNumberGenerator _rng = new();

    public override void _Ready()
    {
        _rng.Randomize();
    }

    public override void _Process(double delta)
    {
        if (_trauma <= 0f)
        {
            Offset = Vector2.Zero;
            return;
        }

        float shake = MaxShakePx * _trauma;
        Offset = new Vector2(
            _rng.RandfRange(-1f, 1f) * shake,
            _rng.RandfRange(-1f, 1f) * shake);

        _trauma = Mathf.Max(0f, _trauma - DecayPerSecond * (float)delta);
    }

    public void AddTrauma(float amount)
    {
        if (amount <= 0f) return;
        _trauma = Mathf.Min(1f, _trauma + amount);
    }
}
