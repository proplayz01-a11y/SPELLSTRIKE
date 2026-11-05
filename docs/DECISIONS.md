# SPELLSTRIKE Decisions Log

> **Purpose**
>
> This file records important project decisions and the reasoning behind them.
> Use this when Codex or the team asks: "Why did we do it this way?"

---

# Decision Template

```md
## DEC-000 — Title
**Date:** YYYY-MM-DD  
**Status:** Accepted / Rejected / Deferred / Revisited

### Decision
What was decided?

### Reason
Why was this chosen?

### Consequences
What does this affect?
```

---

# Accepted Decisions

## DEC-001 — Use Layer-by-Layer Development
**Date:** 2026-07-06  
**Status:** Accepted

### Decision
Implement systems in small layers instead of building complete features all at once.

### Reason
This prevents broken prefabs, huge scripts, and impossible debugging.

### Consequences
Every new enemy should start with foundation first:
movement, detection, attack, hit/death, then abilities.

---

## DEC-002 — Keep Stage 1 Stable
**Date:** 2026-07-06  
**Status:** Accepted

### Decision
Do not aggressively refactor Stage 1 while Stage 2 is being built.

### Reason
Stage 1 is the playable vertical slice and should remain reliable for demos and defense.

### Consequences
Stage 1 may keep bespoke scripts temporarily.

---

## DEC-003 — Use NavMeshAgent for Enemy Movement
**Date:** 2026-07-06  
**Status:** Accepted

### Decision
Enemies should use `NavMeshAgent` for movement unless a specific mechanic requires otherwise.

### Reason
NavMeshAgent is easier to tune, debug, and reuse across enemies.

### Consequences
Animation root motion should usually remain OFF.

---

## DEC-004 — Animator Events Handle Attack Timing
**Date:** 2026-07-06  
**Status:** Accepted

### Decision
Attack damage, projectile firing, and special impact timing should happen through animation events when appropriate.

### Reason
This keeps gameplay timing synced with animations.

### Consequences
Codex must preserve existing animation event methods and not rename them casually.

---

## DEC-005 — Stage 2 Introduces Debuffs
**Date:** 2026-07-06  
**Status:** Accepted

### Decision
Stage 1 has no major debuffs. Stage 2 introduces tile and player debuffs.

### Reason
Stage 1 teaches basic combat. Stage 2 increases pressure.

### Consequences
Stage 2 enemies should introduce debuffs gradually:
- Barnacle Husk: Tile Locking / Bleeding
- Corsair Phantom: Tile Cracking
- Tide Wraith: Stun / Knockback
- Drowned Captain: combined pressure

---

## DEC-006 — Passive Items Should Not Break Same-Stage Balance
**Date:** 2026-07-06  
**Status:** Accepted

### Decision
Newly acquired passive items should generally take effect starting from the next stage unless explicitly designed otherwise.

### Reason
This prevents sudden power spikes during the same encounter path.

### Consequences
Reward and inventory systems should separate "unlocked" from "active/equipped."

---

## DEC-007 — Keep Technical Debt Visible but Deferred
**Date:** 2026-07-06  
**Status:** Accepted

### Decision
Known architectural issues should go into `TECH_DEBT.md` instead of being fixed immediately.

### Reason
Finishing playable systems is currently more important than perfect architecture.

### Consequences
Codex should not refactor technical debt unless explicitly instructed.

---

## DEC-010 --- Use Generated Circular Arena Boundaries
**Date:** 2026-07-10
**Status:** Accepted

### Decision
Circular arena locks should use `CircularArenaBoundary` to generate collider
segments at runtime while visual ring art remains a separate child/reference.

### Reason
This avoids hand-placing many box colliders for every circular arena, keeps
collision simpler than mesh-collider torus walls, and gives later Stage 2 nodes
a reusable setup.

### Consequences
Arena boundary wrappers should be wired to `StageNodeTrigger` for activation
and `StageNodeController` for cleanup. Visual meshes can be swapped or scaled
without rewriting combat flow.

---

## DEC-011 --- Build the Vocabulary Flow as an Opt-In One-Word Vertical Slice
**Date:** 2026-07-15
**Status:** Accepted

### Decision
Implement the first educational loop with one fixed Stage 1 prototype word and
small optional components for Teach, combat retrieval, post-stage review,
explanatory feedback, corrective retry, and runtime attempt records.

### Reason
The complete five-stage educational system is not yet validated. A one-word
vertical slice makes the claimed learning activity observable and testable while
preserving the working combat architecture and preventing premature scope
expansion.

### Consequences
The prototype uses `MITIGATE` as unvalidated placeholder content. It does not
prove vocabulary improvement, does not count as expert-reviewed content, and
does not yet persist educational results. Expansion to more words or stages
should follow only after Unity and Android playtests confirm this loop.

---

# Deferred Decisions

## DEC-008 — Unified Save System
**Date:** 2026-07-06  
**Status:** Deferred

### Decision
A final save architecture is not locked yet.

### Reason
The game still has changing systems for inventory, progression, vocabulary tracking, and educational metrics.

### Consequences
Avoid major save-system rewrites until the core gameplay loop stabilizes.

---

## DEC-009 — Stage 1 Migration to Reusable Node System
**Date:** 2026-07-06  
**Status:** Deferred

### Decision
Stage 1 will not be immediately migrated to the Stage 2 reusable node architecture.

### Reason
Stage 1 is already working and should not be destabilized.

### Consequences
Future migration can happen after Stage 2–5 architecture is proven.

## DEC-029 - Use One Persistent Combat HUD Prefab Across Production Stages
**Date:** 2026-11-05
**Status:** Accepted; canonical prefab instances authored for production Stages 1-4; runtime transition validation pending

### Decision
Use `Assets/Prefabs/PersistentGameplayHUD.prefab` as the sole combat-HUD authoring
source and the one runtime HUD owner for each production gameplay scene. Each
production scene contains one canonical prefab instance. Its `PersistentHUDRoot`
singleton persists the complete Canvas hierarchy with `DontDestroyOnLoad` and
rejects newly loaded duplicate instances. `PersistentPlayerRoot` rebinds the
active stage's player health, `TileManager`, `TileSpawner`, and `AttackController`
to the retained HUD after each scene load.

Tutorial-only objectives/dialogue remain scene-owned. Stage 1 and Stage 2 keep
their former scene-authored HUD hierarchies only as renamed, inactive fallbacks
until regression checks prove that all serialized consumers have been rebound;
remove those fallback objects only in a separately backed-up cleanup pass.

### Reason
A single persistent HUD prevents Tile Pool, Word Bar, STRIKE, health, potion, and
mobile presentation from drifting between stages. Existing `PersistentHUDRoot`
and `PersistentPlayerRoot` lifecycle code already provides the singleton and
scene-rebinding boundary needed to share the prefab safely.

### Consequences
- `SampleScene`, `Stage2Scene`, `Stage3Scene`, and `Stage4Scene` reference the
  canonical prefab; Tutorial also uses it through its persistent HUD bridge.
- The runtime singleton retains exactly one active HUD for the current run;
  newly loaded copies are rejected while scene-owned combat systems rebind.
- Stage 1/2 inactive legacy copies remain temporary technical debt, not active
  HUD owners.
- Isolated optimization/reference scenes may retain aligned copies without
  becoming production runtime authorities.
- YAML authoring checks do not replace Unity scene deserialization, Play Mode
  scene-transition tests, or physical Galaxy A05s validation.

---
