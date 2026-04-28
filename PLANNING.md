# StationFall — Project Planning Document

## Vision

StationFall is a sci-fi horror roguelite set on a procedurally reconfigured space station. The player wakes up in a disposable artificial body — clone, android, synthetic — with little or no memory. Each run explores the station in real time: top-down free movement, action combat, room-based puzzles, hidden tools, and gradually accumulated knowledge of what happened here. Death is not failure; it is reincarnation. Narrative and unlocks persist across runs.

This is **real-time action**, not turn-based. Reflexes, positioning, and pattern reading drive moment-to-moment play. Roguelite structure drives long-term play.

---

## Genre Pillars

1. **Real-time top-down action** — Zelda-quality movement and combat feel, dodge-driven, hitbox-precise
2. **Roguelite run structure** — procedurally arranged sectors, permadeath per run, persistent meta-progression
3. **Dungeon exploration** — keys, locked doors, secret rooms, item rooms, puzzles, mid-bosses, sector bosses
4. **Mystery through repetition** — the story assembles across runs from data logs, terminals, echoes, environmental detail

---

## Target Feel

**Adjectives:** tense, lonely, curious, deliberate, eerie, satisfying, escalating.

**Reference moments we want to evoke:**
- The first time a Binding of Isaac item completely changes how you play the rest of the run
- The Zelda moment of acquiring the grappling hook and immediately seeing the world differently
- A Returnal log fragment that re-contextualizes a room you walked through ten runs ago
- The sterile, quiet wrongness of Signalis or Dead Space's early corridors before anything attacks
- The Hades cadence: short runs, instant restart, perceptible meta-progress every attempt

**Anti-feel:** floaty, grindy, padded, twitchy without weight, story dumped in cutscenes, run failures that feel like nothing happened.

---

## Vertical Slice Scope

The vertical slice = **Sector 1 (Medical Wing) playable end to end with all systems wired in**.

Definitions of done:
- Vessel select screen with at least one playable vessel (Clone) and one preview-only locked option
- Procedurally generated Medical Wing dungeon (template-pool rooms, valid critical path every seed)
- Real-time combat against the full Sector 1 enemy roster (6 enemies across 5 archetypes — see § Enemy Roster)
- Doors, keys, room state, minimap
- Magnetic Grapple usable in combat AND one of (traversal | puzzle)
- 2–3 acquired passive modules that visibly interact with each other
- One mid-boss and one sector boss, both with multi-phase patterns
- Run currency (credits) drops + at least one in-run vendor
- Meta currency awarded for sector-boss kill
- Death → reincarnation → new vessel select with persisted unlocks/flags
- A handful of narrative fragments: data logs, terminals, one echo fragment
- Atmosphere/horror pass: lighting, audio, room pacing — the slice **feels like a horror game**, not a sprite-painted dungeon crawler

**Not in the vertical slice:** sectors 2–5, full enemy roster, full item pool, polished art, final-ending content, Hard Mode escalation, the inter-sector hub (deferred until a second sector exists). The slice proves the loop is fun once.

---

## Non-Goals

- Turn-based combat or any party/menu-driven combat layer
- 3D rendering or 3D art pipeline
- Multiplayer (co-op or PvP) — single-player only
- Mobile or console targets — desktop Windows first; macOS/Linux later if cheap
- Mod support, level editor, Steam Workshop integrations
- Voice acting, full cinematic cutscenes (light in-engine moments are fine)
- Fully procedural room geometry — template pools first; hybrid layout generation comes later
- An RPG-style dialog system or branching conversation trees
- Crafting trees, gathering loops, or inventory-management busywork

---

## Inspirations

| Aspect | Source |
|--------|--------|
| Roguelite structure, item-room feel, escalation after first ending | Binding of Isaac |
| Narrative through repetition, log fragments, atmosphere | Returnal |
| Action feel, run cadence, permanent upgrades | Hades |
| Dungeon design: keys, locked doors, puzzles, bosses | Zelda (ALttP, Link's Awakening, LANS) |
| Sci-fi horror, environmental storytelling | Dead Space, Signalis, System Shock |

---

## Core Loop

One run:

1. **Vessel select** — choose a body type (more unlock via meta-progression)
2. **Sector entry** — spawn at the entry room of the current sector
3. **Exploration** — real-time movement room to room; fight, solve, find, read
4. **Mid-boss / item / secret detours** — optional risk/reward branches
5. **Boss fight** — defeat the sector boss to descend
6. **Inter-sector rest** — short hub: partial heal, vendor, upgrade choice *(post-vertical-slice; not in M9)*
7. **Repeat** — deeper sectors are harder, stranger, denser with story
8. **Death** — run ends; narrative flags, vessel unlocks, meta currency persist; restart

The station has a true ending. Reaching it requires accumulated knowledge and accumulated unlocks across many runs.

---

## Station Structure

The station is divided into **sectors**. Each is a distinct dungeon with its own atmosphere and mechanic.

| # | Sector | Atmosphere | Signature Mechanic |
|---|--------|-----------|-------------------|
| 1 | Medical Wing | Sterile, too quiet | Contamination locks, bio-seals |
| 2 | Engineering | Dark, industrial, hot | Power routing, heat hazards |
| 3 | Research | Uncanny, wrong | Cloning vats, anomalies, experiment remnants |
| 4 | Command | Corporate, foreboding | Security clearance doors, blackout zones |
| 5 | The Core | Cosmic horror, physics breaking | Reality distortion |

Each sector is a procedurally generated dungeon. Per-sector deep design (room template categories, enemy assignment, hazard set, lighting/audio palette, narrative beats) is **workshop output** — see [WORKSHOPS.md](WORKSHOPS.md).

---

## Dungeon Anatomy

```
Sector Dungeon
├── Entry Room        (always present)
├── Connector rooms   (filler/exploration)
├── Combat rooms      (enemies; doors lock until cleared)
├── Puzzle rooms      (no enemies; environmental challenge)
├── Key Room          (contains a key required for the boss path)
├── Item Room         (1-of-3 passive choice, Isaac-style)
├── Vendor Room       (spend run credits)
├── Mid-Boss Room     (optional; harder fight, better loot)
├── Narrative Rooms   (logs, terminals, environmental storytelling)
├── Secret Rooms      (hidden, reward curiosity)
└── Boss Room         (sector boss; locked behind critical path)
```

The generator guarantees a **solvable critical path**: Entry → (Key) → (Locked Gate) → Boss. Everything else branches.

---

## Systems

### Player / Vessel

The player inhabits a **vessel** — a body type with a starting stat distribution, starting passive(s), a signature ability, and (for one vessel) a unique health-economy mechanic.

#### Design philosophy: Engagement Contract

Each vessel is built around its **engagement contract** — the question *"what does this body ask me to do differently from room to room?"* Stats support the fantasy; the signature completes the loop; HP/speed differences are tuning knobs, not identity.

Every vessel must pass these tests before shipping:
1. **Selection sentence:** *"This is the one that ___."*
2. **Room behavior:** what does the player do differently in a normal combat room?
3. **Boss behavior:** what does the player do differently in a boss fight?
4. **Panic behavior:** what happens when the plan breaks?
5. **Signature dependency:** would the vessel still feel distinct without its signature? If yes, good passive/stat identity. If no, the signature must be extremely reliable and central. If neither, redesign it.

#### Isaac-style portability

Vessel distinctions are mostly *starting* distinctions. Categorize each piece of identity as:

- **Stat distribution** — different starting roll on a shared stat sheet. Trivially portable; any vessel can drift toward another's profile mid-run via pickups.
- **Starting passive / item** — a real entry in the item pool. The vessel starts with it; other vessels can find it (rarely) in hidden rooms, secret rooms, or rare drops.
- **Vessel-unique mechanic** — engine-level change to how the vessel relates to a system. Cannot be picked up. Used **sparingly** — these carry real engineering cost.

The current roster has **exactly one** vessel-unique mechanic: Aberrant's Echo Conversion. Everything else is data.

#### Shared action set

All vessels share the same actions: **Attack**, **Dodge**, **Tool**, **Signature**. The dodge action itself is a **data-driven profile** (`DodgeActionDefinition`: movement vector, duration, i-frames, contact damage on/off, hitbox shape). Default profile is "Roll"; Exo's default is "Shoulder-Charge"; both profiles (and any future ones) are pool-portable as passives.

#### Stat sheet (all values starting; everything modifiable by pickups)

| Stat | Clone baseline | Unit | Notes |
|------|----------------|------|-------|
| Max HP | 5 | small icons | Sector 1 enemies do 1–2 dmg/hit; HP is the most common per-run upgrade |
| Move speed | 120 | px/sec | |
| Attack power | 1 | dmg/hit | |
| Attack rate | 1.5 | swings/sec | |
| Reach | 32 | px | Melee hitbox extent |
| Dodge recharge | 1.5 | sec | |
| Dodge distance | 96 | px | |
| Dodge i-frames | 0.20 | sec (12 frames @ 60fps) | |
| Luck | 0 | abstract | Affects drop *frequency* on random-drop tables (enemy / breakable / locker / room reward). Reduces the table's Nothing-weight; redistributes proportionally across actual drops. **Does NOT affect crit, rarity quality, item rooms, vendors, or scripted rewards.** Cap: +20pp Nothing-reduction from Luck contribution. (W5) |
| Crit Chance | 0 | additive % | Player-side bonus added on top of the weapon's base crit. **Final crit chance = weapon base + Crit Chance bonus + item/passive modifiers.** Independent of Luck. (W5) |
| Armor | 0 | flat | Damage layer that absorbs N points then breaks; pickup-only regen |

#### HP system: Alundra-style icons

- **Small icon** = 1 HP.
- **Big icon** = 10 HP. Auto-promotes when total ≥ 10.
- No half/quarter units. Damage is integer.
- Per-vessel symbology (the vessel-select screen reads visually before any text):

| Vessel | Icon |
|--------|------|
| Clone | Blood drop |
| Android | Battery cell |
| Synthetic | Hexagonal integrity shard |
| Exo | Armor plate |
| Operator | Signal bar |
| Aberrant | Eye glyph |

#### Roster

Six vessels. Clone is the starting "fair default" — normal HP, normal speed, normal dodge, normal attack, signature that activates under pressure. Other vessels bend the base rules.

| Vessel | Selection sentence | Engagement contract |
|--------|--------------------|---------------------|
| **Clone** | *The one that gets stronger when things go wrong.* | Take risks, survive scrappy fights, recover momentum at low HP. |
| **Android** | *The one that wins by reading patterns.* | Wait, time an opening, overclock, punish precisely. |
| **Synthetic** | *The one that survives by never being where the hit lands.* | Constant movement, phase through danger, burst in and out. |
| **Exo** | *The one that can take the hit but cannot easily escape.* | Slow, armored, no dodge roll — wins through commitment and positioning. |
| **Operator** | *The one that fights through tools instead of raw damage.* | Lower direct combat strength, two tool slots, tool synergies, console manipulation. |
| **Aberrant** | *The one that turns danger into power.* | High-risk: damage permanently converts max HP into Echo charge usable as offense. |

#### Per-vessel starting stats (deltas from Clone baseline)

| Vessel | HP | Move | Atk | Rate | Reach | Dodge Recharge / Dist / iFrames | Luck | Armor |
|--------|----|------|-----|------|-------|----------------------------------|------|-------|
| Clone | 5 | 120 | 1 | 1.5 | 32 | 1.5 / 96 / 0.20 (Roll) | 0 | 0 |
| Android | 4 | 110 | 1 | 1.2 | 40 | 1.5 / 96 / 0.20 (Roll) | 0 | 0 |
| Synthetic | 3 | 145 | 1 | 1.7 | 28 | 1.0 / 120 / 0.30 (Roll) | 0 | 0 |
| Exo | 7 | 90 | 1 | 1.0 | 36 | 2.2 / 60 / 0.14 (Shoulder-Charge) | 0 | 4 |
| Operator | 4 | 120 | 1 | 1.5 | 32 | 1.5 / 96 / 0.20 (Roll) | 1 | 0 |
| Aberrant | 6 | 125 | 1 | 1.5 | 32 | 1.5 / 96 / 0.20 (Roll) | 0 | 0 |

All numbers are **playtest-tunable**. Code ships configurable values (`.tres` data files or inspector-exposed consts), not hardcoded magic numbers.

#### Per-vessel signatures

**Clone — Adrenaline Rush** (passive trigger)
On the hit that drops HP to ≤40% max, gain **+50% move** and **+30% attack rate** for **5 sec**. Cooldown **30 sec** from effect end. Healing back above threshold doesn't end the buff. Won't trigger while staggered; queues until stagger ends.

**Android — Overclock** (active)
World time scales to **0.30×** for **2.0 sec**. Player and player-fired projectiles unaffected. UI not affected. Bullets in flight respect the slow. Cooldown **25 sec**. Cannot activate while staggered.

**Synthetic — Phase Shift** (active)
**1.5 sec** intangibility: ignores all damage and passes through enemies / projectiles / hazards (not walls). **+30% move** during. Cannot attack during phase. Refreshes dodge cooldown on activation. Cooldown **18 sec**.

**Exo — Bulwark** (active)
**4 sec** stance: incoming damage **halved** (after armor) and **+6 temp armor** that decays to 0 over the duration. Move speed **−50%** during. Breaks current stagger on activation. Cooldown **35 sec**.

**Operator — Cascade** (active)
Resets both tool cooldowns instantly. **Next 2 tool activations** get **+50%** magnitude or duration (per-tool definition). Doesn't refund consumable charges. Empty tool slot = no effect on that slot. Cooldown **40 sec**.

**Aberrant — Echo Surge** (active, gated by Echo charge)
Consumes Echo charge; effect scales with charge spent at activation:
- **1–2 Echo:** next 3 attacks gain +1 damage and small AoE
- **3–5 Echo:** above + 1 sec invulnerability on activation
- **6+ Echo:** above + screen-clearing pulse (Sector-1 enemies die; elites take 5; bosses take 3)

No timed cooldown; gated entirely by Echo availability. Numbers all playtest-tunable.

#### Vessel-unique mechanic: Echo Conversion (Aberrant)

Every direct enemy hit taken (damage > 0; **not** DOT, not environmental ticks) converts **1 max HP → 1 Echo charge** permanently for the run. Healing restores current HP only; max HP loss is recovered only by specific rare items (one or two in the whole pool, names TBD via W10). Run ends when max HP reaches 0 even at full current HP.

This is the only engine-level vessel mechanic in the roster.

#### Starting passives (all also exist as rare pool items)

The signatures themselves are starting passives for their vessels and exist as rare items the *other* vessels can find. Plus:

| Item | Effect | Starts on |
|------|--------|-----------|
| Adrenaline Rush | Clone's signature as a passive trigger | Clone |
| Overclock | Android's signature as a rare active item | Android |
| Phase Shift | Synthetic's signature as a rare active item | Synthetic |
| Plated | +Armor stat, swaps dodge → Shoulder-Charge | Exo |
| Shoulder-Charge | Replaces dodge profile with Shoulder-Charge | Exo (default); also pool-rare |
| Schoolbag | Adds a second tool slot | Operator |
| Console Hack | Can hack consoles (unlock some doors without keys; reveal extra lore from terminals) | Operator |

These IDs become entries in `Assets/Data/Items/` and `Assets/Data/Tools/` when the content authoring pipeline lands.

#### Unlock gating (initial)

- **Clone** — starting vessel, always available.
- **Android, Synthetic** — meta currency, low cost. One unlock available after the first sector clear.
- **Exo, Operator** — meta currency, mid cost.
- **Aberrant** — gated behind a narrative flag, not just currency. Reaching it should feel like discovering something off-script. Exact flag → W10.

Hard-mode rules (à la Isaac) — when "easy" unlocks "hard", and what hard mode changes — are **W9** territory; W1 specs target the easy-mode baseline.

#### Implementation notes

- Clone's Adrenaline Rush ships in **M1** with the player movement system. A vessel without its signature is incomplete; signatures are not deferrable to "later polish".
- Vessel definitions live in `Assets/Data/Vessels/*.tres`, loaded by `Core/Entities/PlayerVessel.cs`.
- Stat values are starting values — runtime stats live on the player entity and are mutated by passives, items, and tools per the `DamageCalculator` modifier pipeline.

### Combat — Real-Time Action

Spatial, player-skill-driven. Think Link's Awakening crossed with Hades.

**Player actions:**
- **Attack** — melee swing or equipped ranged; hitbox active for a few frames
- **Dodge** — directional; data-driven profile (`DodgeActionDefinition`: movement vector, duration, i-frames, contact damage on/off, hitbox shape). Default is a roll; Exo's default is a shoulder-charge; profiles are pool-portable as rare passives
- **Active tool** — equipped tool slot (grapple, EMP, scanner, turret); cooldown or charges
- **Signature ability** — vessel-specific; rechargeable

**Melee combat model:**
- Default attack input is a **3-hit combo** (light/light/heavy). Combo length is per-weapon and modifiable by pickups (`Refrain` adds +1 step, capped at +2 over weapon base).
- Each combo step has a **6-frame cancel window** at the start of recovery — pressing attack inside it chains; outside it, combo resets.
- **Dodge cancels any frame ≥ end-of-windup.** Attacks cannot cancel into other attacks (only the chain advances).
- **Hit-stop applies to both attacker and target**, target frozen ~2× longer (sells weight on attacker, impact on target).
- **Vessels always carry a weapon.** Each vessel ships with a default weapon matching its identity. Found weapons replace the held weapon; there is no "bare-hands" state.

**Weapon roster (slice):**

| Weapon | Vessel default | Adjective | Combo | Per-hit frames (W/A/R, 60fps) | Reach | Arc | Damage | Hit-stop (target/attacker ms) | Special |
|---|---|---|---|---|---|---|---|---|---|
| Sword | Clone | clean | 3 (L/L/H) | 4/3/8 → 4/3/8 → 8/4/14 | medium | 90° arc | 1/1/2 | 60/30 → 60/30 → 120/60 | — |
| Hammer | Exo | heavy | 2 (H/H) | 10/4/18 → 14/5/22 | medium | 120° overhead | 2/3 | 140/70 → 180/90 | — |
| Dual-blade | Synthetic | fast | 4 (L/L/L/L) | 3/2/5 ×4 | short | narrow flurry | 1×4 | 40/20 each | — |
| Rapier | Android | precise | 3 (T/T/T) | 5/3/9 ×3 | long | thin thrust line | 1/1/3 | 50/25 → 50/25 → 100/50 | Third hit crits if all three thrusts connected; missed thrust resets crit eligibility but combo continues |
| Claws | Aberrant | brutal | 3 (L/L/H) | 3/2/6 → 3/2/6 → 6/4/12 | short | tight 60° | 1/1/2 | 50/25 → 50/25 → 110/55 | Heavy applies Bleed (1 dmg/sec, 3s) |
| Daggers | (found) | deft | 4 (L/L/L/H) | 3/2/5 ×3 → 6/3/10 | short | narrow | 1/1/1/2 | 40/20 ×3 → 100/50 | +1 dmg per hit when attacker is in target's rear 90° cone |

**Combo-extender pickups (handed to W5 for full pool integration):**
- `Refrain` — adds 1 combo step to the held weapon (cap +2 over base)
- `Pirouette` — final combo hit's hitbox becomes 360°

**Damage model:**
- No regen between rooms — careful play matters
- **Shields** — optional layer; absorbs one hit then breaks/recharges per item rules

**HP recovery economy** *(default rules; refined in W5)*:
- Vendors sell heal items (paid in credits)
- Specific consumables (medkits) and rare passives can heal
- Inter-sector hub partial heal *(post-slice)*
- Sector-boss clear triggers a small heal
- **No** heal-on-kill or heal-per-room as defaults — pickup of those is an item-tier perk only

**Enemy archetypes (W3):**
- **Melee rusher** — predictable, punishable charge
- **Ranged shooter** — maintains distance; projectile patterns
- **Shield bearer** — frontal block; flank, break with bash/heavy finisher, or disable with electric
- **Swarm** — small, individually weak, dangerous in groups; tests crowd control
- **Ambient hazard creature** — stationary or near-stationary; teaches "not every threat chases you" and contributes atmosphere
- **Status applier** — ranged support that hits with debuffs (slow, bleed) more than damage; teaches debuff UI and target priority
- **Elite** — variant of any base archetype: modest stat scaling **plus exactly one readable pattern mutation**. Elites never get faster telegraphs.

Full per-archetype detail (Sector 1 roster, elite rules, perception tags, durable principles) lives in [§ Enemy Roster](#enemy-roster).

**Core owns:** damage formulas, status-effect tick logic, enemy AI state-machine definitions, stat application
**Godot owns:** physics, collision, hitbox geometry, animation, audio/visual feedback, input

### Game Feel

Game feel is a first-class system, not a polish pass. It ships in M3.5 (the explicit Combat Feel Pass) and is iterated continuously after.

**Required by M3.5:**
- Hit-stop on melee impact (configurable per attack)
- Screen shake on hit, dodge, and damage taken (trauma-based)
- Particle burst on hit (placeholder OK)
- Dodge i-frames visualized (sprite tint or outline)
- Sound cue on hit, dodge, death (placeholder OK)
- Attack swing timing tables (windup/active/recovery frames per weapon)

**Hit-stop (ms, target / attacker)** — see weapon roster table for per-weapon values. Attacker freeze is half target freeze; both are configurable per attack in `.tres`.

**Dodge profile — default Roll:**
- Total: 12 frames (60fps) — 1 startup, 8 i-frames (frames 2–9), 3 recovery
- Distance: ~96px
- Recharge: 0.6s after recovery ends
- **Post-dodge vulnerability:** the last 3 recovery frames have NO i-frames. This is the punish window — i-frame chaining cannot cover the entire dodge.

**Dodge profile — Shoulder-Charge (Exo default; pool-rare for others):**
- Total: 16 frames — 2 startup, 10 i-frames, 4 recovery
- Distance: ~128px, contact damage 1, knockback on contact
- Recharge: 1.0s

**Screen shake — trauma values (0–1 scale; shake intensity = trauma²):**

| Event | Trauma |
|---|---|
| Light hit landed | 0.15 |
| Heavy hit landed | 0.30 |
| Damage taken (1 HP) | 0.25 |
| Heavy hit taken | 0.50 |
| Dodge i-frames entered | 0.05 |
| Perfect block (Active Shield) | 0.20 |
| Death | 0.80 |

All values ship configurable; numbers are starting points for playtest tuning.

### Enemy AI

State machine **defined in Core, executed in Godot**.

```
Idle → Patrol → Chase → Attack → Stagger → Dead
```

Each transition is a pure function: `(sensorData) → nextState`. Godot enemy nodes gather sensor data each `_PhysicsProcess`, ask Core for the next state, then execute the appropriate movement / animation / attack.

This keeps AI behavior unit-testable without the engine.

### Enemy Roster

Full W3 output. The vertical slice ships the complete Sector 1 roster (6 enemies); Sectors 2–5 are sketched and finalized in their own future passes.

#### Durable principles (apply to every enemy in every sector)

1. **Telegraph floor.** Enemy attack windup is **never below 14 frames at 60fps**. The dodge Roll has 8 i-frames (frames 2–9 of a 12-frame action); shorter windups make dodging unreliable. **Elites never get faster telegraphs** — pattern mutation is allowed, telegraph erosion is not.
2. **Contact damage off by default.** An enemy only deals contact damage if its definition explicitly opts in (most don't — damage comes from telegraphed attacks).
3. **Hazards damage enemies.** Knockback, grapple, and baiting through hazards are valid tactics. Enemies do not get hazard immunity unless their identity is "thing made of fire" or similar.
4. **Sector 1 resistance ceiling = 1.** With player damage at 1–2 per hit, Resistance 2 is functionally Immunity. Use Immunity directly when you mean it.
5. **Elite rule.** Modest stat scaling **plus exactly one readable pattern mutation**. Pure stat scaling is rejected as boring; full pattern overhauls are rejected as a different enemy.
6. **Numbers are playtest-tunable.** Every value below ships in `.tres` data; balance happens at playtest, not on paper.

#### `PerceptionType` enum

| Tag | Meaning | Sector 1 examples |
|-----|---------|-------------------|
| `Vision` | Needs LOS and sufficient light | Twitching Patient, Drip Drone, Bio-Seal Orderly, Corrupted Medbot |
| `Sound` | Reacts to attacks, footsteps, impacts; doesn't need LOS | Suture Mite |
| `Heat` | Detects living vessels better than synthetic/mechanical | Reserved for Sectors 2+ |
| `Omniscient` | Always knows player location | Bosses, special horrors only |
| `Ambient` | Doesn't hunt; acts on timer/proximity | Convulsing Body |

Sector 1 leans `Vision` so darkness matters. `Sound` on Suture Mites makes dark rooms genuinely dangerous without overusing the tag.

#### Shared aggro defaults

| Rule | Default |
|------|---------|
| Idle until aggro condition met | Yes |
| Damage always causes aggro | Yes |
| Most enemies require LOS | Yes (`Sound` and `Omniscient` excepted) |
| Last-known-position memory | Yes |
| Memory duration | 1.5–3.0 sec by archetype |
| Enemies can damage each other with hazards | Yes |
| Enemies can be damaged by room hazards | Yes |
| Contact damage default | Off unless explicitly specified |

#### Sector 1 — Medical Wing (full roster)

##### 1. Twitching Patient — starter enemy, melee rusher

Half-collapsed humanoid, IV stand still attached, lurches at the player. Teaches the entire combat sentence in the first 30 seconds: notice → telegraph → dodge → punish recovery.

| | |
|---|---|
| HP | 2 |
| Contact damage | 0 |
| Attack damage | 1 (physical slash) |
| Idle move | 35 px/sec |
| Chase move | 75 px/sec |
| Lunge speed | 180 px/sec |
| Stagger duration | 10 frames |
| Attack cooldown | 1.25 sec after recovery |
| `PerceptionType` | `Vision` |
| Aggro range | 160 px |
| LOS required | Yes |
| Lose aggro | After 2.0 sec without LOS |
| Dark-room behavior | Cannot detect beyond 80 px unless player attacks or makes noise |

**Attack: IV Lunge** (windup 14 / active 8 / recovery 22 frames @ 60fps) — straight-line lunge ~44 px forward at last-known position. Long recovery is the punish window.

**Tells:** head snaps on aggro · IV pole rattles before lunge · body leans opposite the lunge during windup · drags one foot when overextended.

**Counter:** wait for wind-back, dodge the lunge, attack during recovery. Bait into hazards in later rooms.

**Drops:** 1 cr (40%) · 2 cr (10%) · med scrap (5%) · nothing (45%).

**Resistance:** all damage types 0. No immunities.

##### 2. Drip Drone — first ranged enemy

Dangling med-bot with a syringe gun. Teaches LOS, perpendicular dodging, and ranged-priority.

| | |
|---|---|
| HP | 1 |
| Contact damage | 0 |
| Projectile damage | 1 (physical pierce) |
| Move | 60 px/sec |
| Preferred range | 140 px |
| Projectile speed | 170 px/sec |
| Fire cooldown | 1.5 sec |
| `PerceptionType` | `Vision` |
| Aggro range | 220 px |
| LOS required | Yes |
| Lose aggro | After 1.5 sec without LOS |
| Dark-room behavior | Red targeting laser reveals it before firing |

**Attack: Syringe Shot** (aim telegraph 24 / fire 1 / recovery 28). Red laser locks onto the player's *current* position — leading the laser by moving sideways defeats the shot.

**Tells:** laser sight · charging whine · backs away when player closes.

**Counter:** break LOS, dodge perpendicular, rush during recovery. Prioritize before fighting rushers.

**Drops:** 1 cr (45%) · slingshot ammo *(post-W5)* (10%) · med scrap (5%) · nothing (40%).

**Resistance:** electric −1 (vulnerable). Poison Immune.

##### 3. Convulsing Body — ambient hazard creature

A body on a gurney that periodically convulses and emits a poison cloud. Atmosphere first, combat second.

| | |
|---|---|
| HP | 1 |
| Contact damage | 0 |
| Cloud damage | 1 poison after 0.75s exposure, then 1 poison/sec while in cloud |
| Pulse cooldown | 3.0 sec |
| Cloud duration | 1.25 sec |
| `PerceptionType` | `Ambient` |
| Aggro | N/A |
| Dark-room behavior | Silhouette visible only during convulsion |

**Pattern: Septic Exhale** (warning 30 / active 75 / recovery 75 frames). Never chases. Damages enemies too.

**Tells:** sheet rises and falls before pulse · heart monitor glitches · gas color visible before damage active · sound is gross-but-quiet (no jump-scare).

**Counter:** don't stand in the gas during warning · bait Twitching Patients into the cloud · Gas Mask (W5/W7) makes it harmless.

**Drops:** 1 cr (15%) · med scrap (15%) · nothing (70%).

**Resistance:** fire −1 (vulnerable). Poison Immune.

##### 4. Suture Mite — swarm pressure

Spawns in packs of 3–5. Teaches crowd control, movement, and not tunneling on one target.

| | |
|---|---|
| HP | 1 |
| Contact damage | 0 |
| Attack damage | 1 (physical pierce) |
| Move | 95 px/sec |
| Attack cooldown | 1.1 sec |
| Spawn count | 3–5 per pack |
| `PerceptionType` | `Sound` |
| Aggro range | 130 px (220 px if player attacks nearby) |
| LOS required | No, but walls block pathing |
| Lose aggro | After 3.0 sec without sound or proximity |
| Dark-room behavior | Detects by sound — dark rooms don't disable them |

**Attack: Needle Skitter** (windup 16 / active 5 / recovery 14). Bumped from 12-frame windup to satisfy the telegraph floor in swarm context — overlapping 12-frame windups in a pack made dodging unreliable.

**Hard rule:** no overlapping full-body contact damage. Mite damage comes from the active stab only, never from being touched.

**Tells:** metallic skittering grows louder as they close · brief stop before stabbing · tail flashes/lifts during windup.

**Counter:** wide arcs · keep moving · don't get surrounded · gas vents and fire jets are excellent.

**Drops:** 1 cr (20%) · nothing (80%).

**Resistance:** bash −1 (vulnerable) · electric −1 (vulnerable). Poison Immune. *(No pierce resistance — Android's Rapier and Daggers should not be punished by the player's first swarm fight.)*

##### 5. Bio-Seal Orderly — shield/flank teacher

Containment officer with a shield projector and stun baton. Teaches positioning, flanking, and damage-type counters.

| | |
|---|---|
| HP | 4 |
| Contact damage | 0 |
| Attack damage | 1 (physical bash) |
| Move | 55 px/sec |
| Shielded frontal arc | 120° |
| Attack cooldown | 1.6 sec |
| `PerceptionType` | `Vision` |
| Aggro range | 150 px |
| LOS required | Yes |
| Lose aggro | After 2.5 sec without LOS |
| Dark-room behavior | Carries a small flickering medical lamp — visible in darkness |

**Bio-Seal Brace** (frontal defense rule):
- Frontal slash and pierce: **−1 damage (resistance)**
- Frontal bash: **full damage**
- **Heavy combo finisher (any damage type) ignores the resistance** — Sword/Claws/Daggers' final hit, all of Hammer
- Side and rear attacks: **full damage** regardless of type or position in combo
- **Hazards ignore facing** — gas, fire, electric floor, falling debris all hit normally
- **Electric damage disables the brace for 1.5 sec** — any source

This means Synthetic (Dual-blade, no Heavy) and Android (Rapier, no Heavy) **must flank, use electric, or use hazards** — they cannot brute-force through the front. Sword/Claws/Daggers/Hammer can choose to commit a full combo from the front or flank for efficiency.

**Attack: Restraint Baton** (windup 18 / active 7 / recovery 24).

**Tells:** shield projector hums when frontal guard is active · baton arm lifts slowly before strike · shield flickers during recovery.

**Counter:** circle around · use bash/Heavy finisher/electric · pull or knock into hazards.

**Drops:** 2 cr (45%) · 4 cr (10%) · small medkit (5%) · nothing (40%).

**Resistance:** *(per Bio-Seal Brace above)* electric −1 (vulnerable, also disables brace). Poison 1 (resistance from any direction).

##### 6. Corrupted Medbot — debuff teacher *(scope-up: 6th enemy beyond W3 baseline)*

Ranged support with two attacks (one slow dart, one melee bleed). Teaches the debuff UI, target priority, and that zero-damage hits can still matter. Most expensive Sector 1 enemy to implement; if M9 schedule slips, this is the one to defer.

| | |
|---|---|
| HP | 3 |
| Contact damage | 0 |
| Move | 65 px/sec (chase 75 / retreat 85) |
| Preferred range | 130 px |
| Stagger duration | 8 frames |
| Ranged cooldown | 1.4 sec |
| Melee cooldown | 1.8 sec |
| `PerceptionType` | `Vision` |
| Aggro range | 210 px |
| LOS required | Yes |
| Lose aggro | After 1.75 sec without LOS |
| Dark-room behavior | Diagnostic scanner glows blue before attacks — reveals itself |

**Attack 1: Sedative Dart** (aim 22 / fire 1 / recovery 24). 1 physical pierce + **Slow** debuff: −35% move, −20% dodge distance, **no attack-rate effect**, 2.5 sec, refreshes (no magnitude stack). Projectile 155 px/sec, 1.5 sec lifetime, blocked by walls.

**Attack 2: Emergency Sutures** (windup 16 / active 6 / recovery 30). 0 direct damage + **Bleed** debuff: 1 pierce DOT every 2 sec for 4 sec (2 total), refreshes (no magnitude stack), ignores armor.

**AI priority:**
1. Out of aggro: idle/patrol
2. In preferred range with LOS: Sedative Dart
3. Player too close: Emergency Sutures (panic melee)
4. Dart on cooldown: kite away
5. Damaged recently: retreat 0.5 sec, then resume

**Design intent:** mixing one Medbot with one Twitching Patient creates the lesson "being slowed makes the basic lunge enemy more dangerous" without authoring extra content.

**Tells:** blue laser line + scanner tone (Dart) · surgical arms unfold + pulsing red cross (Sutures) · stapler/suture sound during active frames.

**Counter:** dodge perpendicular to laser · break LOS behind beds/curtains/pillars · prioritize before melee enemies · don't close recklessly when slowed.

**Drops:** 1 cr (35%) · 2 cr (15%) · small medkit (5%) · anticoagulant *(post-W5)* (5%) · nothing (40%). *(Medkit drop rate kept low — status enemies should not become health piñatas.)*

**Resistance:** bash −1 (vulnerable) · electric −1 (vulnerable). Poison Immune.

#### Sector 1 elites

Each Sector 1 base enemy that exists in the slice gets one elite variant. Elites follow the durable elite rule.

**Global elite scaling:**

| Stat | Modifier |
|------|----------|
| HP | +50%, rounded up |
| Move | +10% |
| Attack cooldown | −15% |
| Damage | +0 in onboarding tier; +1 only at higher difficulty tiers |
| Drop quality | +1 tier |
| Visual size / silhouette | +10% scale or stronger silhouette |
| Telegraph | **Never shorter than base** |

**Pattern mutations (one per elite):**

- **Redline Patient** (Twitching Patient elite, HP 3) — after a missed IV Lunge, may perform a second short stumble-lunge with its own 12-frame tell. Teaches "elite means altered pattern, not just more health."
- **Dose Drone** (Drip Drone elite, HP 2) — fires a two-shot burst; second shot retargets after 10 frames; long recovery after the second shot. Teaches "don't relax after the first dodge."
- **Containment Orderly** (Bio-Seal Orderly elite, HP 6) — shield briefly projects a small frontal shockwave after blocking three hits (20-frame telegraph). Electric still disables the brace, which also cancels the shockwave windup. *Note: Aberrant's max Echo Surge does 5 damage to elites, leaving Containment at 1 HP — a deliberate "elites survive your panic button" beat. Revisit after playtesting.*
- **Overdose Medbot** (Corrupted Medbot elite, HP 5) — **Double Dose:** one Sedative Dart, 12-frame pause, second Dart at the player's *updated* position. Both shots keep visible laser tells. Bleed duration on Emergency Sutures extended to 6 sec. Use sparingly — late-Sector-1 elite, not opening rooms.

Suture Mites and Convulsing Bodies don't get elite variants in Sector 1 — swarms scale via pack size, and ambient hazards scale via density and pulse timing.

#### Recommended encounter order (first run)

The first run's room order is generator-driven, but the slice's tier-0 generator should heavily weight the following progression:

1. **Room 1 — tone-setting, no combat.** Broken medical bed, flickering observation window, body bag that twitches once but does nothing, sealed `BIO-SEAL ACTIVE` door. No tutorial text.
2. **Room 2 — first Twitching Patient.** Single enemy, no hazards, room has dodge space. Patient starts facing away; on aggro, turns slowly, then IV Lunges. Player learns the full combat loop here.
3. **Room 3 — two Twitching Patients, staggered activation.** Target awareness without true swarm pressure.
4. **Room 4 — Drip Drone behind cover.** Ranged-priority lesson.
5. **Room 5 — Convulsing Body + Corrupted Medbot.** Hazard exploitation + debuff awareness in one room (the Medbot's slow makes the gas harder to escape — the lessons reinforce each other rather than competing).
6. **Room 6 — Suture Mite pack OR Bio-Seal Orderly.** Generator picks one based on tier; both are valid "graduation" fights.

#### Sectors 2–5 — sketch (full design TBD per future workshops)

5 enemies per sector, one elite per archetype, ~25 enemies total at full game scope. Full design in future passes; this sketch exists so M9's slice doesn't paint Sector 1 into a corner.

| Sector | Sketch roster |
|--------|---------------|
| **2 — Engineering** | Furnace Crawler (fire melee ambusher) · Arc Welder Drone (electric line shooter) · Pressure Hulk (slow heavy charger) · Repair Swarm (heals/shields nearby machines) · Heat-Vent Larva (hazard spawner using fire jets) |
| **3 — Research** | Failed Clone (player-mimic melee) · Phase Aberration (short-range teleport attacker) · Glass-Tank Oracle (stationary psychic/echo turret) · Splitter Mass (divides into smaller bodies on death) · Anomaly Leech (drains tool cooldown / signature charge) |
| **4 — Command** | Security Drone (disciplined ranged patrol) · Riot Frame (shield bearer with stun baton) · Camera Warden (buffs enemies while alarm active) · Lockdown Officer (creates temporary door/laser barriers) · Blackout Stalker (stronger in darkness) |
| **5 — The Core** | Echo Husk (mimics prior player movement) · Gravity Wretch (distorts movement zones) · Null Seraph (disables passives in pulses) · Core Parasite (attaches to hazards, amplifies them) · Reincarnation Error (rare late-game hunter built from failed vessel data) |

These are working names and intents only. They reserve design space and signal which damage types and mechanics each later sector will introduce; nothing in the table is locked.

### Room System

Atomic gameplay unit.

**State lifecycle:** `Unexplored → Entered → Active → Cleared`

**Door taxonomy (W7):**

| Category | Description | When |
|---|---|---|
| Basic — Open / Closed | No requirements. Closed/Open are visual states only — no Interact required. | M2 |
| Locked — generic key | Consumed on use; stack count visible on HUD. **Skeleton Key** is a *rare* run-permanent variant. Key-drop-rate items exist as passives. | M4 |
| Locked — unique key | HUD-pinned, sector-scoped, color-coded (red / blue / green / yellow, Doom-style). | M4+ |
| Barred — kill-clear | Locks behind the player on entry (Hades-style commitment); opens when room is cleared. *Replaces the prior "EnemyLocked" naming.* | M3 |
| Barred — switch / lever | Opens when a paired switch / lever / pressure plate / console is activated. | M2+ |
| Barred — hack | Opens when a paired terminal is hacked. Operator's *Console Hack* bypasses some. | M5+ |
| Barred — one-way | Opens only from one side. Visually telegraphed on the wrong side (visible bars + floor arrow / glyph) so the player never thinks they missed a switch. Reserve the silhouette-only treatment for scripted "you cannot go back" beats. | M5+ |
| Secret | Hidden door behind a wall. Reveals via cracked-wall destructible / no-indicator super-secret / Scanner item / triggered switch or terminal. | M5+ |

**Reset rule:** doors are sticky-open by default — once unlocked or disarmed, they stay openable for the rest of the run. **Exception:** doors wired to a contraption can be intentionally re-closed by the contraption's logic. They never auto-reset.

**Triggered-closed door visual cue:** when a contraption closes a door, the player must be able to identify the trigger — colored conduits/cables run from the trigger to the door, with matching colored panels at both ends.

**Secret rooms (W7):**
- Guaranteed minimum number per sector (procgen invariant)
- Most are *indicated* (cracked walls, faint visual tells)
- 1–2 *super-secrets* per sector with no indicator at all
- Scanner item reveals secrets persistently while held; consumable scan-card variants (Isaac-tarot style) are one-shot reveals

**Interactive prop catalog (W7):**

| Prop | Behavior |
|------|----------|
| Pressure plate | Triggers logic on overlap. Both **one-shot** and **hold-release** variants — hold-release stays armed only while pressed (enables 2-plate puzzles). |
| Conveyor tile | Constant directional movement on standers (player + enemies + projectiles). |
| Vacuum vent | Pulls player toward it; hazard if reached, can be temporarily blocked. |
| Fragile floor | Cracks on first step, collapses on second; broken tile leaves a Zelda-style **pit** that persists for the rest of the room. |
| Terminal | Dual-role: lore readout *and* hackable to activate switches / yield loot. |
| Locker / container | Some unlocked, some locked; lock variants mirror the full door taxonomy (key, barred, hack). |
| Breakable crate / object | Hurtbox; despawns on kill. No drop in M2; rare lootable variants in later milestones. |
| Bed | Decoration only (slice). Future: vessel-specific signature interactions (e.g. Adrenaline refresh) — post-slice. |
| Camera | Scene-dressing only (slice). Alert/aggro mechanic is post-slice. |
| Corpse | Decoration; rare lootable variants (small chance of currency / consumable). |

**Room transition (M2 default):** instant teleport at door crossing; player repositions at the matching door on the destination room. Camera pan / fade is polish for later.

**Room shape model (W7) — Isaac × Link to the Past hybrid:**

- One room visible at a time
- Some rooms are **screen-sized** (camera locked, classic Isaac feel)
- Other rooms exceed the screen and the camera **follows the player within room bounds** (LttP / LANS feel)
- Camera **hard-clamps** to room bounds — no soft drift past the wall
- **Max dimensions:** 2×2 cells default, 3×N permitted for boss arenas / set-pieces
- **Multi-door per wall** allowed (long walls can have 2+ doors, no diagonal exits)
- **Hub rooms** (4+ doors / 4-way junctions) allowed
- Room size is a property of the *template*; content is hand-tuned per template (the generator does not auto-scale enemy count to room size)
- Sectors have **distinctive room-design languages** — each sector ships its own template pool with its own visual feel

**Roomwide mechanics (W7):**
- **Slow zones** — time-dilated regions; slow *everything* (player, enemies, projectiles, attack animations). Distinct from Android's *Overclock*, which preserves player speed.
- **Lights-out rooms** — see *Lights & Power* below.
- **Gravity flips** — *deferred post-slice.* Likely shape: directional-pull tiles or roomwide low-grav drift; design pass when the Engineering sector is built.

**Clear conditions:** combat → all dead; puzzle → solved; narrative/item/secret/vendor → entry only.

**Minimap rules:**
- Visited rooms shown as filled
- Adjacent unvisited rooms shown as outlines (you know they exist)
- Item room icon revealed when player enters an adjacent room
- Boss room icon revealed when player enters an adjacent room
- Secret rooms NOT shown until found
- Vendor and mid-boss rooms revealed on first entry

### Lights & Power

Lighting is a real gameplay mechanic, not just art.

**Default ambient progression:**
- Early/early-mid sectors: most rooms well-lit by default; some *dark rooms* (power off on arrival) exist as exceptions.
- Later sectors invert the default: lit-well becomes the *exception*, dim becomes the default — darkness creeps in as the game gets scarier.

**No-light fallback** (player in a dark room with no light source):
- Low visibility nearby, near-zero at distance — **never fully black**.
- Even unpowered rooms ship with minimal guidance lights to hint at navigation.
- Some enemies produce or carry their own light — silhouettes/glows visible at distance double as a horror beat **and** a navigation aid.

**Light items** (full integration in W5):
- **Flashlight** — rechargeable battery; forward-facing cone.
- **Lantern** — fuel-based; small radial bubble.
- Lantern *or* fire-based attacks can light environmental torches.
- Wall lights with switches need *power* (which itself may be a small puzzle to enable).

**Power scope** — both **per-room** (room-local switch / breaker) and **per-sector** (master breaker that lights several rooms when restored) are valid. Sector grids and room-local power can coexist.

**Torch persistence** — per-torch property. Some torches stay lit for the run once ignited; others burn out on a timer. Mix to taste.

**Enemy perception in dark** — every enemy archetype carries a `PerceptionType` tag (`Vision` / `Sound` / `Heat` / `Omniscient` / `Ambient`). Some are blind in the dark, some see fine, some detect by sound or thermal; `Ambient` enemies don't hunt at all and act on timer/proximity. A killed lantern-carrying enemy may drop their lantern as a lootable light source. Per-enemy assignments live in [§ Enemy Roster](#enemy-roster).

### Hazards

Hazards are environmental damage sources. Some are static traps; some are dynamic; some are tactical opportunities.

**Catalog (slice):**

| Hazard | Type | Behavior | Sector lean |
|--------|------|----------|-------------|
| Spike trap | Persistent | Always-on contact damage (physical-pierce) | universal |
| Gas vent | Cycling | Telegraph → emit gas cloud (poison DOT) → cooldown | Medical |
| Electric floor | Persistent / cycling | Tile-based shock zones; some pulse on/off (electric) | Engineering |
| Fire jet | Cycling | Telegraph → flame bar → cooldown (fire damage) | Engineering |
| Vacuum vent / breach | Persistent | Pulls player toward it; sustained exposure = vacuum DOT | Hull |
| Falling debris | Cycling | Telegraph → drop on tile → impact damage (bash) | Engineering |
| Fragile floor | Triggered | Crack → collapse → pit persists for rest of room | universal |
| Damaging laser — toggling | Cycling | Fixed beam; on/off cycle (electric) | Command / Research |
| Damaging laser — patrolling | Persistent | Beam slides along a track at constant speed | Command / Research |
| Damaging laser — tracking | Persistent | Beam slowly aims at the player; forces movement | Command / Research |

**Sector tagging:** each hazard has a primary sector(s); templates may pull non-primary hazards on purpose for cross-sector flavor. Tagging is metadata on the hazard definition.

**Damage model:**
- **Flat damage** is the default
- **DOT** for occasional hazards (gas, vacuum, bleed)
- **Never percentage-based**

**DOT stacking:**
- **Same effect** from multiple sources → *refreshes* (latest source resets duration; magnitude does not stack)
- **Different effects** stack independently — fire + poison both tick on the same target

**Telegraph rule (slice):** all hazards must telegraph. No gotcha hazards. Cycling hazards have a visible windup + audio cue (~0.5s before activation); always-on hazards are visually distinct from safe terrain.

**Disarm rules:**
- **Mechanical hazards** (gas vents, fire jets, electric floors, lasers, dart traps) are disarmable via **multiple methods**: hack via terminal, shoot the control box, tool-required (Cutter / Hack tool). Operator's *Console Hack* bypasses some.
- **Always-on environmental hazards** (spike grates, contact pools, fragile floors, vacuum vents) typically *cannot* be disarmed. The counter is **protection items** (passives, full integration in W5):
  - `Gas Mask` — gas / poison immunity
  - `Reinforced Soles` — spike-floor protection
  - `Hover Jets` — float over floor-based hazards (spikes, fragile tiles, vents)
- **Permanence:** once disarmed, a trap stays disarmed for the rest of the run. No timer reactivation.

**Hazard tactical use:** the player can intentionally force enemies into hazards via knockback, grapple-pull, or dodge-shove. Hazards damage enemies who wander or are forced in. Some enemies have hazard immunities or partial resistances (e.g. fire imp immune to fire).

### Damage System

Damage is type-tagged at the source and resolved against per-type defenses.

**Damage types — column-set:**

| Category | Types |
|----------|-------|
| Physical | slash, bash, pierce |
| Elemental | fire, electric, poison, cold/cryo |
| Exotic | vacuum, sonic, psychic/echo |

The full enum is defined in code from day one. The **vertical slice** uses *only* physical + fire + electric + poison for enemies and items; cold / vacuum / sonic / psychic stay reserved for later sectors and Aberrant content. Per-type status effects (cold → chill/freeze, vacuum → suffocation DOT, sonic → stun, psychic → ignore-physical-defenses) are **W5 / W6 territory**.

**Defense layers — order of operations per hit:**

1. **Crit roll** — per-hit base rate (per weapon) + Crit Chance stat + passives. On success, multiply final damage by the per-weapon crit multiplier. **Luck does not contribute to crit** (W5 — Luck handles drop frequency only).
2. **Resistance** — per-type **flat per-hit subtraction**. *Resist 2 poison* = subtract 2 from each poison instance applied (min 0). Crits **bypass Resistance**. *Resistance-piercing gear* (passives) reduces an enemy's effective Resistance.
3. **Immunity** — flat 0 damage from that type. Hard cap; not penetrable.
4. **Armor (physical-only)** — flat damage pool that absorbs and depletes. Catches all 3 physical sub-types (slash + bash + pierce) but is **ignored** by elementals and exotics. Crits do **not** bypass Armor — but the doubled (or higher) damage chews armor down faster. Armor regenerates only via pickups.

**Critical hits — starting per-weapon table** (all numbers playtest-tunable):

| Weapon | Vessel | Base Crit Rate | Crit Multiplier | Notes |
|--------|--------|----------------|-----------------|-------|
| Sword | Clone | 5% | 2.0× | clean baseline |
| Hammer | Exo | 3% | 3.0× | rare but devastating |
| Dual-blade | Synthetic | 8% | 1.5× | flurry of mini-crits, lots of rolls |
| Rapier | Android | 15% | 2.5× | reliable crit + W2 third-thrust positional crit |
| Claws | Aberrant | 6% | 2.5× | brutal multiplier — gnaws armor down fast on crit |
| Daggers | (found) | 10% | 2.0× → 3.0× from rear 90° cone | deft + W2 rear-bonus stack |

- **Crit Chance scaling:** Crit Chance stat is additive % on top of weapon base. No baseline cap from the stat; per-passive contribution caps possible.
- Passives can boost crit rate (additive %) and crit multiplier (additive × on base) independently.
- **Crit feedback:** larger damage number, distinct color, brief screen flash, +50% hit-stop on crit.

**Luck — drop-frequency model (W5):**

- Applies to *random-drop tables only*: enemy drops, breakables, lockers, room rewards.
- Each Luck point reduces the table's `Nothing` weight by **2 percentage points**, with a hard cap of **+20pp Nothing-reduction** total from Luck.
- Recovered weight is redistributed **proportionally** across the table's actual drop entries (preserves their relative ratios).
- Luck does **NOT** affect: item-room rarity, vendor stock composition, boss-guaranteed drops, scripted rewards, or crit chance.
- Example — Twitching Patient drop table `[1cr 40%, 2cr 10%, med scrap 5%, Nothing 45%]` at +5 Luck → Nothing becomes 35%, 10pp redistributed proportionally to actual drops by their existing weights (40:10:5).

### Dungeon Generation

Generation lives entirely in `Stationfall.Core`. Godot only instantiates the result.

**Algorithm:**
1. Generate a **room graph** — spanning tree for connectivity, then a few back-edges for loops/shortcuts
2. Walk the graph to place the **critical path** (Entry → Key → Boss)
3. Assign **branch rooms** (item, vendor, secret, narrative, mid-boss)
4. Tag each room with type and **content tier** (enemy difficulty, loot quality, set by current difficulty tier)
5. Godot reads the produced `DungeonLayout` and spawns scenes

**Room content method:** **template pools** to start (curated enemy placements, puzzle configurations). Hybrid (procgen layout + templates) is the goal once the slice ships.

**Generator invariants** (must hold for every seed):
- Connected critical path Entry → Boss
- Every locked door has its key reachable without that key
- No room double-assigned a type
- Item room reachable from Entry without prior locked-door traversal
- Layout size within target bounds (room count min/max per sector)
- **No soft-lock state reachable** — every layout has a verified solvable path to the boss from any state the player could legitimately be in. Sector-spanning puzzles must either have their prerequisite present in the layout or fall back to a non-puzzle alternative path. Layouts that fail this check are rejected and re-rolled. *(W7 rule.)*
- **Secret rooms ≥ minimum per sector** (W7) — guaranteed minimum count per sector, with the indicated/super-secret ratio respected.

**Core produces:** `DungeonLayout` — pure C# room graph, room types, connections, door types, key placements
**Godot instantiates:** room scenes, doors, props

### Items, Tools, Modules

**Categories:**

| Category | Description | Example |
|----------|-------------|---------|
| Passive module | Permanent run-long stat/behavior modifier | "All hits apply 1s slow" / "Gain shield on room entry" / Poison Coat (DoT on hit) |
| Active tool | Equipped slot; cooldown / charges / ammo / battery (per tool) | Magnetic Grapple, Stun Coil, Slingshot, Flashlight |
| Consumable | Single use, occupies inventory slot | Medkit, Flashbang, Breaching Charge |
| Resource pickup | Tops up a tool-internal counter; **not** an inventory item | Slingshot Ammo, Flashlight Battery |
| Vessel upgrade | Improves signature ability | Longer duration, lower cooldown |
| Key item | Unlocks specific doors; not randomized | Sector keycard, override module |

**Acquisition:** item rooms (1-of-3), vendors (spend credits), boss/mid-boss drops, secret rooms, narrative rewards.

**Synergies — design constraint, not feature:** every passive must be designed *with at least one other existing passive in mind*. Lone-wolf passives that don't compose are rejected at workshop time. Synergy categories use a tagging system (full spec in **Item Tagging** below) so the design surface is searchable and synergy chains are intentional.

**Item pool size targets:**
- Slice (M9): ~10 passives, **4 tools**, ~5 consumables, all designed for interaction
- Post-slice (full game): 50+ passives, 10+ tools, 10+ consumables

> Slice tool count is 4, not 3 — Flashlight occupies a *utility/perception* slot distinct from the three combat tools.

#### Item Tagging (W5)

Tags are **authoring / search / UI metadata, NOT the source of gameplay behavior.** Behavior lives in `ItemEffect` / `ToolEffect` definitions and event handlers. Tags exist so designers can query the pool, W6 can locate synergy chains intentionally, and the UI can render player-facing chips.

**Five orthogonal tag axes.** Each item may carry zero or more tags from each axis; tags are static per item definition (stacks change magnitude, duration, or caps — never tag identity).

| Axis | Values |
|------|--------|
| **StatusTag** | slowed · stunned · bleeding · poisoned · burning · frozen · marked · shielded · armored · contaminated |
| **DeliveryTag** | melee · projectile · beam · aoe · deployable · hazard · tool · dodge · combo · self_buff · room_effect |
| **TriggerTag** | always · on_hit · on_damaging_hit · on_crit · on_kill · on_room_entry · on_room_clear · on_dodge · on_perfect_dodge · on_damage_taken · on_low_hp · on_tool_use · on_pickup |
| **RoleTag** | offense · defense · movement · utility · economy · perception · exploration · sustain |
| **EffectScope** | player · enemy · weapon · projectile · tool · room · door · hazard · pickup · vendor · run_state · meta_state |

**Damage type is NOT a tag.** Damage type lives in the existing `DamageType` enum (W7 § Damage System). Tag axes are searchable; damage type is also searchable as an implicit property of the item — it is rendered as a chip in the UI alongside true tag chips, but it is not part of any tag namespace.

**Two intentional name collisions across axes:**
- `DeliveryTag.hazard` (item *creates* hazards — e.g. "drop fire pool on dodge") vs `EffectScope.hazard` (item *modifies* hazards — e.g. "disarm hazards on room entry").
- `DeliveryTag.tool` (item is delivered via the tool slot — i.e. it is a tool) vs `EffectScope.tool` (item *modifies* your tools — e.g. Schoolbag adds a slot).

These are namespaced enums so they cannot collide in code. Documenting here so authors don't conflate them in design discussion.

**Player-visible chips — curated subset only** (rendered on item descriptions):

`DamageType.Electric` · `DamageType.Poison` · `StatusTag.bleeding` · `StatusTag.slowed` · `StatusTag.stunned` · `StatusTag.shielded` · `DeliveryTag.melee` · `DeliveryTag.projectile` · `DeliveryTag.tool` · `DeliveryTag.dodge` · `DeliveryTag.combo` · `EffectScope.hazard` · `crit` (rendered as a chip when an item touches Crit Chance or Crit Multiplier).

Triggers and Scopes are mostly hidden — they're authoring conveniences, not player vocabulary.

#### Magnetic Grapple (W5 — gates M6)

**Design intent:** a positioning verb. The player asks *"can I pull that to me, pull myself to that, or pull something through danger?"* It is **not** primarily a damage tool. It must solve at least two distinct problem types in the slice: combat displacement and traversal.

**MassClass enum** (new, on enemy / level-object definitions):

`Light · Medium · Heavy · Boss · Anchor · Immovable`

**Sector 1 mass mapping:**

| Enemy | MassClass | Notes |
|---|---|---|
| Twitching Patient | Light | Canonical pull-target. Teaches the verb. |
| Drip Drone | Light | Flying — pulled to ground; briefly grounded for stagger duration. |
| Convulsing Body | Medium | Hazard creature — split-pull walks player into a hazard. Read-and-don't-grapple choice. |
| Suture Mite | Light | Grapple targets *one* mite, not the swarm. Others continue swarming. |
| Bio-Seal Orderly | Heavy | Pull-player → into frontal brace. Grapple does not break brace (electric still required, per W3). |
| Corrupted Medbot | Medium | Canonical "pull through hazard" target. |

**Pull rules (per MassClass):**

| Result | MassClass |
|---|---|
| Pull enemy to player; stagger 8 frames | Light |
| Split-pull to midpoint (50/50); both move | Medium |
| Pull player to enemy | Heavy / Elite |
| No effect by default; phases / attack patterns can briefly expose `MassClass: Heavy` or an `AnchorPoint` child object (W4 boss choreography) | Boss |
| Pull player to anchor | Anchor |
| No effect | Immovable |

- **Wall collision mid-pull:** clean stop, no impact damage. (Damage-on-collision is a future synergy item, not default.)
- **Pull resolves with no leftover momentum** — player ends pull stationary, not sliding.
- **Mid-pull death:** if the pulled enemy dies during pull (hazard tick, ally projectile), pull cancels, cooldown starts as if it resolved.
- **Already-stunned enemy + grapple stagger:** `max(currentRemaining, grappleStagger)`, never sum. Prevents trivializing elite stagger lock with grapple-spam.

**Cooldown / range / timing:**

| Knob | Value |
|---|---|
| Range | 220 px |
| Projectile speed | 520 px/sec |
| Windup before fire | 6 frames (0.1 sec) |
| Cooldown after pull resolves | 2.5 sec |
| Cooldown on miss / wall hit | 2.5 sec (full — no spam) |
| Damage | 0 (default) |
| I-frames | None |

All values playtest-tunable.

**State gating — when can the player grapple?**

| State | Grapple? |
|---|---|
| Idle / moving | Yes |
| Attacking | No |
| Dodging | No |
| Staggered | No |
| Already grappling | No (committed until pull resolves or projectile cancels) |
| Charging another tool (e.g. Stun Coil) | No (one tool action at a time) |

**Dodge cancel windows during a Grapple action:**

| Window | Dodge cancels? | Notes |
|---|---|---|
| Windup (6f) | Yes | Full cooldown still incurred. |
| Projectile travel (before hit) | Yes | Full cooldown. |
| Post-attach pull | **No** by default. | Future "Grapple Cancel" passive can enable cancel during pull (W6). |

Being hit during projectile travel does **not** cancel — only stagger / death cancels.

**Reticle iconography — shape first, color optional:**

| Icon | Meaning |
|---|---|
| Inward arrow | Pull enemy to you |
| Double-arrow toward midpoint | Split-pull (Medium) |
| Outward arrow / hook-to-target | Pull self to target |
| Anchor | GrappleAnchor / traversal |
| Broken-link | Invalid / Immovable |

Color may layer in for redundancy/polish, never as primary signal. Accessibility win.

**Aim model:** free-aim with **~10° half-angle cone-snap** (~20° full cone) toward valid targets. Configurable; flag at playtest if loose/tight.

**M6 deliverable scope (4 rooms):**
1. **Tool pedestal room** — guaranteed, grants Magnetic Grapple.
2. **Combat-teach room** — pulling a Light enemy (Twitching Patient).
3. **Hazard-teach room** — pull / split-pull an enemy through a hazard.
4. **Traversal-teach room** — `GrappleAnchor` across a pit/gap.

This satisfies M6's exit criterion (combat use + one of traversal/puzzle): combat × 2 scenarios + traversal. Hazard pull is enriched combat use; not a "third" deferred case.

**Far-side switches, yankable crates, chain-grapple, and ceiling anchors are deferred** post-slice. Reserve a `Grappable` prop class in the data model now to avoid schema churn later.

**Grapple's own tag profile:**

```
Delivery: [tool, projectile]
Trigger:  [on_tool_use]
Role:     [utility, movement, offense]
Scope:    [enemy, player, hazard, door]   // door for far-side switch — deferred
Status:   []                              // Grapple itself applies no status
```

**Synergy hooks W6 should design into:** damage-on-grapple, stagger-extender, marked-on-pull, generic tool cooldown reduction, "Grapple Cancel" passive (allow dodge-cancel during pull). Pulling enemies through hazards is the headline emergent play and works for free — hazards apply standard damage via `DamageCalculator` regardless of how the entity got there.

#### Slice tool roster (W5)

Four tools ship in the slice. Each uses a distinct resource model — variety is intentional, teaches the player different rhythms.

| Tool | Source | Resource | Slice acquisition |
|---|---|---|---|
| **Magnetic Grapple** | W5 | Pure cooldown | Guaranteed via M6 pedestal room (off-pool entirely) |
| **Stun Coil** | W2 | Charge-up + post-release cooldown | Random — vendor stock or item room |
| **Slingshot** | W2 | Ammo + short per-shot cooldown | Random — vendor stock or item room |
| **Flashlight** | W7 | Battery (depletion over time while on) | Random — vendor stock or item room |

**No vessel starts with a tool equipped.** Operator's `Schoolbag` (second tool slot, W1) and `Console Hack` (passive, W1) provide identity without forcing a starting tool. *Long-term Operator note:* may need a weak/basic starting tool or guaranteed early tool-room bias because "the one that fights through tools" feels awkward with empty slots — defer until tool pool is implemented and tested.

**Tool slot rules:**
- One tool slot for most vessels; two for Operator (`Schoolbag`).
- Tools are unique per-run (no duplicates in the same slot).
- **Pickup-when-occupied:** swap-prompt (replaces current tool with prompt, not silent replace). Same UX pattern applies to consumables-at-cap.

**Implementation order (target):**
1. Magnetic Grapple — M6 (foundation).
2. Slingshot — opportunistic next, piggybacks on Grapple's projectile infrastructure.
3. Stun Coil — once status / electric damage / charge-up resource models mature.
4. Flashlight — once W7 lighting / perception work lands for M9.

#### Deferred / reserved tools

Concept-reserved, not specced. Re-evaluate when the gating sector or system actually lands.

| Tool | Source | Reserved for | Notes |
|---|---|---|---|
| Lantern | W7 | Post-slice | Radial-light variant of Flashlight. Different shape, not a collapse candidate. |
| Active Shield | W2 | Post-slice | Needs directional defense + perfect-block timing systems. |
| Scanner | W7 | Post-slice | Persistent variant of scan-card consumable. Slice covered by scan-cards. |
| **Maintenance Tool** | W7 (collapse) | Post-slice | **Cutter + Hack Tool collapsed** into one design problem when puzzle complexity demands it. |
| EMP Burst | PLANNING categories | Reserved (Engineering sector) | |
| Deployable Turret | PLANNING categories | Reserved (Operator-leaning) | |

**Vessel signatures (Overclock, Phase Shift) stay out of the W5 tool pool.** They are vessel-bound abilities, not pool tools. May appear later as rare pool items per W1's portability rule, but that's a W6+ decision.

#### Pool tiering & rarity (W5)

**Four tiers — defined by *role*, not raw power:**

| Tier | Role |
|---|---|
| **Common** | Stat bumps, simple modifiers, low individual impact, stack-friendly. Most pool churn. |
| **Uncommon** | Single meaningful effect, mild build influence. |
| **Rare** | Build-defining or run-shaping. Vessel starting passives appearing as pool-portable rares. |
| **Cursed** | Strong upside + meaningful drawback (Isaac-style). Player-chosen, never forced. |

The full enum (including `Cursed`) ships from day one even though slice ships zero cursed items — schema-cost is zero, retrofitting is migration work.

**Cursed model:**
- Each cursed item has a strong primary effect + clear drawback. The player evaluates the trade.
- **Hard cap: max 3 cursed items active per run.** Prevents stack-and-ascend degenerate builds.
- **Boss drops are clean rewards — never cursed.** Cursed acquisition routes go through cursed rooms, bargain rooms, secret rooms, or explicit choice prompts. Boss-drop chance for Cursed is `0%`.
- Sample cursed seeds for W5-passives session: `Bloodletting` (+50% melee dmg / lose 1 HP per 30s — Aberrant interaction), `Greedy Fingers` (vendors charge double / item rooms offer 4 pedestals), `Static Field` (all hits crit / +50% dmg from electric).

**Pool composition targets** (content authoring targets — what % of *designed items* exist at each tier):

| Tier | Slice (M9) | Full game |
|---|---|---|
| Common | ~50% | ~50% |
| Uncommon | ~30% | ~30% |
| Rare | ~20% | ~15% |
| Cursed | 0% (slice ships zero cursed items) | ~5% |

Slice's missing 5% (cursed) lifts Rare from 15% → 20% — slice runs feel slightly more rare-dense to compensate for the missing risk-reward axis.

**Per-source roll weights** (runtime tuning — given a drop event from source X, chance of each tier):

| Source | Common | Uncommon | Rare | Cursed |
|---|---|---|---|---|
| Item room (3 pedestals) | 25% | 50% | 25% | 0% (slice) |
| Vendor stock | 65% | 30% | 5% | 0% (vendors don't sell cursed) |
| Boss drop | 0% | 0% | **100%** | 0% (clean reward — never cursed) |
| Mid-boss drop | 0% | 40% | 60% | 0% |
| Secret room | 15% | 40% | 35% | 10% (slice: 15% / 40% / 45% / 0% — see roll-up rule) |
| Cursed room | (deferred — design with cursed acquisition path) |

**Tier-disabled roll-up rule:** when a roll lands on a disabled tier, weight transfers to the **next-most-rare still-available tier**. Slice example: secret-room baseline `15/40/35/10` with Cursed disabled becomes `15/40/45/0` (the 10pp Cursed weight transfers to Rare). Same rule applies to any tier-disabled scenario.

**Vendor restock:**
- Single stock per vendor instance — once items leave the shelf, slot stays empty until next run.
- No reroll mechanic in slice (post-slice "Reroll" tool / consumable possible).
- **Vendor stock size: 4 items.** Mix biased per the table above.
- Stock is deterministic from seed.

**Per-run uniqueness:**

| Item type | Duplicate allowed in same run? |
|---|---|
| Common / Uncommon / Rare / Cursed passive | No (until pool exhausted, then re-enable) |
| Stackable passive (Refrain, +max-HP variants) | Yes — up to documented stack cap |
| Tool | No (tool slot replaces on pickup with swap-prompt) |
| Consumable | Yes (up to inventory cap) |

**Consumable inventory model:**
- **Cap: 3 total consumables held simultaneously** (not per-type — total slots).
- Pickup-when-full uses the same swap-prompt UX as tools.
- **Resource pickups (Slingshot Ammo, Flashlight Battery) do NOT count toward the 3-slot cap.** They top up tool-internal counters and are a separate entity class (`ResourcePickup`) in the data model.

**Slice consumable pool:**

| Tier | Consumables |
|---|---|
| Common | Small Medkit, Slingshot Ammo (resource — not a slot), Battery (resource — not a slot) |
| Uncommon | Anticoagulant (W3 — cleanse Bleed), Scan Card (W7 — one-shot reveal), Flashbang |
| Rare | Breaching Charge, Big Medkit |

No cursed consumables — the cursed tradeoff doesn't bite for short-lived single-use items.

#### Open W5 work (future sessions)

- **Passive roster** — full design pool (~10 slice / ~50 full game). Tags + tier per item.
- **M7 synergy chain** — 2–3 passives with explicit pairwise synergies. Coordinated with W6.
- **Full consumable pool design** — beyond the slice list above; rule details (e.g. pickup-up animation, can-throw-during-attack, etc.).
- **Cursed acquisition path** — cursed room and/or bargain room mechanics, if/when cursed items are added to slice. Currently deferred.

### Currency

Two-tier system.

**Run currency — Credits**
- Source: enemies, breakable environment (crates, lockers), chests, mini-boss drops, secret rooms
- Spend: in-run vendors, NPCs, vending machines, lock-bypass devices
- Resets to zero on death

**Meta currency — *(working name TBD: Echoes / Imprints / Memory Shards — pick in W10)***
- Source: rare — sector boss kills, first-time discoveries, milestone achievements (e.g., first time clearing Sector 1 without taking damage)
- Spend: persistent unlocks at the meta-progression hub — new vessels, mirror-style stat upgrades, item-pool additions, starting inventory, shortcut unlocks
- Persists across all runs

### Difficulty Escalation

Like Isaac, the game eases newer players in and ramps stakes after early progression. Tier transitions are stored in `MetaState` and read at run-start to seed generation difficulty.

| Tier | Trigger | Mechanical changes *(placeholder — finalized in W9)* |
|------|---------|------------------------------------------------------|
| 0 — Onboarding | Default for new save | Smaller dungeons, fewer enemies/room, no elites, smaller boss pool |
| 1 — Standard | First sector-1 boss kill | Full enemy density, elites begin spawning, full item pool, full boss pool |
| 2 — Escalated | "Easy ending" cleared OR N total boss kills (TBD) | New enemy variants, multi-phase elites, environmental hazard frequency up, deeper sectors unlocked |
| 3 — True Path | Specific narrative-flag accumulation | Hidden boss variants, true ending content gated, top-difficulty rooms |

**W9 must produce:** the precise per-tier multipliers (room count, enemy density, elite spawn rate, vendor stock quality, boss pool, hazard frequency) and the precise tier-up trigger formula.

### Puzzle System

Puzzles exist at **room** scope and **sector** scope.

**Slice primitives (W7):**
- Switch / pressure plate (one-shot or hold-release)
- Pressure-plate combinations (multi-plate sequences)
- Enemy-clear gate (the kill-clear barred door)
- Terminal code entry (code found elsewhere in the dungeon)

**Post-slice primitives** (deferred):
- Power-node routing
- Light/laser redirect
- Item-carry puzzles
- Timed sequence puzzles
- Environmental physics (push to plate, redirect beam)

**Sector-spanning puzzles** — multi-room logic where a hint, code, or item found in one room is used in another.
- **Frequency:** at least one guaranteed per run.
- **Persistence:** per-run only — codes you find this run do *not* carry into future runs.
- **Failure tolerance:** none. The no-soft-lock invariant means the generator must guarantee a solvable path or a non-puzzle alternative.

**Discovered codes log** — when the player finds a code, hint, or puzzle item, it pins to a HUD-accessible log so the player doesn't have to memorize anything.

Puzzle state lives in the room's Godot scene. Completion emits a signal the room listens for. Cross-room puzzle state is owned by the run state (`Stationfall.Core.Runs.RunState`).

### Narrative & Mystery

Story is delivered through **discovery, not exposition**. We use *all* of the following channels:

- **Data logs** — found in rooms; written documents from station crew
- **Terminals** — interactive screens; can reveal map, open seals, give codes, react to flags
- **Echo fragments** — the player's own fragmented memories; lore about the player's origin
- **Environmental storytelling** — the room itself tells the story
- **Light in-engine moments** — short scripted beats (a vat opening, a corpse twitching, a voice on the comms) — never long, never blocking

**Echo fragment persistence rules:**
- Echo locations are **fixed** per sector (not random)
- An echo fragment is **collected once per meta-save** — re-runs do not respawn collected echoes
- Stored in `MetaState` as collected-echo IDs
- The player can re-read collected echoes from the meta-progression hub

**Narrative flags:**
- String keys marking discoveries — `"found_dr_chen_log_1"`, `"saw_cloning_vat_sector_3"`
- Persist across all runs in the meta save
- Some terminals/rooms react when set: "You remember this room."
- Gate true-ending content

Master story document, log subjects, echo timeline, true-ending arc — **W10 output**.

### Meta-Progression

**Persists across runs:**
- Narrative flags
- Vessel unlocks
- Station shortcuts (find a hidden path once → it persists)
- Mirror-style upgrades — small permanent stat buffs purchased with meta currency
- Item-pool additions (unlocked items become possible drops in future runs)
- Difficulty tier state
- Collected echo fragment IDs
- Meta currency balance

**Does not persist:** items, keys, run stats, consumables, room progress, credits.

### Save System

Three save layers, distinct lifecycles, **no save-scumming**.

| Layer | When written | Lifetime | Purpose |
|-------|--------------|----------|---------|
| **Meta save** | Auto on **run completion** (death, sector boss kill, true ending) | Persistent across all runs | Narrative flags, vessel unlocks, mirror upgrades, meta currency, collected echoes, difficulty tier |
| **Hard run save (inter-sector)** | Auto when player enters a *safe zone* between sectors | Per-run; cleared on death or run completion | Run state at the safe zone — player commits to advancing or quitting cleanly |
| **Quick save** | Manual via Escape menu → *Save and Quit* | Single slot; consumed on Continue | Pause-and-resume only — for stopping mid-room and picking back up later |

**Quick save rules — designed to forbid save-scumming:**
- One slot. Saving overwrites whatever's there.
- Loading via *Continue* **consumes** the slot (wiped immediately at load).
- Selecting *New Run* from the main menu also wipes the quick save.
- Death never sees the quick save — it was already consumed at load time.
- Effect: the quick save is a *bookmark*, not a checkpoint. Cannot be used to retry a fight.

**Safe zones** — short rooms between sectors where the player can voluntarily stop, save, and quit. Slice scope is M9-deferred; placeholder presentation is fine until then.

JSON format for all three. Save serialization is introduced in **M3**, not M8 — all Core types are designed with serialization as a day-one constraint.

**Schema versioning strategy:**
- **Pre-1.0 (development):** every save file has a `schemaVersion` field. On mismatch, the save is wiped with a warning shown to the player. Wipes are expected during development.
- **Post-1.0:** version field with explicit migration path for each bump where feasible; otherwise wipe with backup file written to `meta_save_backup_v{N}.json`.

---

## Architecture

### Core / Godot Boundary

**Rule: Core decides. Godot displays.**

`Stationfall.Core` has no `using Godot;` — ever. It is fully unit-testable.

**`Stationfall.Core` (pure C#):**
- `EntityStats` — HP, move speed, attack, defense, modifiers
- `DamageCalculator` — pure math; `(attackerStats, defenderStats, modifiers) → DamageResult`
- `StatusEffect` — definitions and tick logic
- `EnemyAiBrain` — state-machine definition and transition functions
- `ItemDefinition` / `ToolDefinition` — definitions and application functions
- `DungeonGenerator` / `DungeonLayout` — pure C# room graph
- `RunState` — current sector, room graph, held items, keys, HP, run flags, credits
- `MetaState` — persistent flags, unlocks, mirror upgrades, meta currency, difficulty tier, collected echoes
- `SaveSerializer` — JSON in/out, with schema versioning
- `RngService` — seeded; deterministic generation

**`Scripts/` (Godot-facing C#):**
- `PlayerController` — input, `CharacterBody2D` movement, dodge roll
- `HitboxComponent` / `HurtboxComponent` — `Area2D` collision detection
- `EnemyController` — runs `EnemyAiBrain` per physics frame, drives movement and animation
- `RoomController` — room state, door signals, enemy spawning
- `DungeonInstantiator` — reads `DungeonLayout`, spawns rooms
- `ItemPickup`, `Vendor`, `Terminal`, `DataLog` — interaction nodes
- `DebugOverlay` — dev tools (god mode, room teleport, set HP, give item, print seed)
- HUD, menus, VFX, audio

**Testing posture:**
- All Core logic with formulas, transitions, generation steps, or unlock rules **must** have a unit test
- Godot scripts are **tested by playing the scene**, not by code
- This is acceptable *only because* Godot-side logic is kept thin — if a Godot script grows real branching logic, that logic moves to Core

### Damage Pipeline

```
Player input (Godot PlayerController)
  → Hitbox overlaps enemy HurtboxComponent (Godot Area2D)
  → DamageCalculator.Calculate(attackerStats, defenderStats, modifiers) (Core)
  → EntityStats.ApplyDamage(result) (Core mutates stats)
  → DamageResult returned to Godot
  → Visual feedback: numbers, screen shake, hit-stop, animation (Godot)
  → Death check (Core) → death event → Godot plays death sequence
```

### Enemy AI Pipeline

```
Core EnemyAiBrain: state set + transition function (sensorData) → nextState

Godot EnemyController, each _PhysicsProcess:
  1. Gather sensor data (distance, line-of-sight, HP ratio)
  2. brain.Tick(sensorData) → new state
  3. Execute: move CharacterBody2D, trigger animation, spawn attack hitbox
```

### 2D vs 3D

**2D top-down. Resolved.**

Godot 4 Forward Plus supports 2D normal-map lighting, dynamic shadows, and post-processing — enough for genuine atmosphere without a 3D pipeline. Viewport: 1280×720, stretch mode `canvas_items`.

---

## Content Authoring Pipeline

By M9 the project will have ~3 enemies, ~10 room templates, ~10 passives, ~5 narrative fragments. Post-slice it grows 5× or more. Adding content cannot require recompiling C#.

**The pattern:**
- **Definitions** (the schema — what fields an enemy/item/room has) live in `Stationfall.Core` as records
- **Instances** (a specific enemy, a specific item) live as **Godot Resource files** (`.tres`) under `Assets/Data/`
- A `DefinitionLoader` in Godot reads `.tres` files at startup, hands `Definition` records to Core, and Core uses them as data
- Hot-reload during play is a stretch goal

**Folder layout:**
```
Assets/Data/
├── Enemies/         # one .tres per enemy
├── Items/           # one .tres per passive
├── Tools/           # one .tres per tool
├── Consumables/
├── Vessels/
├── RoomTemplates/   # template metadata; rooms themselves are .tscn scenes in Scenes/Rooms/
└── Narrative/       # log text, echo fragments
```

This pattern is set up in **M3 or M4** so that early enemies/items already use it. Workshop W3, W5, W7, W8, W10 produce content that lands in these files.

---

## Atmosphere & Horror Direction

Horror is a stated pillar but easily lost in mechanical work. Explicit guarantees:

- **M9 includes a deliberate atmosphere/horror pass** — not optional polish
- Per-sector lighting palette and audio palette are workshop output (W8) and are *applied*, not just planned
- Pacing rule: not every room is combat. The slice should include narrative-only rooms and quiet "wrong" environmental rooms
- Sound design uses absence as a tool — silence between rooms, sound that *stops*, ambient hums that suddenly change
- Lighting uses `PointLight2D` with deliberate dark zones; not every room is fully lit
- The first room of any new playtester's first run should establish tone in 30 seconds without a single text box

**Anti-patterns to avoid:**
- Music constantly playing (kill atmosphere)
- Every room same lighting setup
- Tutorial text overlays explaining horror
- Jump scares (cheap; not the design)

---

## Design Workshops

Several systems need brainstorming and concrete decisions before the milestones that depend on them can ship. Workshops are run as conversations in their own session, with output landing in this PLANNING.md (canonical) and tracked in [WORKSHOPS.md](WORKSHOPS.md) (working notes).

**Why workshops matter:** PLANNING.md describes archetypes; WORKSHOPS produce *the actual roster*. Archetype-level design is not enough to build M3+ — the generator needs concrete enemies, the item room needs concrete items, the boss room needs a concrete boss.

**Workshop list:**

| # | Topic | Gates milestone(s) |
|---|-------|---------------------|
| W1 | Vessels & Signature Abilities | M1 |
| W2 | Combat Feel & Weapon Patterns | M1, M3.5 |
| W3 | Enemy Roster & Archetype Detail | M3, M9 |
| W4 | Bosses & Mid-Bosses | M8, M9 |
| W5 | Items: Passives, Tools, Consumables | M6, M7, M9 |
| W6 | Synergies | M7 |
| W7 | Dungeon Elements & Mechanics | M2, M5 |
| W8 | Dungeon Types & Sector Themes (Sector 1 deep dive) | M9 |
| W9 | Difficulty Tier Mechanics | M8 |
| W10 | Narrative Architecture (incl. meta-currency naming) | M9 |

See [WORKSHOPS.md](WORKSHOPS.md) for per-workshop pre-reads, output goals, and brainstorm notes.

---

## Folder / Project Structure

```
station-fall/
├── Stationfall.sln
├── Stationfall.Godot.csproj
├── project.godot
├── PLANNING.md                    # canonical design (this file)
├── CLAUDE.md                      # instructions for Claude sessions
├── ROADMAP.md                     # milestone tracker
├── WORKSHOPS.md                   # brainstorming agendas + working notes
│
├── Scripts/                       # Godot-facing C# (extends Node*, uses Godot APIs)
│   ├── Bootstrap/                 # Game entry, scene loaders, autoloads
│   ├── Player/                    # PlayerController, vessel runtime
│   ├── Enemies/                   # EnemyController, archetype scripts
│   ├── Combat/                    # HitboxComponent, HurtboxComponent, ProjectileNode
│   ├── Dungeon/                   # RoomController, DoorNode, DungeonInstantiator
│   ├── Items/                     # ItemPickup, ToolNode, Vendor, ConsumableUse
│   ├── Puzzles/                   # Switches, terminals, pressure plates
│   ├── UI/                        # HUD, menus, vessel-select, log reader, minimap
│   ├── Narrative/                 # Terminal, DataLog, EchoFragment nodes
│   └── Debug/                     # DebugOverlay, dev keybinds
│
├── Scenes/                        # Godot .tscn files
│   ├── Bootstrap/                 # Title, vessel select, run start
│   ├── Player/                    # Player scene per vessel
│   ├── Enemies/                   # Enemy scenes per archetype
│   ├── Rooms/                     # Room template scenes per type & sector
│   ├── UI/                        # HUD, menus, log reader
│   └── FX/                        # Reusable VFX scenes
│
├── Assets/                        # Art, audio, fonts, data
│   ├── Sprites/
│   ├── Audio/
│   ├── Fonts/
│   └── Data/                      # .tres definition files (see Content Authoring Pipeline)
│       ├── Enemies/
│       ├── Items/
│       ├── Tools/
│       ├── Consumables/
│       ├── Vessels/
│       ├── RoomTemplates/
│       └── Narrative/
│
└── src/
    ├── Stationfall.Core/          # Pure C#, no Godot refs
    │   ├── Entities/              # EntityStats, PlayerVessel, EnemyDefinition
    │   ├── Combat/                # DamageCalculator, StatusEffect, DamageResult
    │   ├── Ai/                    # EnemyAiBrain, AiState enum, SensorData
    │   ├── ProcGen/               # DungeonGenerator, DungeonLayout, RoomTemplate
    │   ├── Items/                 # ItemDefinition, ItemEffect, item pool
    │   ├── Tools/                 # ToolDefinition, tool effect application
    │   ├── Currency/              # Credit + meta currency rules
    │   ├── Progression/           # MetaState, mirror upgrades, difficulty tier
    │   ├── Narrative/             # NarrativeFlag registry, log/echo definitions
    │   ├── Runs/                  # RunState
    │   ├── SaveData/              # SaveSerializer, save schema, versioning
    │   └── Rng/                   # RngService
    │
    └── Stationfall.Tests/         # xUnit tests against Core only
        ├── Combat/
        ├── ProcGen/
        ├── Items/
        ├── Progression/
        └── SaveData/
```

**Guidelines:**
- One folder per concern. If a Core folder grows past ~10 files, split.
- Godot `Scripts/` folders mirror `Scenes/` folders where possible.
- Tests mirror Core folders.
- No business logic in Scenes/ or Assets/ — definitions only.

---

## Resolved Decisions

| Decision | Resolution |
|----------|------------|
| 2D vs 3D | **2D top-down** |
| Save format | **JSON** for both run and meta saves |
| Save schema versioning | **Dev: wipe on schema mismatch with warning. Post-1.0: version field + migration where feasible, else wipe with backup.** |
| Save introduction milestone | **M3** (not M8) — Core types designed with serialization as day-one constraint |
| Room layout method | **Template pools** to start; **hybrid (procgen + templates)** is the long-term goal |
| Run currency | **Credits** — drops from enemies, environment, chests; spent in-run at vendors/NPCs/vending machines |
| Meta currency | **Yes** — name TBD (W10 deliverable); rare; awarded for boss kills and milestone achievements; spent on persistent unlocks |
| Mirror-style permanent upgrades | **Yes** — purchased with meta currency |
| Difficulty escalation | **Yes — Isaac model, four tiers.** Mechanical specifics in W9 |
| Narrative delivery | **All channels** — data logs, terminals, echo fragments, environmental storytelling, and short in-engine scripted beats |
| Echo fragment persistence | **Once per meta-save** at fixed locations; stored in MetaState; re-readable from hub |
| Inter-sector hub in M9 | **No** — deferred until a second sector exists |
| HP recovery defaults | **No regen, no heal-on-kill** by default; vendors, consumables, rare passives, and post-boss small heal are the only sources |
| Minimap reveal rules | **Visited filled; adjacent outlined; item/boss/vendor icons revealed by adjacency; secrets hidden until found** |
| Game-feel pass timing | **M3.5** — not deferred to M9 |
| Content authoring | **Godot Resource (.tres) files** under `Assets/Data/` loaded into Core as definition records |
| Debug tooling | **M0/M1** — overlay with god mode, give item, set HP, room teleport, print seed |
| Synergy design | **Constraint, not feature** — every passive must compose with at least one existing passive |
| Godot test posture | **Tested by play.** Logic minimized in Godot scripts so this is acceptable |

---

## Milestone Roadmap

See [ROADMAP.md](ROADMAP.md) for the M0–M9 plan, exit criteria, workshop gates, and the immediate next session.
