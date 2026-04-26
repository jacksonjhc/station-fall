# StationFall — Design Workshops

Working document for brainstorming sessions. PLANNING.md is the *canonical* design — once a workshop reaches a decision, the decision is promoted into PLANNING.md and the workshop's status here is updated.

This file holds the messy middle: pre-reads, prompt questions, brainstormed options, narrowing notes, and the eventual decisions.

See [PLANNING.md](PLANNING.md) for the established design and [ROADMAP.md](ROADMAP.md) for milestone gates.

---

## How a Workshop Works

1. **Schedule** — pick a workshop from the list below. Each gates one or more milestones.
2. **Pre-read** — re-read the linked PLANNING.md sections.
3. **Brainstorm** — generate options widely, then narrow.
4. **Decide** — for each output the workshop owes, pick a concrete answer.
5. **Promote** — update PLANNING.md to reflect the decisions. Mark status here as "decisions promoted".
6. **Unblock** — milestones gated by this workshop can now ship.

A workshop need not finish in one session. "Partial completion" is fine — partial decisions still unblock partial milestone work (e.g., W3 only needs Sector 1 enemies decided to unblock M3).

---

## Workshop Status Summary

| # | Topic | Status | Gates |
|---|-------|--------|-------|
| W1 | Vessels & Signature Abilities | Decisions promoted | M1 |
| W2 | Combat Feel & Weapon Patterns | Decisions promoted | M1, M3.5 |
| W3 | Enemy Roster & Archetype Detail | Decisions promoted | M3, M9 |
| W4 | Bosses & Mid-Bosses | Not started | M8, M9 |
| W5 | Items: Passives, Tools, Consumables | Not started | M6, M7, M9 |
| W6 | Synergies | Not started | M7 |
| W7 | Dungeon Elements & Mechanics | Decisions promoted | M2, M5 |
| W8 | Sector Themes (Sector 1 deep dive) | Not started | M9 |
| W9 | Difficulty Tier Mechanics | Not started | M8 |
| W10 | Narrative Architecture (incl. meta-currency name) | Not started | M9 |

**Suggested run order** (to keep workshops aligned with milestone needs):
W1 → W2 → W7 → W3 → W5 (tools) → W6 → W4 → W9 → W8 → W10 (with W5 passives interleaved between W6 and W7).

---

## W1 — Vessels & Signature Abilities

**Status:** Decisions promoted
**Gates:** M1
**Pre-read:** [PLANNING.md § Player / Vessel](PLANNING.md#player--vessel)

**Output owed (all delivered — see PLANNING.md § Player / Vessel for full spec):**
- Final vessel roster — 6 vessels with names, identity sentences, engagement contracts
- Per-vessel concrete starting stats — HP, move speed, attack power, attack rate, reach, dodge profile, luck, armor
- Per-vessel signature with full rules — cooldown, duration, magnitude, edge cases
- Starting passives (each pool-portable as a rare item)
- Vessel-unique mechanic — Aberrant's Echo Conversion (the only engine-unique mechanic in the roster)
- Starting vessel — Clone
- Unlock gating — Clone start; Android/Synthetic/Exo/Operator via meta currency; Aberrant via narrative flag

**Decisions (summary — full spec lives in PLANNING.md):**

1. **Design framing — Engagement Contract.** Each vessel is built around the question *"what does this body ask me to do differently from room to room?"* and must pass the 5-test gate (selection sentence / room behavior / boss behavior / panic behavior / signature dependency).

2. **Isaac-style portability.** Vessel identity decomposes into stat distribution (always portable), starting passives (real pool items, rarely findable by other vessels), and vessel-unique mechanics (engine-level, used sparingly). Roster has exactly one unique mechanic — Aberrant's Echo Conversion.

3. **HP scale recalibrated.** Baseline 5 HP, not 100. Sector 1 enemies do 1–2 damage; HP is the most common per-run upgrade. Late-run totals into the 50+ range are normal. Alundra-style icons: small icon = 1 HP, big icon = 10 HP, no fractions, integer damage. Per-vessel symbology (blood drops / battery cells / armor plates / signal bars / integrity shards / eye glyphs).

4. **Dodge as data profile.** `DodgeActionDefinition` (movement / duration / i-frames / contact damage / hitbox) is data-driven. Default is "Roll"; Exo's default is "Shoulder-Charge"; profiles are pool-portable as rare passives.

5. **Stat sheet locked.** HP, move speed, attack power, attack rate, reach, dodge recharge / distance / i-frames, luck, armor — all modifiable by pickups.

6. **Roster (6 vessels):** Clone (fair default), Android (precision), Synthetic (mover), Exo (tank, no roll), Operator (tools), Aberrant (risk-as-power).

7. **Numbers are playtest-tunable.** All values ship configurable (`.tres` or inspector-exposed), not hardcoded. Real balance happens once the game is playable.

**Items implicitly created by W1 (need IDs in `Assets/Data/Items/` and `Assets/Data/Tools/` when the content pipeline lands):**

- `Adrenaline Rush` (passive trigger; Clone start)
- `Overclock` (active; Android start)
- `Phase Shift` (active; Synthetic start)
- `Plated` (passive; Exo start; +armor stat, swaps dodge → Shoulder-Charge)
- `Shoulder-Charge` (dodge profile; Exo default; pool-rare for others)
- `Schoolbag` (passive; Operator start; second tool slot)
- `Console Hack` (passive; Operator start; bypass some locks, extra terminal lore)
- 1–2 rare max-HP recovery items (Aberrant counter; names TBD → W10)

**Handoffs to other workshops:**

- **W9 (Difficulty Tier Mechanics):** owns the easy-mode → hard-mode unlock rule (Isaac-style: unlocks after first few successful runs), per-tier multipliers, scarcity changes (e.g., scarcer health drops in hard mode).
- **W10 (Narrative Architecture):** owns the narrative flag that gates Aberrant's unlock; owns the names of the 1–2 rare max-HP recovery items in the pool.

---

## W2 — Combat Feel & Weapon Patterns

**Status:** Decisions promoted
**Gates:** M1 (basic feel), M3.5 (full feel pass)
**Pre-read:** [PLANNING.md § Combat](PLANNING.md#combat--real-time-action), [PLANNING.md § Game Feel](PLANNING.md#game-feel)

**Output owed (all delivered — see PLANNING.md § Combat and § Game Feel for full spec):**
- Melee weapon roster with attack timing tables (windup/active/recovery), reach, arc, damage, hit-stop
- Ranged weapon decision: tools-as-ranged (slingshot, stun coil) — laser pistol deferred post-slice
- Hit-stop magnitude per weapon (ms, target / attacker; attacker = half target)
- Screen shake trauma values per event
- Dodge profile concretes (i-frames, recovery, distance, post-dodge vulnerability window) for default Roll and Shoulder-Charge
- Default attack chain: 3-hit (light/light/heavy) with per-weapon variation; cancel rules
- Per-weapon "feel adjective" — clean / heavy / fast / precise / brutal / deft

**Decisions (summary — full spec lives in PLANNING.md):**

1. **3-hit combo as default.** Each weapon defines its own combo; pickups can extend (cap +2). Combo step has a 6-frame cancel window at start of recovery; outside that window, combo resets.

2. **Cancel rules.** Dodge cancels any frame ≥ end-of-windup. Attacks cannot cancel into other attacks — only the chain advances.

3. **Hit-stop applies to both attacker and target.** Target freeze is 2× attacker freeze. All values configurable per attack in `.tres`.

4. **Vessels always carry a weapon.** No bare-hands state. Each vessel ships with a default; found weapons replace.

5. **Weapon roster (slice — 5 vessel defaults + 1 found):** Sword (Clone, clean), Hammer (Exo, heavy), Dual-blade (Synthetic, fast), Rapier (Android, precise), Claws (Aberrant, brutal), Daggers (found, deft). Frame data, damage, hit-stop, and per-weapon special rules locked in PLANNING.md.

6. **Ranged is tool-shaped, not gun-shaped, in the slice.** Slingshot (ammo-fed, Zelda-style) and Stun Coil (charge-up, short-range stun arc) are in. Laser Pistol deferred post-slice.

7. **Dodge profile numbers locked.** Default Roll: 12 frames, 8 i-frames (frames 2–9), 3 recovery, ~96px, 0.6s recharge. Shoulder-Charge: 16 frames, 10 i-frames, 4 recovery, ~128px, contact damage, 1.0s recharge. **Post-dodge vulnerability rule:** the last 3 recovery frames have no i-frames — this is the deliberate punish window.

8. **Trauma values locked** for light/heavy hit landed, damage taken (light/heavy), dodge, perfect block, death. See PLANNING.md table.

9. **Numbers ship configurable.** Frame counts, hit-stop ms, trauma values, dodge timings — all `.tres` or inspector-exposed. Real balance happens at playtest.

**Items implicitly created by W2 (need IDs in `Assets/Data/Items/` and `Assets/Data/Tools/` when the content pipeline lands):**

- `Refrain` (passive; +1 combo step, stacks once, cap +2)
- `Pirouette` (passive; final combo hit becomes 360° hitbox)
- `Active Shield` (active tool; hold to raise, 75% frontal reduction, 3 charges, perfect-block window first 8 frames)
- `Slingshot` (active tool; ammo-fed ranged)
- `Stun Coil` (active tool; charge-up short-range stun arc)
- `Poison Coat` (passive; DoT on damaging hit, 1 dmg/sec for 4s, refreshes, magnitude doesn't stack)

**Handoffs to other workshops:**

- **W3 (Enemy Roster):** weapon frame data drives enemy windup-readability targets — enemy telegraphs need to be at least as long as the player's longest dodge i-frame window so dodging stays viable.
- **W5 (Items: Passives, Tools, Consumables):** owns full pool integration of `Refrain`, `Pirouette`, `Active Shield`, `Slingshot`, `Stun Coil`, `Poison Coat` — rarity tiers, drop weights, vendor stock rules, synergy tags.
- **W6 (Synergies):** Poison Coat + Aberrant's Bleed = double-DoT build identity; combo extenders (`Refrain`, `Pirouette`) multiply DoT trigger frequency. Worth designing around.

---

## W3 — Enemy Roster & Archetype Detail

**Status:** Decisions promoted
**Gates:** M3 (first enemy), M9 (full Sector 1 roster)
**Pre-read:** [PLANNING.md § Combat (enemy archetypes)](PLANNING.md#combat--real-time-action), [PLANNING.md § Enemy Roster](PLANNING.md#enemy-roster)

**Output owed (all delivered — see PLANNING.md § Enemy Roster for full spec):**
- Sector 1 full roster: 6 enemies (scope-up from W3's stated 4–5 — Corrupted Medbot is the most expensive; flagged as deferrable if M9 schedule slips)
- Per enemy: name, archetype, HP, attack power, move speed, attack patterns with frame data, behavior tells, counter-strategy, drops, resistances, `PerceptionType`, aggro/LOS rules, dark-room behavior
- Elite rule + 4 specific Sector 1 elite variants (one pattern mutation each)
- Durable principles (telegraph floor, contact-damage default, hazards-damage-enemies, Sector 1 resistance ceiling, elite rule)
- `PerceptionType` enum (`Vision` / `Sound` / `Heat` / `Omniscient` / `Ambient`)
- Sectors 2–5 sketch roster (working names + intents only; full design deferred)
- Recommended Sector 1 encounter order for tier-0 generator weighting

**Decisions (summary — full spec lives in PLANNING.md):**

1. **Six durable principles.** Telegraph floor ≥ 14 frames at 60fps (never erodes for elites); contact damage off by default; hazards damage enemies; Sector 1 resistance ceiling = 1 (use Immunity directly when meant); elite rule = modest stat scaling + exactly one readable pattern mutation; numbers ship playtest-tunable.

2. **`PerceptionType` enum.** `Vision` / `Sound` / `Heat` / `Omniscient` / `Ambient`. Sector 1 leans `Vision` so darkness matters; `Sound` on Suture Mites makes dark rooms genuinely dangerous without overusing the tag.

3. **Sector 1 roster (6 enemies, 5 archetypes).** Twitching Patient (melee rusher, starter) · Drip Drone (ranged shooter) · Convulsing Body (ambient hazard creature) · Suture Mite (swarm) · Bio-Seal Orderly (shield bearer) · Corrupted Medbot (status applier — scope-up, deferrable).

4. **Starter enemy = Twitching Patient.** Teaches the entire combat loop in 30 seconds: notice → telegraph → dodge → punish recovery. 14-frame windup, 22-frame recovery, 1 dmg slash, no contact damage, no resistances.

5. **Bio-Seal Orderly Brace as resistance model, not binary block.** Frontal slash/pierce −1 (resistance); frontal bash full; **heavy combo finisher** (any type) ignores resistance; rear/side full; hazards ignore facing; **electric disables brace 1.5s**. Synthetic and Android (no Heavy in their default weapon's combo) are forced to flank, electric, or hazard — strong vessel-distinct interaction without bespoke per-vessel logic.

6. **Suture Mite windup raised to 16 frames** (from initial 12) to satisfy the telegraph floor in swarm context — overlapping 12-frame windups across 3–5 mites made dodging unreliable. Pierce resistance dropped (was 1) so Android's Rapier and Daggers aren't punished on first swarm fight.

7. **Elite rule.** Modest stat scaling + one pattern mutation (never faster telegraphs). Sector 1 elites: Redline Patient (second stumble-lunge after miss) · Dose Drone (two-shot burst with retarget) · Containment Orderly (3-hit shockwave; electric still disables) · Overdose Medbot (Double Dose dart sequence + extended bleed). Suture Mites and Convulsing Bodies don't get elites — swarms scale via pack size, ambient hazards via density and pulse timing.

8. **Aberrant Echo Surge interaction with Containment Orderly (HP 6) noted as deliberate.** A 6+ Echo Surge does 5 damage to elites, leaving Containment at 1 HP. Kept as-is — "elites survive your panic button" reads as design, not bug. Revisit after playtest.

9. **Recommended Sector 1 encounter order for tier-0 generator weighting.** Tone-setting room → solo Twitching Patient → staggered pair → Drip Drone with cover → Convulsing Body + Medbot (hazard + debuff) → Mite pack OR Orderly (graduation fight).

10. **Sectors 2–5 sketch.** 5-enemy outlines per sector reserve design space and signal damage-type/mechanic introductions (Engineering = fire/electric, Research = psychic/anomaly, Command = security/control, Core = reality-distortion). Working names only — nothing locked.

**Items / systems implicitly created by W3** (need IDs / types when their content pipelines land):

- Enemy `.tres` definition schema covering: archetype, stats, perception, frame data, drops, resistances, elite link
- `PerceptionType` enum (system-level)
- Slow debuff (Sedative Dart) and Bleed debuff (Emergency Sutures) — first concrete uses of the status-effect system
- Anticoagulant consumable (cleanse Bleed) — handed to W5
- Slingshot ammo as a drop type — handed to W5

**Handoffs to other workshops:**

- **W4 (Bosses & Mid-Bosses):** Sector 1 mid-boss and boss must respect the durable principles (telegraph floor, no faster-than-base elites). The mid-boss likely escalates one Sector 1 archetype; the sector boss should introduce an attack the roster doesn't telegraph yet.
- **W5 (Items, Tools, Consumables):** Owns full pool integration of anticoagulant consumable, slingshot ammo as drop, and any future enemy-targeted items (e.g., shield-piercer passive). Drop table tuning happens here once item rarity tiers are set.
- **W6 (Synergies):** Bleed + Poison stack from different sources is the first cross-source DOT interaction in the slice — flag it for synergy design (Aberrant's Bleed via Claws + Medbot's Bleed should not magnitude-stack since same effect refreshes per W7's DOT rule).
- **W8 (Sector Themes):** Sector 1 deep-dive uses this roster as fixed; lighting and audio palettes are designed *around* enemy `PerceptionType` (most-`Vision` makes darkness scary; Mites' `Sound` makes silence valuable).
- **W9 (Difficulty Tier Mechanics):** Higher tiers can enable elite spawns, raise elite damage from +0 to +1, and shift the encounter weighting away from solo-rusher rooms toward swarm/orderly graduation fights earlier in the run.

---

## W4 — Bosses & Mid-Bosses

**Status:** Not started
**Gates:** M8 (mini-boss), M9 (sector boss)
**Pre-read:** [PLANNING.md § Combat](PLANNING.md#combat--real-time-action), [PLANNING.md § Station Structure](PLANNING.md#station-structure)

**Output owed:**
- 5 sector bosses (one per sector) with full phase rules, attack pattern rotation, arena requirements, narrative weight
- 5 mid-bosses (one per sector) — smaller, more frequent
- "Easy ending" boss design (the equivalent of Isaac's Mom — gates Tier 2 difficulty)
- "True ending" boss design (deepest, gated by narrative flags)
- 2–3 hidden / variant boss possibilities
- For Sector 1 specifically: mid-boss + sector boss, fully designed
- Reward design per boss: guaranteed drop type, meta-currency amount

**Prompt questions:**
- What does the Sector 1 boss communicate about the station's wrongness? It's the player's first real glimpse of the horror.
- Should bosses share design language (all "corrupted" entities) or each be totally distinct?
- Should any boss require a specific tool to defeat or just heavily reward it?
- How many phases is "right"? Two for mid-bosses, three for sector bosses?
- Is there a boss that's primarily a puzzle, not a DPS check?

**Decisions:** *(filled in during workshop)*

---

## W5 — Items: Passives, Tools, Consumables

**Status:** Not started
**Gates:** M6 (Magnetic Grapple — Tools section), M7 (passives chain), M9 (Sector 1 pool)
**Pre-read:** [PLANNING.md § Items, Tools, Modules](PLANNING.md#items-tools-modules)

**Output owed:**
- Tagging system for synergy categories (proposed: `fire`, `electric`, `shield`, `projectile`, `melee`, `movement`, `defensive`, `aoe`, `dot`, `crit`)
- Tool roster: ~10 tools with rules (Magnetic Grapple is mandatory; others?)
- For M6 specifically: Magnetic Grapple full spec — range, cooldown, valid targets, behavior on hit, edge cases (grappling an elite? grappling during own dodge?)
- Passive roster: target ~50 for full game, ~10 for slice; each tagged for synergy
- For M7 specifically: 2–3 passives with explicit pairwise synergies (a chain that visibly stacks)
- Consumable roster: ~10 (medkit, flashbang, breaching charge, etc.) with rules
- Pool tiering: common / uncommon / rare / cursed
- Rarity → drop weight mapping

**Prompt questions:**
- For the M7 chain: which tags should the first three passives share? (The chain "hit applies status → status enables damage bonus → kill grants defensive perk" is a strong starting template — any other shapes?)
- Should there be *cursed* items — strong but with drawbacks? Isaac uses these heavily.
- Should consumables be limited (e.g., max 3 of any consumable held)?
- Is there a "tool ammo" concept or strictly cooldowns?
- For Magnetic Grapple specifically — what feels best for "mass too heavy to grapple"? Hard limit, or "you get pulled instead"?

**Decisions:** *(filled in during workshop)*

---

## W6 — Synergies

**Status:** Not started
**Gates:** M7
**Pre-read:** [PLANNING.md § Items, Tools, Modules](PLANNING.md#items-tools-modules), W5 output

**Output owed:**
- Catalog of intended synergy chains across the passive pool (depends on W5)
- Math rules: do bonuses stack additively or multiplicatively? Per-source caps?
- Anti-synergy avoidance — pairs that *cancel* unintentionally
- Build identity archetypes — recognizable builds the player can intentionally pursue (e.g., "burn DOT", "shield tank", "kiting glass cannon", "summoner")
- Per-build, the 4–6 items that exemplify it
- Unintentional synergy detection: which pairings are emergent rather than designed?

**Prompt questions:**
- Should synergy chains be telegraphed in-game (item description hints at tags) or pure discovery?
- Are there "synergy reveal" items — shows the player what their current items combine to?
- How many distinct build archetypes do we want by full release? 6? 12?

**Decisions:** *(filled in during workshop)*

---

## W7 — Dungeon Elements & Mechanics

**Status:** Decisions promoted
**Gates:** M2 (basic doors + props), M5 (full primitive set for procgen)
**Pre-read:** [PLANNING.md § Room System](PLANNING.md#room-system), [PLANNING.md § Hazards](PLANNING.md#hazards), [PLANNING.md § Damage System](PLANNING.md#damage-system), [PLANNING.md § Puzzle System](PLANNING.md#puzzle-system), [PLANNING.md § Save System](PLANNING.md#save-system), [PLANNING.md § Lights & Power](PLANNING.md#lights--power)

**Decisions (summary — full spec lives in PLANNING.md):**

1. **Door taxonomy.** Three categories — Basic (Open / Closed visual states), Locked (generic-keys consumed; unique-keys HUD-pinned, sector-scoped, color-coded; rare Skeleton Key permanent), Barred (kill-clear locks behind player; switch / lever / hack / one-way variants). Secret doors as a fourth class with four reveal methods (cracked-wall destructible / no-indicator super-secret / Scanner / triggered terminal). Sticky-open reset rule; contraption-tied doors can be intentionally re-closed.

2. **Room shape — Isaac × LttP hybrid.** One room visible at a time. Some rooms screen-sized (camera locked); others larger with camera follow inside hard-clamped room bounds. Max 2×2 default, 3×N for boss arenas. Multi-doors per wall allowed; no diagonals. Hub rooms allowed. Sectors get distinctive room-design languages with their own template pools. Room size is a template property; content is hand-tuned, not auto-scaled.

3. **Hazards.** Catalog of ~10 hazard types (spike, gas vent, electric floor, fire jet, vacuum vent, falling debris, fragile floor, three laser sub-types: toggling / patrolling / tracking). Damage model is flat-by-default with occasional DOT (never percentage). DOT stacking: same effect refreshes (latest source wins duration); different effects stack. **Always-telegraph rule** — no gotcha hazards. Hazards damage enemies too; tactical use via knockback/grapple is encouraged. Mechanical hazards disarmable via multiple methods (hack, shoot control box, tool); always-on environmental hazards rely on protection items (Gas Mask, Reinforced Soles, Hover Jets). Once disarmed, permanent for the run.

4. **Damage system.** 10 damage types: physical (slash, bash, pierce), elemental (fire, electric, poison, cold/cryo), exotic (vacuum, sonic, psychic/echo). **Slice uses only physical + fire + electric + poison.** Defense order: Crit → Resistance (per-type flat subtraction; resist-piercing gear exists) → Immunity (hard 0) → **Armor (physical-only)** flat damage pool that absorbs slash+bash+pierce but is ignored by elementals. Crits bypass Resistance, not Armor.

5. **Critical hits.** Per-weapon base rate and per-weapon multiplier. Starting table locked in PLANNING (Sword 5%/2.0×, Hammer 3%/3.0×, Dual-blade 8%/1.5×, Rapier 15%/2.5×, Claws 6%/2.5×, Daggers 10%/2.0× → 3.0× from rear). Luck +1 = +1% crit (cap +25% from Luck alone; passives can push higher). Crit feedback: bigger number, distinct color, brief screen flash, +50% hit-stop.

6. **Lights & Power.** Default ambient progresses by sector — early sectors mostly-lit with dark-room exceptions, later sectors invert to mostly-dim with powered exceptions. No-light fallback is *low* visibility (never fully black) with minimal guidance lights and enemy-carried light sources. Light items: Flashlight (battery, cone) + Lantern (fuel, radial) + ignitable wall torches (per-torch persistence rule). Power scope is both per-room and per-sector. Enemy perception in the dark is a per-archetype tag (vision / sound / heat / omniscient) — handed to W3 for full spec.

7. **Saves — three layers, no save-scumming.** Meta save auto on run completion (persistent). Hard run save auto on inter-sector safe zones (per-run). Quick save manual via Escape → Save and Quit, single slot, consumed on Continue, wiped if New Run is started, never seen by death.

8. **Puzzles.** Slice primitives: switch / pressure plate (one-shot or hold-release), pressure-plate combinations, enemy-clear gate, terminal code entry. Post-slice primitives (power routing, light redirect, item-carry, timed sequence) deferred. Sector-spanning puzzles guaranteed at least once per run, per-run only (no cross-run carry). HUD-accessible **discovered codes log** so the player never has to memorize. **No-soft-lock invariant** added to the procgen rules.

9. **Roomwide mechanics.** Slow zones in (time-dilated, slows everything). Lights-out in. **Gravity flips deferred post-slice.**

10. **Secret rooms.** Guaranteed minimum count per sector. Mostly indicated (cracked walls); 1–2 super-secrets per sector with no indicator. Scanner persistent while held + consumable scan-card variants.

11. **Numbers are playtest-tunable.** All hazard damage values, crit rates, light-source durations, slow-zone strengths ship as configurable data — real balance happens at playtest.

**Items/systems implicitly created by W7** (need IDs / types when their content pipelines land):

- `Skeleton Key` (passive, rare; run-permanent generic-lock bypass)
- `Gas Mask`, `Reinforced Soles`, `Hover Jets` (passive *protection items* — pair with the resistance system)
- `Flashlight`, `Lantern` (active light tools)
- `Scanner` (active tool — persistent reveal) + scan-card consumables (one-shot reveal)
- `Cutter` / `Hack tool` (active tools — disarm methods)
- Damage-type enum with 10 entries (system-level)
- Per-archetype `PerceptionType` enum on enemies (handed to W3)

**Handoffs to other workshops:**

- **W3 (Enemy Roster):** Each enemy gets a `PerceptionType` tag (vision / sound / heat / omniscient) and per-type Resistance/Immunity values. Some enemies need light to detect the player; some carry their own light.
- **W5 (Items, Tools, Consumables):** Owns full pool integration of `Skeleton Key`, `Gas Mask`, `Reinforced Soles`, `Hover Jets`, `Flashlight`, `Lantern`, `Scanner`, `Cutter`, `Hack tool`, scan-card consumables, key-drop-rate passives, crit-rate / crit-multiplier passives, resistance-piercing gear.
- **W6 (Synergies):** Damage-type interactions are now load-bearing for synergy design (fire + chill / poison + bleed / electric stun chains). Build identities should incorporate damage typing.
- **W8 (Sector Themes):** Each sector picks its hazard set, lighting palette (lit-default vs dim-default), and which template-pool size mix it leans on (mostly screen-sized vs mostly large rooms).
- **W9 (Difficulty Tier Mechanics):** Higher tiers can shift the lit/dim balance, increase hazard density, restrict disarm methods, or buff enemy resistance values.

---

## W8 — Sector Themes (Sector 1 deep dive)

**Status:** Not started
**Gates:** M9
**Pre-read:** [PLANNING.md § Station Structure](PLANNING.md#station-structure), [PLANNING.md § Atmosphere & Horror Direction](PLANNING.md#atmosphere--horror-direction)

**Output owed:**
- Sector 1 (Medical Wing) deep dive:
  - Lighting palette (color references, contrast intent, dark zone density)
  - Audio palette (ambient bed, room cues, silence rules)
  - Room template categories (operating room, ward, lobby, morgue, etc.) and how many of each in the pool
  - Enemy assignment from W3 roster
  - Hazard set from W7 catalog (which apply here)
  - Signature mechanic implementation (contamination locks, bio-seals — how do they read in play?)
  - Narrative beats placement plan (which logs/echoes go where)
  - First-room tone-setting plan: 30 seconds of atmosphere before first combat
- Sector 2–5 outlines (lighter; full deep-dive deferred until that sector ships)

**Prompt questions:**
- What's the "signature image" of Medical Wing — the one screenshot that sells the sector?
- Is there a recurring visual motif (specific monitor color, specific corpse pose, specific sound)?
- Should each sector have a piece of unique music or just ambient?
- Which Sector 1 room is the first the player ever sees — what does it establish?

**Decisions:** *(filled in during workshop)*

---

## W9 — Difficulty Tier Mechanics

**Status:** Not started
**Gates:** M8
**Pre-read:** [PLANNING.md § Difficulty Escalation](PLANNING.md#difficulty-escalation)

**Output owed:**
- Per-tier mechanical changes — concrete numbers:
  - Room count multiplier
  - Enemy density per room (multiplier or replacement table)
  - Elite spawn rate
  - Enemy variant pool (which enemies become available at which tier)
  - Boss pool (which bosses can spawn)
  - Environmental hazard frequency
  - Vendor stock quality
  - Loot quality multiplier
- Tier-up trigger formulas:
  - Tier 0 → 1: first sector-1 boss kill (this seems decided)
  - Tier 1 → 2: easy-ending boss kill OR N total boss kills (pick — or both?)
  - Tier 2 → 3: specific narrative-flag accumulation (which flags?)
- Tier-down rules: do tiers ever decrease? (Likely no, but state it.)
- UI: does the player see their current tier? In a menu, on the run-summary screen?

**Prompt questions:**
- How obvious should tier-up be? A clear "the station has changed" beat, or a quiet difficulty creep?
- Should Tier 2+ unlock entirely *new* room types in earlier sectors, or just escalate existing content?
- Should tier affect meta-currency drop rates (incentivize playing at higher tiers)?

**Decisions:** *(filled in during workshop)*

---

## W10 — Narrative Architecture

**Status:** Not started
**Gates:** M9
**Pre-read:** [PLANNING.md § Narrative & Mystery](PLANNING.md#narrative--mystery)

**Output owed:**
- Master story document: what actually happened on the station? Who did what to whom?
- Cast of crew: 5–10 named station personnel; their roles, voices (in writing), and fates
- Echo fragment timeline — the player's past lives, in the order they're meant to be discovered
- Per-sector narrative beats: 5–10 discoveries the player can make in each sector
- ~30 narrative fragments distributed across the full game; ~5 for Sector 1
- True ending arc: what the player learns, what they choose, what closure looks like
- Light scripted moment list: the in-engine non-cutscene beats (vat opening, comms voice, etc.) and where they fire
- **Meta-currency name** — pick from Echoes / Imprints / Memory Shards or coin a new one

**Prompt questions:**
- Is the player a victim, a perpetrator, or both?
- Does the station have a singular antagonist, or is the horror systemic?
- Should there be a moral choice in the true ending, or a discovered truth?
- How tonal is the writing — clinical / intimate / unhinged / detached?
- Are there NPCs the player can interact with, or only environmental traces of crew?

**Decisions:** *(filled in during workshop)*

---

## After All Workshops

Once W1–W10 are at least to "decisions promoted" stage, the design surface for the vertical slice is closed. New design work after that point is iteration on what shipped, not new invention.

Post-slice (Sectors 2–5, post-1.0 content) will likely require fresh workshops — they belong to a separate planning phase, not this one.
