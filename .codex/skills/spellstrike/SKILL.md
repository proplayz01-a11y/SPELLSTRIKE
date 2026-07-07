---
name: spellstrike
description: Use for SPELLSTRIKE Unity project work in this repository, including feature implementation, audits, enemy or stage work, documentation maintenance, and safe project-memory-driven planning. Trigger when the user mentions SPELLSTRIKE, Stage 1, Stage 2, nodes, enemies, tile debuffs, combat, Unity scene wiring, or asks for implementation inside this repo.
---

# SPELLSTRIKE

Use this skill as the project workflow adapter. The canonical source of truth remains the repo docs; do not duplicate their full contents here.

## Start Every Task

Before auditing, planning, or editing, read:

- `AGENTS.md`
- `docs/PROJECT_STATUS.md`
- `docs/ARCHITECTURE.md`
- `docs/DECISIONS.md`
- `docs/TECH_DEBT.md`
- `docs/CHANGELOG.md`

Use these documents to determine current priority, architecture, known risks, and required documentation updates.

## Default Workflow

1. Inspect existing scripts, prefabs, and scene wiring before changing anything.
2. Identify the smallest safe integration point.
3. Implement only the requested layer.
4. Avoid unrelated refactors, renames, prefab hierarchy changes, and Stage 1 changes unless required.
5. Verify with targeted code checks and Unity manual-test instructions.
6. Update project memory docs when the task changes project state.
7. Stop after the requested layer unless the user explicitly asks to continue.

## Architecture Rules

- Preserve Stage 1 as the playable vertical slice.
- Treat Stage 2 as the current reusable architecture proving ground.
- Prefer small `MonoBehaviour` components with Inspector references.
- Use existing managers instead of creating duplicates.
- Keep movement script/NavMesh driven unless the user explicitly requests root motion.
- Preserve public fields, Animator parameters, scene object names, and serialized references.
- Use animation events for attack impact timing when practical.
- Use `[SerializeField]` for Inspector-tuned values when adding new private fields.

## Current Focus

Stage 2: Sunken Seas.

Near-term work is:

- Verify Stage 2 tile spawning after `TileManager.currentStage` correction.
- Finish Corsair Phantom, especially Tile Cracking.
- Validate Stage 2 node progression.
- Implement Tide Wraith and later Drowned Captain only after the previous layers are stable.

Do not claim later-stage enemies or mechanics are complete unless the implementation proves it.

## Enemy Implementation Workflow

Build enemies in layers:

1. Scene/prefab foundation and references.
2. Activation through `StartBattle()`.
3. Movement and player detection.
4. Basic attack and damage.
5. Hit reaction and death callback.
6. Stage-specific ability or debuff.
7. Balance, VFX, and polish.

For Stage 2 tile debuffs, extend `TileDebuffManager`; do not create another tile debuff manager.

## Documentation Maintenance

After implementation, decide which docs changed:

- Architecture changes: update `docs/ARCHITECTURE.md`.
- Major decisions: update `docs/DECISIONS.md`.
- Progress changes: update `docs/PROJECT_STATUS.md`.
- Newly discovered debt: append to `docs/TECH_DEBT.md`.
- Completed work: update `docs/CHANGELOG.md`.

For audit-only tasks, do not edit docs unless the user asks.

## Required Final Report After Implementation

End implementation tasks with:

```md
## Files Changed

## Unity Setup Required

## Documentation Updated

## Technical Debt Added

## Testing Checklist

## Risks
```

If no Unity editor test was run, say so and provide a manual Unity checklist.
