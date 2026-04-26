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
| W1 | Vessels & Signature Abilities | Not started | M1 |
| W2 | Combat Feel & Weapon Patterns | Not started | M1, M3.5 |
| W3 | Enemy Roster & Archetype Detail | Not started | M3, M9 |
| W4 | Bosses & Mid-Bosses | Not started | M8, M9 |
| W5 | Items: Passives, Tools, Consumables | Not started | M6, M7, M9 |
| W6 | Synergies | Not started | M7 |
| W7 | Dungeon Elements & Mechanics | Not started | M2, M5 |
| W8 | Sector Themes (Sector 1 deep dive) | Not started | M9 |
| W9 | Difficulty Tier Mechanics | Not started | M8 |
| W10 | Narrative Architecture (incl. meta-currency name) | Not started | M9 |

**Suggested run order** (to keep workshops aligned with milestone needs):
W1 → W2 → W7 → W3 → W5 (tools) → W6 → W4 → W9 → W8 → W10 (with W5 passives interleaved between W6 and W7).

---

## W1 — Vessels & Signature Abilities

**Status:** Not started
**Gates:** M1
**Pre-read:** [PLANNING.md § Player / Vessel](PLANNING.md#player--vessel)

**Output owed:**
- Final vessel roster (~5–6 vessels) with names, descriptions, identity hooks
- Per-vessel concrete stats: max HP, move speed (px/sec), attack power scalar, defense scalar
- Per-vessel signature ability with full rules: cooldown (sec), duration (sec or instant), magnitude, edge cases (does it interrupt damage? does it work while staggered?)
- Identity passives where relevant (e.g., does Synthetic take double damage from a specific source?)
- Which vessel is the starting vessel (Clone is the assumed default)
- Which vessels are unlocked from the start vs. behind meta currency

**Prompt questions to seed the brainstorm:**
- What's the *fantasy* of each vessel? Clone = "human under pressure"? Android = "controlled precision"? Synthetic = "fragile predator"?
- Is there a vessel built around an unconventional axis — no melee? No dodge but extra HP? Trades signature for second tool slot?
- Should one vessel have a *negative* identity trait that's a real downside?
- How distinct must vessels feel — are these flavors of the same playstyle, or are they fundamentally different builds?

**Decisions:** *(filled in during workshop)*

---

## W2 — Combat Feel & Weapon Patterns

**Status:** Not started
**Gates:** M1 (basic feel), M3.5 (full feel pass)
**Pre-read:** [PLANNING.md § Combat](PLANNING.md#combat--real-time-action), [PLANNING.md § Game Feel](PLANNING.md#game-feel)

**Output owed:**
- Melee weapon roster (sword-equivalent, hammer, dual-blade, etc.) with attack timing tables: windup frames, active frames, recovery frames, hitbox shape
- Ranged weapon roster (if any) with projectile speed, fire rate, ammo or cooldown rules
- Hit-stop magnitude per weapon (ms)
- Screen shake trauma values per event (hit, dodge, damage taken, death)
- Dodge: i-frame count, recovery frames, distance, post-dodge vulnerability
- Default attack chains (combo cancels?) or single-swing?
- Per-weapon "feel adjective" — heavy / fast / brutal / clean

**Prompt questions:**
- Is the default melee a single tap or a 3-hit combo?
- Can attacks be cancelled into dodge? Into other attacks?
- Do ranged weapons exist at all in the slice, or strictly melee + tools?
- Is hit-stop applied to the attacker, the target, or both?
- Should each vessel have a default weapon, or is the weapon always a found item?

**Decisions:** *(filled in during workshop)*

---

## W3 — Enemy Roster & Archetype Detail

**Status:** Not started
**Gates:** M3 (first enemy), M9 (full Sector 1 roster)
**Pre-read:** [PLANNING.md § Combat (enemy archetypes)](PLANNING.md#combat--real-time-action)

**Output owed:**
- Full enemy roster: ~3–5 enemies per sector × 5 sectors = ~15–25 enemies
- For Sector 1 specifically: 4–5 fully-designed enemies (the slice needs them)
- Per enemy: name, archetype, HP, attack power, move speed, attack patterns with telegraph, drop table, sector(s) they appear in, behavior tells, counter-strategy
- Elite variants: which base enemies have elites, what changes
- Aggro/sensor rules per enemy (line-of-sight required? aggro range?)

**Prompt questions:**
- What's the "starter enemy" the player meets in the first 30 seconds — what does it teach?
- Is there an enemy that exists primarily as a *threat to read*, not to defeat (a roaming hazard)?
- Should some enemies be sector-exclusive vs. generalists?
- Horror enemies: any enemy whose primary design goal is *atmosphere*, not challenge?
- What does the elite designation mechanically change — just stats, or new attack patterns?

**Decisions:** *(filled in during workshop)*

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

**Status:** Not started
**Gates:** M2 (basic doors + props), M5 (full primitive set for procgen)
**Pre-read:** [PLANNING.md § Room System](PLANNING.md#room-system), [PLANNING.md § Puzzle System](PLANNING.md#puzzle-system)

**Output owed:**
- Full door variant list (open, enemy-locked, key-locked, condition-locked, secret + extras: timer, hazard-gated, etc.)
- Hazard catalog: spike traps, gas, electric floors, vacuum breach, fire jets, falling debris — per hazard, the rules and damage model
- Interactive prop catalog: terminals, lockers, vending machines, breakable crates, chests, save points (do we have any?), beds (Adrenaline Rush refresh?), corpses (lootable?), cameras (alert mechanic?), pressure plates
- Puzzle primitive set (W7 expands the [PLANNING.md § Puzzle System](PLANNING.md#puzzle-system) starter list)
- Secret-room reveal rules: what discloses them? Adjacent presence + scanner? Breakable wall? Specific tool?
- Per-element: which sectors it appears in (some hazards are sector-exclusive)

**Prompt questions:**
- Should environmental hazards damage enemies too (use them tactically), or player-only?
- How many puzzle types per sector? Sector 1 might have 1–2 distinctive puzzle types.
- Are there *roomwide* mechanics — gravity flips, lights-out, slow-time zones?
- Should some doors require *combat solutions* (e.g., kill specific enemy to unlock)?

**Decisions:** *(filled in during workshop)*

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
