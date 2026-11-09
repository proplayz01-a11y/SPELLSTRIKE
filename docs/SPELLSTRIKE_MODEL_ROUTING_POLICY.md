# SPELLSTRIKE Codex Model Routing Policy

## Purpose

This policy defines when Codex should recommend **GPT-5.6 Terra**, **GPT-5.6 Sol**, or a **Terra → Sol escalation path** while working on SPELLSTRIKE.

The goal is to use the appropriate model for the task while preserving SPELLSTRIKE's development principles:

- canonical documentation first
- explicit architecture
- modular ownership
- incremental implementation
- smallest safe changes
- real runtime validation
- documented technical debt
- evidence-driven evolution

Model selection must be based on **semantic complexity, architectural risk, and uncertainty**, not merely on file count or task size.

---

## Default Model

**Default: GPT-5.6 Terra**

Use Terra for implementation work when the architecture, ownership, and intended behavior are already clear.

GPT-5.6 Sol should be recommended only when the task crosses the escalation criteria defined below.

---

# Core Routing Principle

> **Sol decides or repairs architecture. Terra executes established architecture.**

Either model may implement code, but Terra must not silently redefine canonical architecture, ownership boundaries, or state authority.

If a task begins as ordinary implementation but reveals architectural uncertainty, Terra must stop speculative patching and recommend escalation to Sol.

---

# Routing Categories

Every non-trivial coding task should be classified into one of three categories before implementation:

1. **TERRA**
2. **SOL**
3. **TERRA → SOL**

The classification should happen after reading the relevant canonical documentation and inspecting enough code to understand the task.

---

# 1. Use GPT-5.6 Terra

Recommend **Terra** when the task primarily involves implementing or maintaining an already-defined design.

Typical Terra tasks include:

- implementing behavior from an approved specification
- wiring animation events
- adding or adjusting hitboxes
- damage, stun, knockback, cooldown, or status-effect logic
- connecting existing components
- straightforward Unity Inspector setup changes
- UI wiring
- database save/load implementation from an established contract
- fixing obvious compiler errors
- localized runtime bugs with a clear root cause
- small or medium refactors that preserve ownership boundaries
- extracting duplicate code without changing system authority
- adding tests
- updating canonical documentation after implementation
- changelog updates
- project status updates
- mechanical renames
- repetitive edits
- implementing an existing pattern for another enemy, node, or stage

### Terra Example

Task:

> Wire the approved Anchor Pursuit Strike animation events into the existing Drowned Captain controller and reuse the established AnchorCollider one-hit logic.

Routing:

```text
MODEL ROUTING

Recommended: TERRA

Architecture defined: YES
Cross-system task: NO
Root cause known: N/A
Canonical ownership change: NO
High regression risk: LOW
Previous fix failed: NO

Reason:
The behavior, ownership, and shared collider design are already established.
This task is implementation and validation, not architecture design.
```

---

# 2. Use GPT-5.6 Sol

Recommend **Sol** when correctness depends on resolving architecture, ownership, systemic ambiguity, or high-risk behavior.

Typical Sol tasks include:

- designing or changing system architecture
- establishing canonical ownership
- resolving competing sources of truth
- redesigning state management
- diagnosing bugs that cross several subsystems
- debugging behavior where the root cause remains unclear after focused inspection
- reconciling canonical documentation with contradictory runtime behavior
- major cross-system refactors
- changing stage progression architecture
- changing encounter lifecycle architecture
- changing combat ownership or ability-state architecture
- changing data flow across multiple managers
- redesigning persistence or save architecture
- security-sensitive architecture
- concurrency or ordering problems
- lifecycle problems that depend on several Unity systems
- final-defense architecture audits
- regression analysis across major systems
- evaluating whether technical debt now requires redesign
- fixing repeated patches that have created fragile behavior

### Sol Example

Task:

> Diagnose why Node 1 sometimes enters combat even though the shrine trigger reports that the node is inactive.

If inspection reveals:

- the trigger fires correctly,
- the shrine state is correct,
- but two managers disagree about encounter ownership,

then routing becomes:

```text
MODEL ROUTING

Recommended: SOL

Architecture defined: NO
Cross-system task: YES
Root cause known: PARTIAL
Canonical ownership change: POSSIBLE
High regression risk: HIGH
Previous fix failed: YES

Reason:
The failure is no longer a localized trigger issue.
Multiple systems disagree about encounter authority, creating competing state paths.
The canonical ownership model must be established before implementation continues.
```

---

# 3. Use Terra → Sol

Use **Terra → Sol** when the task initially appears localized, but the root cause is not yet known.

Terra should perform **one focused diagnostic pass**.

If that pass establishes a clear localized cause, remain on Terra.

If the pass reveals architectural ambiguity, ownership conflicts, or systemic risk, stop implementation and recommend Sol.

### Example

Initial task:

> Determine why the real Node 1 shrine SphereCollider did not invoke the encounter when the Player CharacterController crossed it.

Initial routing:

```text
MODEL ROUTING

Recommended: TERRA → SOL

Architecture defined: YES
Cross-system task: POSSIBLE
Root cause known: NO
Canonical ownership change: UNKNOWN
High regression risk: MEDIUM
Previous fix failed: NO

Reason:
The issue may be a localized Unity collider/configuration problem.
Perform one focused diagnostic pass first.
Escalate to Sol if encounter ownership, state authority, or cross-system behavior is implicated.
```

Possible diagnostic result:

```text
SphereCollider: valid
IsTrigger: enabled
Player enters trigger: confirmed
OnTriggerEnter: confirmed
Encounter request: emitted
Encounter manager rejects request due to conflicting node state
```

At this point:

```text
ESCALATION

TERRA → SOL

Reason:
The physics layer is functioning correctly.
The failure occurs in encounter-state ownership and is therefore architectural.
Do not add fallback booleans, duplicate state, or local bypasses.
```

---

# Mandatory Routing Check

Before substantial implementation, Codex should briefly evaluate:

```text
MODEL ROUTING CHECK

Architecture defined?        YES / NO / PARTIAL
Cross-system task?            YES / NO
Root cause known?             YES / NO / PARTIAL
Canonical ownership change?   YES / NO / POSSIBLE
High regression risk?         LOW / MEDIUM / HIGH
Previous fix failed?          YES / NO

Recommendation:
TERRA / SOL / TERRA → SOL

Reason:
<2-4 concise sentences>
```

This routing output should remain concise.

Do not turn model selection into a long discussion unless the user asks for justification.

---

# Escalation Triggers

Terra should recommend Sol when **any meaningful architectural trigger** occurs.

Strong escalation triggers include:

1. The root cause remains unclear after one focused diagnostic pass.
2. The proposed fix requires changing an established ownership boundary.
3. Multiple systems disagree about state or authority.
4. Canonical documentation conflicts with runtime implementation.
5. A proposed fix would introduce a second source of truth.
6. A previous fix created a new regression elsewhere.
7. The task has shifted from implementation into redesign.
8. Correctness depends on several interacting lifecycle systems.
9. The change affects stage progression, encounter lifecycle, persistence, or other high-impact architecture.
10. The safest solution requires modifying a canonical contract.
11. Codex cannot explain the failure path with sufficient confidence.
12. Existing code contains stacked workaround logic rather than a clear root cause.
13. A local patch would hide rather than resolve the systemic problem.
14. The task requires a final architecture or regression audit before defense.

---

# Do Not Route by File Count Alone

File count is not a reliable proxy for complexity.

Example:

```text
Rename EnemyHealth to CombatHealth across 20 files
```

This is still likely a **Terra** task.

But:

```text
Change authority between EncounterManager and StageProgressionManager
```

may involve only two files and still require **Sol**.

Routing must prioritize:

- ownership
- state authority
- system coupling
- ambiguity
- failure impact
- architectural change
- regression risk

---

# Anti-Patterns

## Do Not Use Sol for Trivial Work

Avoid recommending Sol for:

- simple serialized fields
- obvious null checks
- formatting
- documentation-only updates
- mechanical renames
- straightforward animation-event wiring
- repetitive code changes
- implementation where the canonical architecture is already explicit

---

## Do Not Let Terra Patch Indefinitely

Terra must not follow this pattern:

```text
Bug
↓
Patch
↓
New bug
↓
Patch
↓
Add fallback boolean
↓
Add another manager check
↓
Add special-case bypass
↓
System behavior becomes unclear
```

Instead:

```text
Bug
↓
Focused diagnostic pass
↓
Root cause clear?
├── YES → smallest safe fix
└── NO  → recommend Sol
```

---

## Do Not Create Alternate State Paths

Neither model should solve a problem by introducing:

- duplicate state ownership
- shadow booleans
- hidden fallback managers
- parallel encounter activation paths
- alternate progression logic
- undocumented bypasses

unless a canonical architectural decision explicitly requires them.

---

# Canonical Documentation Rules

Before routing architecture-heavy tasks, read the relevant canonical sources.

At minimum, when applicable:

```text
AGENTS.md
.codex/skills/spellstrike/SKILL.md
docs/ARCHITECTURE.md
docs/PROJECT_STATUS.md
docs/CHANGELOG.md
docs/TECH_DEBT.md
```

Additional system-specific documentation should be read when relevant.

If code behavior conflicts with canonical documentation:

1. do not silently choose one,
2. identify the conflict,
3. determine which source reflects the intended architecture,
4. recommend Sol if architectural reconciliation is required,
5. update canonical documentation after the implementation is validated.

---

# Implementation Rules After Routing

## If Terra Is Selected

Terra should:

1. preserve existing architecture,
2. make the smallest safe change,
3. reuse established patterns,
4. avoid speculative redesign,
5. validate the actual runtime behavior,
6. update documentation where required,
7. record unresolved debt explicitly.

---

## If Sol Is Selected

Sol should:

1. establish the failure path or architectural problem first,
2. identify canonical ownership,
3. define the intended responsibility boundaries,
4. reject competing sources of truth,
5. propose the smallest architecture-correct change,
6. implement only after the model is coherent,
7. validate runtime behavior,
8. check for regressions,
9. update canonical documentation,
10. record remaining technical debt.

---

# Runtime Validation Requirement

Model strength does not replace runtime evidence.

A task is not complete merely because:

- the code compiles,
- the diff looks correct,
- the architecture sounds plausible,
- Codex believes the change should work.

For gameplay behavior, validate the real runtime path whenever practical.

Examples:

- real Player object
- real scene object
- real collider
- real encounter
- real animation event
- real state transition
- real combat completion path

Prefer evidence over assumption.

---

# SPELLSTRIKE-Specific Routing Examples

| Task | Recommended Model |
|---|---|
| Wire Corsair Phantom animation events | Terra |
| Implement an approved Corsair Phantom ability | Terra |
| Redesign Corsair Phantom combat state ownership | Sol |
| Add Drowned Captain Pursuit Strike damage | Terra |
| Design shared AnchorCollider authority | Sol |
| Fix a known collider Inspector setting | Terra |
| Diagnose a collider failure spanning physics + encounter state | Terra → Sol |
| Implement Tide Wraith behavior from an approved spec | Terra |
| Redesign the combat ability framework | Sol |
| Fix a clear Stage 2 TileManager configuration bug | Terra |
| Redesign stage-aware vocabulary generation | Sol |
| Update PROJECT_STATUS.md | Terra |
| Update CHANGELOG.md | Terra |
| Audit docs against runtime before final defense | Sol |
| Routine QA bug fixes | Terra |
| Final architecture/regression audit | Sol |
| Mechanical refactor following an existing pattern | Terra |
| Resolve duplicate managers or competing state ownership | Sol |

---

# Decision Heuristic

Use this mental model:

```text
"How do we implement the already-approved behavior?"
→ TERRA

"Why does this system behave this way?"
→ TERRA → SOL if the cause is not quickly established

"Where should this responsibility live?"
→ SOL

"Which system should own this state?"
→ SOL

"Can you wire this existing design?"
→ TERRA

"Do we need to redesign this?"
→ SOL
```

---

# Final Policy

SPELLSTRIKE should normally use **GPT-5.6 Terra as the daily implementation model**.

Use **GPT-5.6 Sol as the architecture, ambiguity, high-risk debugging, and systemic-refactor model**.

When uncertain, begin with a focused Terra diagnostic pass rather than immediately escalating.

However, once the problem becomes architectural:

> **Stop speculative patching. Escalate to Sol. Establish the canonical model first.**

The purpose of this policy is not to maximize model power.

The purpose is to preserve architectural integrity while using the appropriate amount of reasoning for each task.

