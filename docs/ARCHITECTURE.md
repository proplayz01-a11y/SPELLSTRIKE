# SPELLSTRIKE Architecture

> **Purpose**
>
> This document explains how the main SPELLSTRIKE systems connect.
> Codex should read this before making architectural changes.

---

# Architectural Philosophy

SPELLSTRIKE uses Unity `MonoBehaviour` components with Inspector-driven references.

Preferred direction:

```text
Small components
↓
Inspector references
↓
Reusable systems
↓
Layer-by-layer implementation
```

Avoid:

```text
Giant controllers
Hardcoded scene names
Duplicate managers
Unplanned rewrites
```

---

# Core Runtime Flow

```text
Player enters node
↓
StageNodeTrigger activates encounter
↓
Enemy StartBattle()
↓
Player collects/selects tiles
↓
TileManager builds word
↓
AttackController validates word
↓
Player presses STRIKE
↓
Attack animation plays
↓
Animation event fires projectile
↓
Projectile hits enemy
↓
EnemyHealth / enemy controller receives damage
↓
Enemy dies
↓
Node controller/progression manager advances stage
```

---

# Player System

## Main Responsibilities

```text
PlayerMovement / MovementofPlayer
- Movement input
- Arena navigation
- Stun or lock states

PlayerHealth
- Player HP
- Damage intake
- Death handling

AttackController
- Word validation
- STRIKE button state
- Attack animation
- Projectile firing
- Damage calculation
```

## Risk

There may be duplicate movement scripts. Do not consolidate until gameplay is stable.

---

# Word and Tile System

## Main Responsibilities

```text
DictionaryManager
- Validates English words

TileManager
- Manages tile pool
- Manages selected word
- Handles tile refill/replacement
- Holds stage-related tile settings

Tile
- Represents individual letter tile
- Selection state
- TileState for debuffs
```

## Stage 2 Extension

```text
TileState
- Normal
- Locked
- Cracked
- Broken

TileDebuffManager
- Tile locking
- Tile cracking
- Tile restoration / respawn
```

## Rule

Do not create another tile debuff manager. Extend the existing one.

---

# Enemy System

## Standard Enemy Flow

```text
Idle
↓
StartBattle()
↓
Find player
↓
Chase / approach
↓
Attack
↓
Hit reaction
↓
Death
↓
Notify progression
```

## Preferred Components

```text
Enemy root object
├── NavMeshAgent
├── Collider
├── Rigidbody if needed
├── EnemyHealth or health owner
├── EnemyController
└── VisualModel
    └── Animator
```

## Movement

Use `NavMeshAgent` for grounded/gliding enemies unless explicitly changed.

## Animation

Use Animator parameters and animation events.
Root Motion should usually be OFF.

---

# Boss System

Bosses may use larger custom controllers, but should still follow the same principles:

```text
Activation
Movement
Attack selection
Animation event damage
Hit reaction
Death
Reward / fragment unlock
Stage completion
```

Future refactor target:

```text
BossMovement
BossCombat
BossAbilities
BossAnimationRelay
BossStateMachine
```

---

# Stage Progression

## Current Situation

Stage 1 uses bespoke progression scripts.

Stage 2 introduces reusable node architecture:

```text
StageProgressionManager
StageNodeTrigger
StageNodeController
```

`StageNodeController` supports two completion styles:

```text
Enemy defeated
↓
Fragment unlocks
↓
Player collects fragment
↓
Node completes
```

or, for encounters without a fragment pickup:

```text
Enemy defeated
↓
Node completes directly
```

## Rule

Keep Stage 1 stable.
Use Stage 2 as the model for future reusable architecture.

## Arena Boundaries

Stage combat locks can use an arena boundary object assigned to both
`StageNodeTrigger` and `StageNodeController`.

For circular arenas, use `CircularArenaBoundary` on the boundary wrapper. The
component keeps visual ring art separate from collision and generates low-poly
`BoxCollider` wall segments from Inspector-tuned radius, height, thickness, and
segment count.

Runtime flow:

```text
StageNodeTrigger starts combat
↓
Boundary wrapper activates
↓
CircularArenaBoundary shows visual ring and generates colliders
↓
StageNodeController completes node
↓
Boundary wrapper deactivates
```

---

# Vocabulary Practice Prototype

The Stage 1 educational prototype extends the existing Tile and combat systems
without replacing them.

```text
StageVocabularyProfile (prototype content)
-> VocabularyTeachController (presentation + guided reconstruction)
-> AttackController carries the submitted word into HomingProjectile
-> VocabularyBarrier evaluates the word at confirmed enemy impact
-> StageVocabularyReviewController (question + feedback + corrective retry)
-> VocabularyPracticeSession (runtime attempt records + summary)
-> StageCompleteUI returns the player to StageSelectScene
```

`TileManager.EnsureWordLettersAvailable` guarantees the required target letters
inside the existing Tile Pool, replaces only surplus letters, and shuffles the
pool. It does not arrange or reveal the answer.

The combat barrier is an optional component. Enemies without
`VocabularyBarrier` retain their existing damage behavior. While a barrier is
active, a non-target valid word still damages the enemy at a reduced multiplier;
the target word breaks the barrier and receives the configured bonus.

`VocabularyPracticeSession` is runtime-only in this layer. Persistent SQLite or
JSON educational records are deferred until the current split save architecture
is resolved. Prototype content must not be labelled expert-reviewed until an
actual reviewer and validation record exist.

---

# Items and Rewards

## Current Flow

```text
Enemy/boss dies
↓
Reward object unlocks
↓
Player interacts
↓
Reward is granted
↓
Progression advances
```

## Passive Items

Passive items should not instantly break current-stage balance.
New passive items should generally become useful starting from the next stage unless explicitly designed otherwise.

---

# Save System

## Current Risk

Persistence may be split between:

```text
SQLite / JSON
PlayerPrefs
Runtime-only state
```

## Future Goal

One unified persistence layer for:

- Stage progress
- Player stats
- Words formed
- Inventory
- Potions
- Passives
- Educational results

---

# Architecture Rules for Codex

Before changing architecture:

1. Inspect the existing implementation.
2. Identify the system owner.
3. Avoid duplicate managers.
4. Avoid renaming public/serialized fields.
5. Prefer extension over replacement.
6. Ask before large refactors.
7. Update this document if architecture changes.
