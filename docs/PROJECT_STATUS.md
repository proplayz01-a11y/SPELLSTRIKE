# SPELLSTRIKE Project Status

> **Purpose**
>
> This file tells Codex and the team what is currently done, what is in progress, and what should be built next.
>
> Update this after every major development session.

---

# Current Development Focus

## Main Priority

**Stage 2: Sunken Seas**

Current goal:

1. Stabilize Stage 2 node flow.
2. Finish Corsair Phantom.
3. Implement Tide Wraith.
4. Implement Drowned Captain.
5. Verify Stage 2 debuff systems.

---

# Stage Progress

## Stage 1 — Enchanted Kingdom

**Status:** Mostly playable vertical slice

### Enemies

- [x] Bramble Sprite
- [x] Stone Sentinel
- [x] Cursed Jester
- [x] Hollow Knight Boss

### Systems

- [x] Basic combat
- [x] Word tile system
- [x] Enemy health
- [x] Boss health
- [x] Fragment pickup
- [x] Sword reward pickup
- [x] Node progression
- [x] Basic Stage Complete UI
- [ ] Final polish
- [ ] Architecture cleanup

### Notes

Stage 1 should remain stable. Avoid refactoring unless necessary.

---

## Stage 2 — Sunken Seas

**Status:** In progress

### Enemies

#### Barnacle Husk

- [x] Basic AI
- [x] Melee behavior
- [x] Water Burst
- [x] Tile Locking
- [x] Death callback
- [x] Potion drop
- [ ] Balance pass

#### Corsair Phantom

- [x] Model/assets present
- [x] Controller exists
- [x] StartBattle flow
- [x] Ghost Glide
- [x] Piercing Scream
- [x] Phantom Shot / ability foundation
- [ ] Tile Cracking integration
- [ ] Final balancing
- [ ] Final VFX polish

#### Tide Wraith

- [ ] Not started
- [ ] Stun
- [ ] Knockback
- [ ] Ranged pressure behavior

#### Drowned Captain

- [ ] Not started
- [ ] Boss AI
- [ ] Combined Stage 2 debuff mechanics
- [ ] Fragment/reward flow

### Stage 2 Systems

- [x] TileState system
- [x] TileDebuffManager
- [x] StageProgressionManager
- [x] StageNodeTrigger / StageNodeController
- [ ] Full node chain validation
- [ ] Stage 2 boss completion flow
- [ ] Stage 2 reward flow

---

# Core Systems

## Combat

- [x] Word submission
- [x] STRIKE button validation
- [x] Homing projectile
- [x] Damage scaling
- [x] Enemy damage receiving
- [ ] Standardized enemy damage interface

## Tile / Word System

- [x] Tile selection
- [x] Word bar
- [x] Dictionary validation
- [x] Tile refill
- [x] Tile states
- [ ] Stage-specific tile weighting verification

## Player

- [x] Movement
- [x] Health
- [x] Attack animation event
- [x] Potions
- [ ] Movement script consolidation

## Items / Rewards

- [x] Potion system
- [x] Passive item concept
- [x] Hollow Knight's Blade reward
- [ ] Full passive item database
- [ ] Full inventory persistence

## Save / Persistence

- [x] Basic persistence exists
- [ ] Unified save system
- [ ] Replace split PlayerPrefs / SQLite / JSON behavior if needed

---

# Immediate Next Recommended Tasks

1. Verify Stage 2 tile spawning in Unity after the `currentStage` correction.
2. Confirm Corsair Phantom's current abilities versus intended design.
3. Implement or finish Tile Cracking for Corsair Phantom.
4. Validate Stage 2 node progression from Node 1 to Node 2.
5. Create Tide Wraith using the enemy layer workflow.

---

# Session Update Template

Use this format after each Codex development session:

```md
## YYYY-MM-DD Session

### Completed
- 

### Changed Files
- 

### Unity Setup Needed
- 

### Bugs Found
- 

### Next Task
- 
```
