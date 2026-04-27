namespace Stationfall.Godot.Combat;

// Hit-stop contract. Implemented by anything that can be a hitbox/hurtbox
// owner — currently PlayerController and EnemyController. HitboxComponent
// calls Freeze() on both attacker and target when a hit lands, with the
// per-side ms values from ComboStep.
public interface IFreezable
{
    void Freeze(double seconds);
}
