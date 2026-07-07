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

## Rule

Keep Stage 1 stable.
Use Stage 2 as the model for future reusable architecture.

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
