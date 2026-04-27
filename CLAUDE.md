# CLAUDE.md

Guidance for future Claude Code sessions working in this repo.

## Project

**StationFall** — sci-fi horror roguelite. Godot 4.6 + C# / .NET 8.

**Real-time top-down action**, Zelda-style dungeon exploration (keys, locked doors, puzzles, bosses), Binding of Isaac-style item discovery and difficulty escalation, procedurally generated space station, narrative through discovery across runs.

> ⚠️ **This is real-time action, NOT turn-based.** No menus during combat, no party, no JRPG-style turns. Player input drives a `CharacterBody2D` in real time; hits land via `Area2D` overlaps. If a design suggestion would require pausing combat for menu input, it's wrong for this project.

See [PLANNING.md](PLANNING.md) for the full design document and [ROADMAP.md](ROADMAP.md) for milestones.

**Engine config:** Forward Plus rendering, Direct3D 12 (Windows). 2D top-down, 1280×720, `canvas_items` stretch mode. Forward Plus is kept for 2D normal-map lighting and dynamic shadows.

## Solution Structure

```
Stationfall.sln
├── Stationfall.Godot.csproj      # Root Godot project; references Core
├── Scripts/                      # Godot-facing C# nodes (partial classes, signals)
├── Scenes/                       # .tscn files
├── Assets/                       # Audio, Data (.tres), Fonts, Sprites, Kenney/ packs
├── src/Stationfall.Core/         # Pure C# — zero Godot dependencies
│   ├── Entities/                 # EntityStats, PlayerVessel, EnemyDefinition
│   ├── Combat/                   # DamageCalculator, StatusEffect, DamageResult
│   ├── Ai/                       # EnemyAiBrain, AiState, SensorData
│   ├── ProcGen/                  # DungeonGenerator, DungeonLayout, RoomTemplate
│   ├── Items/                    # ItemDefinition, ItemEffect
│   ├── Tools/                    # ToolDefinition, tool application
│   ├── Currency/                 # Credit + meta currency rules
│   ├── Progression/              # MetaState, mirror upgrades, difficulty tier
│   ├── Narrative/                # NarrativeFlag registry
│   ├── Runs/                     # RunState (per-run progress)
│   ├── SaveData/                 # JSON save/load
│   └── Rng/                      # Seeded RngService
└── src/Stationfall.Tests/        # xUnit tests against Core only
```

## Commands

```bash
# Build Core only (no Godot SDK required)
dotnet build src/Stationfall.Core/

# Run all tests
dotnet test src/Stationfall.Tests/

# Run tests matching a filter
dotnet test src/Stationfall.Tests/ --filter "FullyQualifiedName~DungeonGeneratorTests"

# Run the game — open project.godot in Godot 4.6, then F5
# Run current scene only — F6
```

> `Stationfall.Godot.csproj` uses `Godot.NET.Sdk/4.6.0`. Update if it diverges from your installed Godot.

## Asset Bootstrap

`Assets/Kenney/` is gitignored — the Kenney CC0 placeholder packs are re-procurable from [kenney.nl](https://kenney.nl/) and not redistributed in this repo. To run the game on a fresh clone:

Download these CC0 packs from kenney.nl and extract under `Assets/Kenney/` so the paths shown below resolve:

| Pack | URL | Used for |
|------|-----|----------|
| **kenney_top-down-shooter** | https://kenney.nl/assets/top-down-shooter | Character + tile sprites |
| **kenney_particle-pack** | https://kenney.nl/assets/particle-pack | Hit-burst and death-cloud particle textures |
| **kenney_impact-sounds** | https://kenney.nl/assets/impact-sounds | Hit-landed and damage-taken SFX |
| **kenney_sci-fi-sounds** | https://kenney.nl/assets/sci-fi-sounds | Dodge whoosh, player death |

Each pack zip extracts with its own top-level folder, so the resulting tree is `Assets/Kenney/<pack-name>/...`. Open `project.godot` in Godot 4.6 — the editor reimports textures and audio on first load.

Scenes and scripts reference these resources by Godot UID and `res://` path. Once the files exist at the expected locations the import resolves automatically. Without the packs, the game still launches but characters / tiles render as broken-texture placeholders and audio cues are silent.

> Other Kenney packs may sit untracked under `Assets/Kenney/` locally as a personal library; the table above is the authoritative list of what's actively referenced. When a new pack starts being used, add a row here.

## Architecture Rule

**Core decides. Godot displays.**

`Stationfall.Core` has **no** `using Godot;` — ever. It is testable without the engine.

`Scripts/` contains thin Godot `Node` subclasses that call into Core and emit signals for UI/VFX to consume. Godot owns spatial state (positions, velocities, animation), Core owns rules and data.

### Damage Pipeline

```
Player input (Godot PlayerController)
  → Hitbox overlaps enemy HurtboxComponent (Godot Area2D)
  → DamageCalculator.Calculate(attackerStats, defenderStats, modifiers) (Core)
  → EntityStats.ApplyDamage(result) (Core mutates stats)
  → DamageResult returned to Godot
  → Visual feedback: numbers, screen shake, hit stop, animation (Godot)
```

### Enemy AI Pipeline

```
Core EnemyAiBrain: states + transition rules as pure functions
  (sensorData) → nextState

Godot EnemyController, each _PhysicsProcess:
  1. Gather sensor data (distance to player, line-of-sight, HP ratio)
  2. brain.Tick(sensorData) → new state
  3. Execute: move CharacterBody2D, trigger animation, spawn hitbox
```

## What Lives Where

| Concern | Location |
|---------|----------|
| Damage formulas | `Core/Combat/DamageCalculator.cs` |
| Status effect tick logic | `Core/Combat/StatusEffect.cs` |
| Enemy AI state machine | `Core/Ai/EnemyAiBrain.cs` |
| Item effect definitions | `Core/Items/ItemEffect.cs` |
| Tool definitions | `Core/Tools/ToolDefinition.cs` |
| Dungeon room graph | `Core/ProcGen/DungeonGenerator.cs` |
| Per-run state | `Core/Runs/RunState.cs` |
| Meta-progression / unlocks / mirror upgrades / difficulty tier | `Core/Progression/MetaState.cs` |
| Currency rules | `Core/Currency/` |
| Narrative flag registry | `Core/Narrative/NarrativeFlag.cs` |
| Save/load | `Core/SaveData/` |
| Seeded RNG | `Core/Rng/RngService.cs` |
| Player input + movement | `Scripts/Player/PlayerController.cs` |
| Hitbox / hurtbox | `Scripts/Combat/HitboxComponent.cs` |
| Enemy execution | `Scripts/Enemies/EnemyController.cs` |
| Room state + door logic | `Scripts/Dungeon/RoomController.cs` |
| Room scene instantiation | `Scripts/Dungeon/DungeonInstantiator.cs` |
| Vendors / pickups / terminals | `Scripts/Items/`, `Scripts/Narrative/` |

## Godot 2D Specifics

- **Movement:** `CharacterBody2D` + `MoveAndSlide()` for player and enemies
- **Hitboxes/hurtboxes:** `Area2D` + `CollisionShape2D`; separate layers for player attack, enemy attack, player body, enemy body
- **Camera:** `Camera2D` with position smoothing; trauma-based shake for hits
- **HUD:** `CanvasLayer`, viewport-independent
- **Lighting:** `PointLight2D` for dynamic sources; normal maps on sprites for depth; `DirectionalLight2D` for ambient fill
- **Room geometry:** `StaticBody2D` + `CollisionPolygon2D` for walls; or `TileMapLayer` for tile-based layouts
- **Rendering layers:** keep collision layers documented — layer assignment matters for what hits what

## Conventions

- **Records for value objects** — `DamageResult`, item definitions, room layout nodes. Use `with` for non-destructive update.
- **Pure transition functions** — enemy AI transitions in Core take sensor data and return a state enum. No side effects, no Godot calls.
- **Godot signals flow upward only** — Core never references Godot signals or types.
- **`InternalsVisibleTo("Stationfall.Tests")`** is set in `Stationfall.Core.csproj`. Internal helpers are testable without making them public.
- **Seeded RNG** — never use `System.Random` directly inside generation logic; route through `RngService` so runs are reproducible from a seed.
- **New item effect:** define in `Core/Items/`, write a test, then wire visual feedback in a Godot pickup script.
- **New enemy type:** define `EnemyAiBrain` transitions in Core, test them, then build the Godot `EnemyController` scene.
- **New tool:** define in `Core/Tools/`, write a test for its rules, then wire input + visual in a Godot tool node.
- **Placeholder art** — `Assets/Kenney/kenney_top-down-shooter/` is the chosen placeholder pack. Characters use `Sprite2D` (scale ~1.4 to fit the 44×44 collision body); floors use `TextureRect` with `stretch_mode = 1` (tile). Project's default texture filter is Nearest.
- **Tint / flash via `Modulate`, not `Color`** — visual feedback (hit flash, windup telegraph) must set `CanvasItem.Modulate` so it works on any visual node. Never branch on `ColorRect` — that locks out sprite-based visuals.

## Testing Expectations

- Anything in `Stationfall.Core` that involves a formula, transition, generation step, or unlock rule **must have a unit test**.
- Tests live in `src/Stationfall.Tests/`, mirroring Core folder layout.
- Generation tests should assert invariants (every layout has a solvable critical path) across many seeds, not just one.
- Damage / status / progression tests use plain xUnit `[Fact]` / `[Theory]`.
- Godot-side scripts are not unit-tested; verify them by playing the scene.

## Skill Reminders

- Treat features as real-time first. If a request implies a turn or menu mid-combat, push back.
- Prefer extending existing Core types over inventing parallel ones.
- Don't create new top-level files (PLANNING/ROADMAP/etc.) without asking — if it's design, it goes in PLANNING.md; if it's a milestone, it goes in ROADMAP.md.
