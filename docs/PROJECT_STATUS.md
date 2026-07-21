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
- [x] One-word vocabulary-practice C# foundation
- [x] Submitted-word transport from attack input to confirmed projectile impact
- [x] Optional target-word combat barrier
- [x] Teach, review, explanatory feedback, retry, and runtime record components
- [ ] Stage 1 Teach/Barrier/Review scene and Inspector wiring
- [ ] Unity Play Mode validation of the full educational loop
- [ ] Android device validation of the educational loop
- [ ] Expert validation of vocabulary content and assessment item
- [ ] Persistent educational result storage
- [ ] Final polish
- [ ] Architecture cleanup

### Notes

Stage 1 should remain stable. Avoid refactoring unless necessary. The vocabulary
prototype is opt-in until its Unity wiring is complete; do not report it as a
finished educational system or as evidence of vocabulary improvement.

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
- [x] Node 1 attack-refill TileSpawner scene object
- [ ] Balance pass

#### Corsair Phantom

- [x] Model/assets present
- [x] Controller exists
- [x] StartBattle flow
- [x] BeingHit / Death animation trigger flow
- [x] Ghost Glide
- [x] Piercing Scream damage / cone hit flow
- [x] Phantom Shot / ability foundation
- [x] Phantom Shot charge VFX activation
- [x] Player projectile targeting / damage scene tag
- [x] Player projectile damage / shared health bar sync
- [x] Tile Cracking integration
- [x] Node 2 death/progression callback
- [x] BeingHit / Death animation events
- [x] Node 2 attack-refill TileSpawner arena references
- [x] Node 2 circular arena boundary wiring
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
- [x] Node 1 to Node 2 encounter wiring
- [x] Stage 2 shared TileSpawner for attack-refill world tiles
- [x] Reusable circular arena boundary component
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

1. Wire the one-word vocabulary prototype in the Stage 1 scene using `STAGE1_VOCABULARY_PROTOTYPE_SETUP.md`.
2. Unity-test both correct-first-attempt and wrong-answer/retry educational flows.
3. Build and test the same Stage 1 flow on an Android device.
4. Resume Stage 2 progression validation from Barnacle Husk through Corsair Phantom.
5. Expand the vocabulary prototype only after the Stage 1 loop passes those tests.

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
