namespace Stationfall.Godot.Combat;

// Single source of truth for 2D physics layer/mask bits.
//
// Scenes hardcode the integer values (collision_layer / collision_mask in
// .tscn) — this file mirrors them so C# defaults (LOS raycast masks, future
// hitbox-viz toggles, debug spawning) reference named bits instead of magic
// numbers. If you add or move a bit here, update Project Settings → Layer
// Names → 2D Physics so the editor names match, and audit existing scenes
// for drift.
//
// Channel semantics:
//   PlayerBody         — CharacterBody2D for the player; collides with Walls and EnemyBody.
//   Hurtable           — Area2D for anything player attacks should hit (enemy hurtboxes,
//                        breakable crates, future destructibles). Player attack hitboxes mask this.
//   PlayerAttackHitbox — Area2D the player swings; layer = this, mask = Hurtable.
//   EnemyAttackHitbox  — Area2D for enemy lunges/projectiles; layer = this, mask = PlayerHurtbox.
//   Walls              — StaticBody2D wall geometry and door seal bodies; LOS raycast targets this.
//   DoorTrigger        — Area2D door transition zones; mask = PlayerBody.
//   EnemyBody          — CharacterBody2D for enemies; collides with Walls and PlayerBody.
//   PlayerHurtbox      — Area2D the player gets hit through; EnemyAttackHitbox masks this.
//   GrappleTarget      — Area2D the Magnetic Grapple projectile attaches to (anchors,
//                        future grappable props). Shared across enemies + anchors via
//                        a separate detection layer so the projectile can distinguish
//                        them from walls and ordinary hurtboxes.
public static class CollisionLayers
{
    public const uint PlayerBody         = 1u << 0; //   1
    public const uint Hurtable           = 1u << 1; //   2
    public const uint PlayerAttackHitbox = 1u << 2; //   4
    public const uint EnemyAttackHitbox  = 1u << 3; //   8
    public const uint Walls              = 1u << 4; //  16
    public const uint DoorTrigger        = 1u << 5; //  32
    public const uint EnemyBody          = 1u << 6; //  64
    public const uint PlayerHurtbox      = 1u << 7; // 128
    public const uint GrappleTarget      = 1u << 8; // 256
}
