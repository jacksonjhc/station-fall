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
- Real-time combat against 2–3 enemy archetypes
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

The player inhabits a **vessel** — a body type with base stats, a unique signature ability, and (in some cases) a passive identity trait.

| Vessel | HP | Move Speed | Signature Ability |
|--------|----|----|------------------|
| Clone | 100 | Medium | Adrenaline Rush — speed burst at low HP |
| Android | 80 | Medium | Overclock — brief time-slow on activation |
| Synthetic | 60 | Fast | Phase Shift — brief intangibility |

Concrete numbers (move speed in px/sec, HP, signature cooldown/duration/magnitude, edge cases) are **workshop output** — Workshop W1 in [WORKSHOPS.md](WORKSHOPS.md). Additional vessels beyond these three are also W1 output.

All vessels share the same action set (attack, dodge, tool, signature). Stats and signature define the feel.

**Implementation note:** Clone's Adrenaline Rush ships in M1 with the player movement system. A vessel without its signature is incomplete; signatures are not deferrable to "later polish".

### Combat — Real-Time Action

Spatial, player-skill-driven. Think Link's Awakening crossed with Hades.

**Player actions:**
- **Attack** — melee swing or equipped ranged; hitbox active for a few frames
- **Dodge roll** — directional; i-frames; short cooldown
- **Active tool** — equipped tool slot (grapple, EMP, scanner, turret); cooldown or charges
- **Signature ability** — vessel-specific; rechargeable

**Damage model:**
- No regen between rooms — careful play matters
- **Shields** — optional layer; absorbs one hit then breaks/recharges per item rules

**HP recovery economy** *(default rules; refined in W2/W5)*:
- Vendors sell heal items (paid in credits)
- Specific consumables (medkits) and rare passives can heal
- Inter-sector hub partial heal *(post-slice)*
- Sector-boss clear triggers a small heal
- **No** heal-on-kill or heal-per-room as defaults — pickup of those is an item-tier perk only

**Enemy archetypes (template — full roster is W3):**
- Melee rusher — predictable, punishable charge
- Ranged shooter — maintains distance; projectile patterns
- Shield bearer — frontal block; flank or break with a tool
- Hazard spawner — drops mines, summons drones
- Elite — named variant of any archetype, harder, drops better loot

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

Concrete numbers (hit-stop ms, shake trauma units, i-frame counts) are **workshop output** — W2.

### Enemy AI

State machine **defined in Core, executed in Godot**.

```
Idle → Patrol → Chase → Attack → Stagger → Dead
```

Each transition is a pure function: `(sensorData) → nextState`. Godot enemy nodes gather sensor data each `_PhysicsProcess`, ask Core for the next state, then execute the appropriate movement / animation / attack.

This keeps AI behavior unit-testable without the engine.

### Room System

Atomic gameplay unit.

**State lifecycle:** `Unexplored → Entered → Active → Cleared`

**Door types:**
- Open
- Enemy-locked (seals on enter, opens on clear)
- Key-locked (specific key)
- Condition-locked (item, ability, puzzle)
- Secret (hidden until adjacent or scanner used)

Additional door variants (timer-locked, hazard-gated, factional, etc.) are **workshop output** — W7.

**Clear conditions:** combat → all dead; puzzle → solved; narrative/item/secret/vendor → entry only.

**Minimap rules:**
- Visited rooms shown as filled
- Adjacent unvisited rooms shown as outlines (you know they exist)
- Item room icon revealed when player enters an adjacent room
- Boss room icon revealed when player enters an adjacent room
- Secret rooms NOT shown until found
- Vendor and mid-boss rooms revealed on first entry

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

**Core produces:** `DungeonLayout` — pure C# room graph, room types, connections, door types, key placements
**Godot instantiates:** room scenes, doors, props

### Items, Tools, Modules

**Categories:**

| Category | Description | Example |
|----------|-------------|---------|
| Passive module | Permanent run-long stat/behavior modifier | "All hits apply 1s slow" / "Gain shield on room entry" |
| Active tool | Equipped slot; cooldown or charges | Magnetic Grapple, EMP burst, Scanner, Deployable Turret |
| Consumable | Single use | Medkit, Flashbang, Breaching Charge |
| Vessel upgrade | Improves signature ability | Longer duration, lower cooldown |
| Key item | Unlocks specific doors; not randomized | Sector keycard, override module |

**Acquisition:** item rooms (1-of-3), vendors (spend credits), boss/mid-boss drops, secret rooms, narrative rewards.

**Synergies — design constraint, not feature:** every passive must be designed *with at least one other existing passive in mind*. Lone-wolf passives that don't compose are rejected at workshop time. Synergy categories use a tagging system (e.g., `fire`, `shield`, `projectile`, `movement`) so the design surface is searchable and synergy chains are intentional.

**Item pool size targets** (full design in W5):
- Slice (M9): ~10 passives, 3 tools, 5 consumables, all designed for interaction
- Post-slice (full game): 50+ passives, 10+ tools, 10+ consumables

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

Puzzles exist at the room level. They gate the room's exit or a chest.

**Primitives** (full set defined in W7):
- Switch / pressure plate
- Power-node routing
- Enemy-clear gate
- Timed sequence
- Terminal code entry (code found elsewhere in dungeon)
- Environmental physics (push to plate, redirect beam)

Puzzle state lives in the room's Godot scene. Completion emits a signal the room listens for.

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

- **Run save** — autosaved after each room clear; single slot; deleted on death
- **Meta save** — JSON file; never deleted; flags, unlocks, shortcuts, mirror upgrades, meta currency

JSON format for both. Save serialization is introduced in M3, not M8 — all Core types are designed with serialization as a day-one constraint.

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
