# StationFall — Milestone Roadmap

The plan from empty repo to first vertical slice. Each milestone has an **exit criterion** that must be demonstrably true before moving on. Milestones may overlap slightly, but never skip an exit criterion.

Milestones reference **workshops** that must be complete before the milestone can ship. Workshops are run as separate sessions; see [WORKSHOPS.md](WORKSHOPS.md).

See [PLANNING.md](PLANNING.md) for the design context behind these.

---

## Workshop Gates

| Workshop | Gates milestone(s) | Topic |
|----------|--------------------|-------|
| W1 | M1 | Vessels & Signature Abilities |
| W2 | M1, M3.5 | Combat Feel & Weapon Patterns |
| W3 | M3, M9 | Enemy Roster & Archetype Detail |
| W4 | M8, M9 | Bosses & Mid-Bosses |
| W5 | M6, M7, M9 | Items: Passives, Tools, Consumables |
| W6 | M7 | Synergies |
| W7 | M2, M5 | Dungeon Elements & Mechanics |
| W8 | M9 | Sector Themes (Sector 1 deep dive) |
| W9 | M8 | Difficulty Tier Mechanics |
| W10 | M9 | Narrative Architecture (incl. meta-currency name) |

A milestone cannot ship until its gating workshops are at least to "decisions documented" stage in [WORKSHOPS.md](WORKSHOPS.md).

---

## M0 — Repo / Project Cleanup

**Goal:** project skeleton matches the documented architecture; no dead/legacy code.

**Workshop gates:** none.

**Tasks:**
- Confirm there is no turn-based legacy code to remove (verified — current Core scaffolding is generic).
- Rename [Stats](src/Stationfall.Core/Entities/Stats.cs) → `EntityStats` to match docs and disambiguate from future `RunStats`/`MetaStats`.
- Remove [Combatant](src/Stationfall.Core/Entities/Combatant.cs) — Godot Nodes own runtime entities; Core owns stats and definitions only. Update [PlayerVessel](src/Stationfall.Core/Entities/PlayerVessel.cs) and [RunState](src/Stationfall.Core/Runs/RunState.cs) accordingly.
- Create empty Core folders: `Combat/`, `Ai/`, `ProcGen/`, `Items/`, `Tools/`, `Currency/`, `Progression/`, `Narrative/`, `SaveData/`, `Rng/` (each with a `.gitkeep` or a placeholder type).
- Create empty `Scripts/` subfolders: `Bootstrap/`, `Player/`, `Enemies/`, `Combat/`, `Dungeon/`, `Items/`, `Puzzles/`, `UI/`, `Narrative/`, `Debug/`.
- Create `Scenes/`, `Assets/`, and `Assets/Data/` (with subfolders per [PLANNING.md § Content Authoring Pipeline](PLANNING.md#content-authoring-pipeline)).
- Add an xUnit smoke test that asserts `Core` builds and `EntityStats.IsAlive` works.

**Exit criterion:** `dotnet build src/Stationfall.Core/` and `dotnet test src/Stationfall.Tests/` both green; folder layout matches the structure documented in CLAUDE.md.

---

## M1 — Movement, Combat Sandbox, Adrenaline Rush

**Goal:** one hand-built room where moving and attacking *feels* good — including the Clone vessel's signature ability.

**Workshop gates:** W1 (Vessels), W2 (Combat Feel) — at least the Clone-vessel and basic-attack sections decided.

**Tasks:**
- `Scripts/Bootstrap/` — minimal main scene that loads a single test room and spawns the player
- `Scripts/Player/PlayerController.cs` — input → `CharacterBody2D` movement, dodge roll with i-frames, basic melee swing
- **Clone signature: Adrenaline Rush** — speed burst at low HP, per W1 spec
- `Scripts/Combat/HitboxComponent.cs` and `HurtboxComponent.cs` — `Area2D`-based, with collision layers documented
- `Core/Combat/DamageCalculator.cs` — pure function `(attackerStats, defenderStats, modifiers) → DamageResult`
- `Core/Combat/DamageResult.cs` record
- `Scripts/Debug/DebugOverlay.cs` — minimal dev keybinds: god mode, set HP, print seed (more added in M3.5)
- Health bar HUD (placeholder)
- One static training dummy that takes hits and shows numbers

**Exit criterion:** the player can run, dodge with i-frames, hit a dummy that visibly takes damage, and trigger Adrenaline Rush by dropping below the HP threshold. Combat feel is good enough that further iteration is polish, not redesign.

---

## M2 — Hand-Built Rooms & Door Transitions

**Goal:** movement between hand-authored rooms works. **No procgen yet** — that comes in M5.

**Workshop gates:** W7 (Dungeon Elements) — at least basic door types and one or two interactive props decided.

**Tasks:**
- 3 hand-authored room scenes in `Scenes/Rooms/`
- `Scripts/Dungeon/RoomController.cs` — room state machine (Unexplored → Entered → Active → Cleared)
- `Scripts/Dungeon/DoorNode.cs` — open and enemy-locked variants; transition (camera pan or fade)
- `Core/ProcGen/DungeonLayout.cs` — pure data model used to *describe* this hand-built dungeon (the procgen step that *produces* it lands in M5)
- A hand-authored `DungeonLayout` fixture wires the 3 rooms together
- Minimap stub: shows visited rooms only

**Exit criterion:** start in Entry, walk through doors into adjacent rooms, return; minimap updates with visited rooms; the dungeon is *described* by a `DungeonLayout` instance even though that instance is hand-built.

---

## M3 — First Enemy Archetype + Save/Load Foundation

**Goal:** enemies can hurt the player; player can kill them. **Save/load works end to end** so all subsequent Core types are designed with serialization as a constraint.

**Workshop gates:** W3 (Enemy Roster) ✓ — Twitching Patient (melee rusher, starter enemy) fully specced; full Sector 1 roster available for M9.

**Tasks:**
- `Core/Ai/AiState.cs` enum (Idle, Patrol, Chase, Attack, Stagger, Dead)
- `Core/Ai/SensorData.cs` record (distance, has line-of-sight, hp ratio)
- `Core/Ai/EnemyAiBrain.cs` with the Twitching Patient brain per [PLANNING.md § Enemy Roster](PLANNING.md#enemy-roster) — `Vision` perception, 160 px aggro, LOS required, IV Lunge attack (windup 14 / active 8 / recovery 22)
- Tests: chase activates inside aggro range and only with line-of-sight; attack triggers in melee range; stagger transitions out after duration; LOS loss expires aggro after 2.0 sec
- `Scripts/Enemies/EnemyController.cs` — runs the brain each `_PhysicsProcess`, executes movement and attack hitbox
- Enemy definition loaded from `Assets/Data/Enemies/*.tres` via the content authoring pipeline (per [PLANNING.md § Content Authoring Pipeline](PLANNING.md#content-authoring-pipeline))
- Enemy-locked doors (close on entry, open on clear)
- `Core/SaveData/SaveSerializer.cs` — JSON load/save for `RunState` and `MetaState`, with `schemaVersion` field
- `Core/SaveData/SaveSchema.cs` — version constant + wipe-on-mismatch behavior
- Tests: round-trip a `RunState` and `MetaState`; mismatched schema version triggers wipe path
- Death/restart-to-title flow with persistent meta-save (just narrative-flag persistence for now; nothing meaningful to persist yet but the pipe exists)

**Exit criterion:** enter a room, doors lock, fight rushers, doors open. Save file persists across runs. Adding a new enemy is a `.tres` file edit, not a code change.

---

## M3.5 — Combat Feel Pass & Debug Overlay

**Goal:** mechanics built from M3 onward are tuned around real game feel, not silent placeholders. Iteration speed jumps because debug tools land.

**Workshop gates:** W2 (Combat Feel) — concrete numbers required.

**Tasks:**
- Hit-stop on melee impact (configurable per attack)
- Screen shake on hit, dodge, damage taken (trauma-based)
- Particle burst on hit (placeholder OK)
- Dodge i-frame visualization (sprite tint or outline)
- Sound cue on hit, dodge, death (placeholder OK)
- Re-tune attack timings per W2 tables now that feel-affecting timing exists
- Expand `Scripts/Debug/DebugOverlay.cs`: give item by name, force-spawn enemy by name, teleport to room, clear room, lock seed, kill all enemies, toggle hitbox visualization

**Exit criterion:** hits land with weight; dodges read clearly; you can debug a run without restarting the game.

---

## M4 — Keys, Doors, Credits, Vendor

**Goal:** the dungeon has goals and the player can earn things.

**Workshop gates:** none new — uses W3 enemies and W7 door types.

**Tasks:**
- Key item type in `Core/Items/`; `RunState` tracks held keys
- Key-locked door type in `RoomController` and `DoorNode`
- A reward room type with a placeholder chest emitting a "got loot" signal
- Credits system in `Core/Currency/` — pickups in rooms, drops on enemy kill
- Credits HUD readout
- First **vendor**: hand-authored vendor room scene; spends credits on heal items + one consumable
- Tests: credits accrue and spend correctly; vendor stock generation deterministic from seed

**Exit criterion:** in the (still hand-built) dungeon, finding a key opens a locked door; clearing rooms drops credits; the vendor room lets you spend them.

---

## M5 — Procedural Dungeon Generator

**Goal:** the room graph is now generated from a seed, with provable invariants.

**Workshop gates:** W7 (Dungeon Elements) — full set of room/door primitives required.

**Tasks:**
- `Core/ProcGen/DungeonGenerator.cs` — generates a `DungeonLayout` from a seed
- Algorithm per [PLANNING.md § Dungeon Generation](PLANNING.md#dungeon-generation): spanning tree + back-edges → critical path → branch rooms → tier tagging
- `Scripts/Dungeon/DungeonInstantiator.cs` — reads any `DungeonLayout` and spawns the matching room scenes from a template pool
- Room templates loaded from `Assets/Data/RoomTemplates/` + `Scenes/Rooms/`
- Property-style tests across many seeds asserting all generator invariants from PLANNING:
  - Connected critical path
  - Every locked door has key reachable without it
  - No room double-typed
  - Item room reachable without prior locked-door traversal
  - Layout size within bounds

**Exit criterion:** run the game with seed N, get layout L; every randomly-chosen seed produces a layout that passes all invariants. The hand-built dungeon from M2 is now obsolete (or kept as a debug fixture).

---

## M6 — Magnetic Grapple (Combat + One Other Use)

**Goal:** one tool that proves the tool slot works in at least two distinct contexts. Scope cut from the original "all three" — defer the third use to M9 polish or later.

**Workshop gates:** W5 (Tools section) — Grapple fully specced.

**Tasks:**
- `Core/Tools/ToolDefinition.cs` and `Core/Tools/MagneticGrapple.cs` (rules: range, cooldown, valid target categories)
- Tests: cooldown enforced; valid target categories; pull mechanic outcomes
- `Scripts/Items/ToolNode.cs` and `Scripts/Player/PlayerController.cs` integration: equip, aim, fire
- **Required:** combat use — pull a small enemy toward the player, or pull the player to an enemy
- **Required:** one of (traversal: cross gap by grappling anchor) OR (puzzle: yank a far-side switch)
- **Deferred to M9 or later:** the third use case
- Pickup scene that grants the tool, wired through `Assets/Data/Tools/`

**Exit criterion:** Grapple can be picked up mid-run and meaningfully changes how at least two distinct room types are solved.

---

## M7 — Item Room with 2–3 Interacting Passives

**Goal:** prove the synergy pipeline works. One passive in isolation proves nothing.

**Workshop gates:** W5 (Passives section), W6 (Synergies) — chain of 2–3 specced.

**Tasks:**
- `Core/Items/ItemDefinition.cs` and `Core/Items/ItemEffect.cs` with synergy tagging system
- 2–3 passives chosen by W6 because they should compose. Example chain:
  1. "Static Discharge" — melee hits apply 1s slow
  2. "Voltage Surge" — slowed enemies take +25% damage
  3. "Capacitor" — kill on slowed enemy grants brief shield
- Tests for each passive in isolation, plus tests for each pairwise interaction, plus a test for all three stacked
- Item room scene with three pickup pedestals (only one can be taken)
- `RunState` tracks active passives; `DamageCalculator` reads modifiers from active passives
- Visible effect on enemies (placeholder shader/tint is fine)

**Exit criterion:** entering an item room and choosing one of three passives; subsequent fights show the chosen effect; if the run already has the prior link in the chain, the synergy is observably stronger than the sum of parts.

---

## M8 — Mini-Boss, Meta Currency, Difficulty Tier Rules

**Goal:** the first big-fight encounter, the meta-progression economy is real, and the difficulty tier system is engineered (not just described).

**Workshop gates:** W4 (Mini-Bosses), W9 (Difficulty Tier Mechanics).

**Tasks:**
- New AI brain in `Core/Ai/` for the mini-boss with at least two phases (HP threshold transition), per W4
- Tests: phase transition rules, attack-pattern rotation
- `Scripts/Enemies/MiniBossController.cs` executes the brain
- Telegraphed attacks (visual wind-up) — the player must read patterns
- Boss health bar UI
- Reward on kill: guaranteed item drop + credits + first **meta currency** drop (name TBD per W10)
- `Core/Progression/MetaState.cs` — meta currency balance, vessel unlocks, mirror upgrades, difficulty tier
- Mirror-upgrade hub: minimal screen between runs that spends meta currency on a single unlock (e.g., second vessel)
- Difficulty tier engine — per W9 specs: per-tier multipliers and tier-up trigger formula encoded in `MetaState`/generator
- Vessel-select screen pulls available vessels from `MetaState`

**Exit criterion:** kill the mini-boss → see meta currency added → die → restart → spend currency on a new vessel → start new run with it. Difficulty tier visibly changes generator output (different room count / enemy density / elite presence) on tier-up.

---

## M9 — Sector 1 Vertical Slice + Atmosphere/Horror Pass

**Goal:** Sector 1 (Medical Wing) is end-to-end playable and **feels like the horror game we said we were making**.

**Workshop gates:** W8 (Sector 1 deep dive), W10 (Narrative Architecture), W3 ✓ (Sector 1 roster locked), W4/W5 final entries for Sector 1.

**Tasks:**

*Content* (driven by workshops):
- Template pool of 6–10 hand-authored Medical Wing rooms in `Scenes/Rooms/MedicalWing/`
- Generator selects from templates; assigns content tier from difficulty tier
- Sector 1 enemy roster per [PLANNING.md § Enemy Roster](PLANNING.md#enemy-roster): 6 enemies (Twitching Patient, Drip Drone, Convulsing Body, Suture Mite, Bio-Seal Orderly, Corrupted Medbot) plus 4 elite variants. Corrupted Medbot is deferrable if scope tightens.
- One mid-boss + one sector boss (multi-phase) per W4
- One vendor (spend credits)
- 3–5 narrative fragments per W10: data logs, a terminal, one echo fragment
- Onboarding tier active for new saves; standard tier unlocks after first sector clear (per W9)

*Atmosphere/horror pass* (mandatory, not polish):
- Sector lighting palette per W8: deliberate dark zones, `PointLight2D` placement
- Sector audio palette per W8: ambient bed, room-specific cues, silence as a tool
- Pacing: at least one narrative-only room and one quiet "wrong" environmental room per generated layout
- First-room tone-setting: 30 seconds of atmosphere with no text overlay before the first combat

*Polish:*
- Hit-stop, screen shake, particles, sounds tuned for the slice's fights
- Death screen with run summary
- Title screen → vessel select → run → death → vessel select loop fully wired

**Exit criterion:** a stranger can sit down, pick a vessel, play a coherent run of Sector 1 with combat, exploration, items, a vendor, narrative, a mid-boss, and a sector boss. The run *feels* like a sci-fi horror roguelite, not a sprite dungeon. Death loops back to vessel-select with persisted unlocks. Adding the next enemy or item for Sector 2 is a content change, not a code change.

---

## Immediate Next Actions (the next few sessions)

### Closed milestones

**M0 → M4 ✅** — repo cleanup, movement/combat sandbox, hand-built rooms + door transitions, first enemy + save/load, combat-feel pass + debug console, keys/doors/credits/vendor. Six rooms wired (Entry · WestHall · FarRoom · VaultRoom · RewardRoom · VendorRoom). 168/168 Core tests green at M4 close.

### M5 — Procedural Dungeon Generator (next)

**Workshop gate:** W7 ✓ (decisions promoted). M5 is unblocked.

The hand-built `M2Sandbox` layout becomes the algorithm's reference fixture and a debug-tool target — generated layouts replace it as the default at run start.

Suggested session breakdown (mirrors the M3/M4 cadence — one feature, Core-first, then Godot wire-up):

1. **M5-1 — Graph generation (Core)** — spanning tree + back-edges from seed; produces a `DungeonLayout` shaped like `M2Sandbox`. Tests: connectivity, room-count bounds, same-seed-same-graph determinism.
2. **M5-2 — Critical path + room typing (Core)** — Entry→Boss walk, branch-room assignment (item/vendor/secret/narrative/mid-boss), key placement, content-tier tagging from `MetaState` difficulty tier. Tests: invariants from PLANNING.md § Dungeon Generation.
3. **M5-3 — Bulk invariant tests (Core)** — property-style suite running 1000+ seeds × the full invariant set (no soft-locks, key reachable, item room reachable without locked-door traversal, secret-room minimum, layout size bounds).
4. **M5-4 — `DungeonInstantiator` (Godot)** — reads any `DungeonLayout` and spawns rooms from a template pool keyed off `TemplateName`. Replaces `DungeonRoot.ResolveScene`'s hard-coded switch.
5. **M5-5 — Default to generated layouts** — `DungeonRoot` uses a per-run seed (from `RunState`) and the generator instead of `HandBuiltLayouts.M2Sandbox()`. The hand-built layout stays as `HandBuiltLayouts.M2Sandbox()` for the debug console (`tp` between known rooms still works against either).

**Exit criterion** (per the M5 milestone definition above): every randomly-chosen seed produces a layout that passes all invariants; running the game with seed N twice produces the same layout.

### Workshops still pending

W4 (Bosses), W5 (Items), W6 (Synergies), W8 (Sector 1 deep dive), W9 (Difficulty Tier), W10 (Narrative). None gate M5. See the gate table at the top of this file for which milestone unlocks each.
