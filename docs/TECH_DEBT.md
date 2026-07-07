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

*Last Updated:* 2026-07-06
