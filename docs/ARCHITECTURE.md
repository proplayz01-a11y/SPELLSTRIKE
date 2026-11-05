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

## Combat HUD scene coverage and lifetime (2026-11-05)

`Assets/Prefabs/PersistentGameplayHUD.prefab` is the single combat-HUD authoring
source and runtime owner for production gameplay. Each production stage scene
(`SampleScene`, `Stage2Scene`, `Stage3Scene`, and `Stage4Scene`) contains one
canonical prefab instance in its scene hierarchy. Its `PersistentHUDRoot` singleton
persists the complete Canvas hierarchy with `DontDestroyOnLoad`; when the next
stage loads, `PersistentPlayerRoot` rebinds the new stage's `TileManager`,
`TileSpawner`, `AttackController`, and player-health presentation to that HUD.
A newly loaded duplicate prefab instance is rejected by the HUD singleton, so
the active HUD from the current run remains the only runtime owner.

Stage 1 and Stage 2 retain their former 63-object scene-authored HUD hierarchies
as renamed, inactive `LegacySceneAuthoredHUD_*_Inactive` fallbacks. They are not
runtime presentation owners. Keep them until scene-load/rebind and regression
checks confirm that no serialized consumers still require the old object IDs;
remove them only in a separately backed-up cleanup pass. Stage 3/4 and the Stage
4 encounter preview scenes already use canonical prefab instances. TutorialScene
also uses the canonical prefab; `TutorialPersistentHudBridge` disables its
legacy combat visuals and rebinds tutorial consumers. Tutorial objectives and
dialogue remain tutorial-owned rather than becoming part of the shared combat
presentation. Stage 1 optimization tests and Mage HUD reference previews remain
isolated test/reference scenes with aligned copies, not extra production owners.

Preserve the 16 Tile children under `TilePoolPanel`; decorative frame/fill
objects remain siblings outside the counted `GridLayoutGroup`. Word Bar columns
and final cell width remain runtime-adaptive to selected tile count.

## Approved Android combat HUD baseline (2026-11-05)

The approved A05s live layout is serialized in Assets/Prefabs/PersistentGameplayHUD.prefab and mirrored by the Android code path. Tile Pool: 720x190 panel at Y=56.7, scale .83; frame/fill derived from the 1460:361 source crop at 720x178.027 and uniform .89; 8x2 GridLayoutGroup with UpperLeft alignment, compensated padding 23/23/16/12, cell 82.6x81.18 and spacing 2/3.5. The compensated insets preserve the original full 8x2 rack origin while partial pools compact from the first slot instead of being centered; the remaining tiles may reorder among the wells. Desktop TileManager computes equivalent first-slot insets from the live panel/cell dimensions. Word Bar: 733.3334x89.7 panel at Y=215.56, scale 1.02/.97/.85; artwork derives from the 1648:168 crop at 709.13135x76.01638 and unit scale. Its GridLayoutGroup uses padding 9/12/8/8, 2.4 horizontal gap and a 66.5px base cell. TileManager uses the same values for tile reparent/return updates. Word Bar column count and final cell width are dynamic by selected tile count; do not clamp gameplay to the captured 11-column Inspector state. The approved ornate STRIKE uses unit scale. Independent panel fill opacity is authored at alpha 215/255 (0.84313726, approximately 84.3%) for both Word Bar backdrop and Tile Pool fill; `PersistentHUDRoot` serializes and reapplies the same values at runtime.

## Persistent Combat HUD authoring parity

The canonical PersistentGameplayHUD prefab is the shared authoring source for the combat HUD. For Android Edit Mode review, open Assets/Prefabs/PersistentGameplayHUD.prefab in Prefab Mode with Android as the active Build Target and run SPELLSTRIKE > HUD > Sync Android Edit Preview. This applies the same TutorialResponsiveLayout calculation used in Play Mode to the Tile Pool, paired art layers, Word Bar, STRIKE, CanvasScalers, and sibling draw order. Review the result and save the Prefab Stage explicitly.

During Play Mode, use SPELLSTRIKE > HUD > Open Live HUD Editor. The standalone EditorWindow resolves the actual PersistentHUDRoot runtime instance in DontDestroyOnLoad, even while a separate PersistentGameplayHUD Prefab Stage is open. Its Pause Responsive Layout For Live Editing toggle pauses TutorialResponsiveLayout and TileManager's Tile Pool/Word Bar grid writers. Auto Align Panel Artwork keeps each frame/fill pair attached to its panel by default; turn it off only to tune the decorative layers independently. The window exposes RectTransforms, GridLayoutGroups, STRIKE transforms and independent panel fill opacity. With live editing paused, changes are applied directly to the running Game View.

Unity normally reverts runtime object changes when Play Mode exits. CombatHudPlayModeSnapshotCapture writes a timestamped JSON snapshot of the runtime HUD transforms, grid settings and fill opacities to RecoveryBackups/HUD_FRAME_FILL_20261104 on ExitingPlayMode. The JSON is not applied to or allowed to overwrite the canonical prefab automatically. Review the snapshot and explicitly apply accepted values to Assets/Prefabs/PersistentGameplayHUD.prefab after exiting Play Mode. With live editing off, the existing responsive layout and gameplay grid ownership run normally.

## Shared PC / Android Gameplay Input and HUD Ownership

Desktop input remains intact while mobile input is layered onto the same gameplay
owners rather than emulating keyboard keys.

```text
MobileJoystickController
-> owns one movement touch and exposes normalized movement input
-> MovementofPlayer consumes it beside the existing desktop/gamepad paths

CombatCamera
-> owns one right-side empty-area touch for yaw/pitch
-> rejects touches that begin over EventSystem UI
-> permits a separate joystick finger at the same time

MobileStageControls
-> owns the shared Stage 1/2 contextual interaction button
-> world pickups offer/clear one guarded COLLECT action
-> existing desktop E interactions remain available

PersistentPlayerRoot
-> owns the one runtime player instance across production gameplay scene loads
-> applies the destination PlayerSpawnPoint and rebinds stage-owned dependencies

PersistentHUDRoot
-> owns one canonical PersistentGameplayHUD prefab instance for the active run
-> persists its combat Canvas hierarchy with DontDestroyOnLoad
-> caches health, enemy-health, Tile Pool, Word Bar, STRIKE, potion and mobile UI

TutorialResponsiveLayout
-> responsive layout authority attached to the persistent combat Canvas
-> applies safe-area/scaling rules and the approved Android HUD geometry
-> remains separate from tutorial-only objectives/dialogue layout
```

The shared gameplay presentation is player health, enemy health, Word Bar,
8 x 2 Tile Pool, STRIKE, potion UI, and shared mobile controls. The canonical
HUD is a separate persistent root rather than a child of the player. Stage-owned
`TileManager`, `TileSpawner`, and `AttackController` are rebound to its panels and
button on each scene load; the singleton prevents multiple active HUD owners.

Progression-critical mobile interactions must invoke the same guarded gameplay
action used by desktop input. A separate mobile Jump action is not part of the
current Stage 1/2 completion contract and should be added only if device testing
proves it is required.

---
