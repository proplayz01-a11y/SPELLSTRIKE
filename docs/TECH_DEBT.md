# SPELLSTRIKE Technical Debt

> **Purpose**
>
> This file is a living backlog of architectural issues, code smells,
> duplication, and future refactors. It is **not** a to-do list for the
> current sprint.
>
> **Rule:** Never interrupt feature development just to clean technical
> debt unless it blocks progress.

------------------------------------------------------------------------

# Priority Legend

-   🔴 Critical -- Can cause bugs, data corruption, or architecture
    failure.
-   🟠 High -- Should be fixed before the project becomes significantly
    larger.
-   🟡 Medium -- Cleanup/refactor after major gameplay milestones.
-   🟢 Low -- Cosmetic or quality improvements.

------------------------------------------------------------------------

# Current Technical Debt

## TD-001 --- Stage Progression Architecture

**Priority:** 🟠 High

### Issue

Two progression systems currently exist: - Stage 1 uses bespoke
controllers. - Stage 2 introduces reusable node architecture.

### Risk

Future stages may become inconsistent.

### Current Decision

Keep Stage 1 stable. Migrate only after Stage 2--5 are complete.

**Status:** Deferred

------------------------------------------------------------------------

## TD-002 --- Player Movement Duplication

**Priority:** 🟠 High

### Issue

Multiple player movement scripts exist (e.g. `MovementofPlayer` and
`PlayerMovement`).

### Risk

Bug fixes may only affect one implementation.

### Current Decision

Do not merge until gameplay stabilizes.

**Status:** Deferred

------------------------------------------------------------------------

## TD-003 --- Enemy Health Ownership

**Priority:** 🟠 High

### Issue

Enemy health may be split between controller-local variables and
`EnemyHealth`.

### Risk

Damage and death logic can become inconsistent.

### Current Decision

Move toward a single health authority in the future.

**Status:** Investigate after Stage 2.

------------------------------------------------------------------------

## TD-004 --- SendMessage Usage

**Priority:** 🟡 Medium

### Issue

Some systems rely on `SendMessage()`.

### Risk

Weak compile-time safety and harder refactoring.

### Current Decision

Replace with events/interfaces after core gameplay is complete.

**Status:** Deferred

------------------------------------------------------------------------

## TD-005 --- Persistence Split

**Priority:** 🟠 High

### Issue

SQLite/JSON and PlayerPrefs are both used.

### Risk

Save data becomes fragmented.

### Current Decision

Design one unified save pipeline before release.

**Status:** Deferred

------------------------------------------------------------------------

## TD-006 --- Large Controller Scripts

**Priority:** 🟡 Medium

### Issue

Some controllers are becoming very large.

### Guideline

Split into: - Movement - Combat - Abilities - Animation Events - State
Machine

Only refactor after gameplay is stable.

**Status:** Monitor

------------------------------------------------------------------------

## TD-007 --- Stage 2 TileManager Stage Value

**Priority:** 🟠 High

### Issue

`TileManager.currentStage` was serialized as `1` inside Stage 2.

### Action

Confirmed as a Stage 2 scene configuration bug and corrected
`Assets/Scenes/Stage2Scene/Stage2Scene.unity` so the scene `TileManager`
uses `currentStage: 2`.

**Status:** Resolved 2026-07-06

------------------------------------------------------------------------

## TD-008 --- Documentation Drift

**Priority:** 🟢 Low

### Issue

Some handoff documents no longer reflect the current implementation.

### Current Decision

Update documentation periodically instead of every feature.

**Status:** Ongoing

------------------------------------------------------------------------

## TD-009 --- Stage 2 Tile Spawning Validation

**Priority:** Medium

### Issue

`Stage2Scene` has `TileManager.currentStage` serialized as `2`, and now has a
serialized `TileSpawner` for attack-refill world tiles. However, the current
`TileManager.GenerateDictionaryWeightedLetters(int amount, int stage)` path does
not yet apply the stage-specific weights returned by `GetStageLengthWeights`.
Stage 2 tile spawning also still needs Unity validation against actual Ground
layer colliders.

### Risk

Stage 2 may appear to be stage-configured while still using mostly generic tile
generation behavior. Future Stage 2 nodes also need their own spawn-area
validation as they are wired.

### Current Decision

Stage 2 `TileSpawner` scene wiring was restored on 2026-07-09. Unity-test the
Node 1 and Node 2 attack-refill flow plus Ground layer raycast behavior, then
keep the stage-specific weighting investigation deferred until the Stage 2 node
chain is stable.

**Status:** Partially Resolved / Monitor

------------------------------------------------------------------------

## TD-010 --- TileSpawner Startup Pool Duplication

**Priority:** Medium

### Issue

`TileSpawner.Start()` still spawns initial tile objects into the tile pool, while
`TileManager` also owns UI tile pool prefill.

### Risk

Scenes that use both components can briefly create duplicate tile objects and
depend on cleanup behavior to remove world-tile prefabs from the UI pool.

### Current Decision

Keep the behavior stable for now. Revisit after Stage 2 attack-refill spawning
is manually confirmed.

**Status:** Monitor

------------------------------------------------------------------------

## TD-011 --- Stage 2 Node 2 Arena Boundary Placement

**Priority:** Medium

### Issue

Stage 2 Node 2 now uses `C_ArenaBoundary` with a generated circular collider
wall and the `Stage2_CP-ARENA BOUNDARY` visual ring, but the collision feel
still needs Unity playtesting.

### Risk

Corsair Phantom combat should lock the player inside the intended arena, but the
radius, height, wall thickness, and visual-ring alignment may need tuning in
Unity.

### Current Decision

Use `CircularArenaBoundary` instead of hand-placed collider boxes or a torus
mesh collider. Validate the generated collider wall in Play Mode before treating
Node 2 arena locking as final.

**Status:** Partially Resolved / Validate

------------------------------------------------------------------------

## TD-012 --- Vocabulary Prototype Validation and Persistence

**Priority:** High

### Issue

The Stage 1 vocabulary-practice code compiles, but its Teach UI, Bramble Sprite
barrier, mandatory review, and Stage Complete integration are not yet wired or
playtested in the Unity scene. Attempt records are runtime-only, and the
`MITIGATE` content has not been reviewed by a qualified English or education
expert.

### Risk

Documentation could overstate a coded foundation as a finished educational
system. Results would also be lost when the runtime session ends, and unvalidated
content would not support the manuscript's expert-reviewed wording.

### Current Decision

Complete the exact Inspector wiring and Play Mode checklist in
`STAGE1_VOCABULARY_PROTOTYPE_SETUP.md`, then test the same loop on Android.
Defer SQLite/JSON persistence until the prototype flow is stable and the
project's split persistence architecture has a chosen owner.

**Status:** Open / Validate Before Expansion

------------------------------------------------------------------------

# Future Refactor Candidates

-   Replace bespoke Stage 1 progression with reusable node framework.
-   Standardize enemy base architecture.
-   Standardize boss architecture.
-   Unify save system.
-   Reduce controller sizes.
-   Replace reflection-based messaging.
-   Consolidate duplicate utility code.

------------------------------------------------------------------------

# Rules

Before adding a new entry:

-   Describe the issue.
-   Explain why it matters.
-   Assign a priority.
-   Record the current decision.
-   Never refactor immediately unless the issue blocks development.

------------------------------------------------------------------------

*Last Updated:* 2026-07-15
