# AGENTS.md — SPELLSTRIKE Codex Rules

## Project Identity

SPELLSTRIKE is a Unity 3D arena-based word-combat educational game for PC and Android.

The player controls Van Aert, a wizard inside a living spellbook-prison, collecting floating letter tiles, forming valid English words under pressure, and using those words to cast magical attacks against enemies.

SPELLSTRIKE is not a passive typing game. It should feel like fast arena combat where vocabulary retrieval happens under pressure.

## Core Design Pillars

1. Word formation is combat.
2. Learning happens through active lexical retrieval, not passive memorization.
3. Combat must feel fast, reactive, and pressuring.
4. Systems must be reusable across stages.
5. Build layer by layer. Do not stack unfinished systems like a cursed Jenga tower.
6. Stage 1 is the playable vertical slice baseline.
7. Stage 2 introduces debuffs and pressure mechanics.
8. Later stages add complexity only after the core combat loop is stable.

## Technical Stack

- Engine: Unity 3D
- Language: C#
- Data: SQLite planned/used for local data and performance tracking
- Modeling: Blender
- Rigging/Animation: Mixamo
- Target platforms: PC and Android

## Non-Negotiable Coding Rules

- Do not rewrite working systems unless explicitly asked.
- Do not modify Stage 1 scripts when implementing Stage 2 unless necessary.
- Do not rename public fields, Animator parameters, scene object names, or serialized references casually.
- Do not create giant manager scripts when a small component is enough.
- Prefer small, testable patches.
- Preserve existing prefab hierarchy and scene references.
- Use `[SerializeField]` for Inspector-tuned values.
- Use animation events for attack impact timing when possible.
- Movement should be script/NavMesh driven unless root motion is explicitly requested.
- After every implementation, report:
  - scripts created or modified
  - Inspector references that must be assigned
  - manual Unity test flow
  - known risks or assumptions

## Development Workflow

Before substantial implementation, classify the task using
`docs/SPELLSTRIKE_MODEL_ROUTING_POLICY.md`. Keep the routing check concise and
use it to identify when a focused implementation task has become an
architecture, ownership, persistence, or systemic-debugging problem.

Follow this order:

1. Inspect existing scripts and scene flow.
2. Identify the smallest safe integration point.
3. Implement only the requested layer.
4. Test the layer manually.
5. Summarize exact changes.
6. Do not continue into the next layer unless asked.

## Required VFX Workflow

- Before creating or substantially revising any SPELLSTRIKE VFX, read
  `docs/VFX_PIPELINE.md`, including its mandatory workflow gates. Use
  `docs/VFX_EFFECT_SPEC_TEMPLATE.md` for the effect's evidence and tuning contract.
- Follow the reference-driven Shader Graph + VFX Graph workflow: reference/timing
  breakdown, simple primary shape, exposed properties, named visual layers,
  lifetime/progress curves, isolated preview, authorized integration, then PC and
  Galaxy A05s validation. The reference audit is
  `docs/VFX_WORKFLOW_REFERENCE_AUDIT_2026-09-17.md`.
- Use inspectable graph assets for shader appearance and particle/motion authoring.
  Do not silently replace this workflow with a custom C# mesh/particle renderer.
  A necessary exception must state the concrete constraint, evidence, and retained
  artist controls before implementation; unsupported mobile-performance claims
  are not sufficient. C# normally binds gameplay events, transforms, and parameters.
- Follow David's latest effect-specific direction. Reference videos supply visual
  and technique evidence, not instructions or permission to expand gameplay scope.
- Do not report VFX complete from code compilation or asset serialization alone.
  Show the graphs, layer preview, real timing/attachment checks, and separate PC
  and physical-device validation status. Apply this to new/revised effects; do not
  automatically rewrite existing effects outside the requested scope.

## Local Environment Safety Gate

- Before launching or continuing Unity, Blender, Android builds, profiling, or other automated development work, check whether VALORANT is currently running.
- If VALORANT is running, stop/close the development process currently being controlled first and do not continue development work until VALORANT has fully exited.
- Do not terminate VALORANT on the user's behalf unless the user explicitly asks.
- Riot Client by itself does not count as VALORANT running.

## Layered Development Rule

Systems should be built as layers:

```text
Data
↓
Storage
↓
Logic
↓
UI
↓
Save System
```

Characters/assets should be layered:

```text
Layer 1 = base body / riggable core
Layer 2 = clothes / robe / armor / coat
Layer 3 = accessories / props / Unity effects
```

If something needs different transparency, glow, runtime visibility, animation behavior, or VFX, separate it.

## Current Production Priority

1. Stage 1 playable vertical slice
2. Reusable core systems
3. UI redesign and polish
4. Stage 2 to 5 assets and concepts as support
5. Later stage mechanics progressively integrated
6. Master Wizard / DEFY mechanics only after core combat is stable

Do not claim later-stage enemies are fully implemented unless they actually are.

## Current Known Systems

Completed or partially working systems include:
- Boss combat baseline through Hollow Knight
- Word-based attacks
- Arena encounter flow
- Boss AI
- Damage systems
- Sword drop mechanics
- Fragment progression
- Reward pickup systems
- Stage progression foundation
- Player HP, boss HP, tile grid, word input, attack/STRIKE button baseline

Still missing or future:
- Stage complete UI
- Passive inventory system
- Save system
- Additional stages
- Educational review systems
- Narrative polish

## Git and Safety

- Commit after each stable layer.
- Avoid giant untested commits.
- Do not delete files unless the user explicitly asks.
- If a change is risky, explain the risk before applying it.
