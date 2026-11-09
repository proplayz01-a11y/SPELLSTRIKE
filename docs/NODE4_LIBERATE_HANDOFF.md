# SPELLSTRIKE Stage 1 Node 4 — LIBERATE Implementation Handoff

> **Purpose**
>
> This is a self-contained handoff for another ChatGPT/Codex session that will
> help implement Stage 1 Node 4 without redesigning or damaging the existing
> SPELLSTRIKE systems.
>
> **Current status (2026-08-01):** Layer 1's trigger-side encounter backbone
> is implemented and compiles. Scene/Inspector wiring, Unity Play Mode
> validation, the Hollow Knight disturbance handoff, binding presentation, and
> final VFX remain pending.

---

## Copy-Paste Prompt for a New Chat

```text
We are continuing the SPELLSTRIKE Unity project at:
C:\Users\David\PROTOTYPE\SPELLSTRIKE

Read these files before changing anything:
1. AGENTS.md
2. .codex/skills/spellstrike/SKILL.md
3. docs/PROJECT_STATUS.md
4. docs/ARCHITECTURE.md
5. docs/DECISIONS.md
6. docs/TECH_DEBT.md
7. docs/CHANGELOG.md
8. docs/NODE4_LIBERATE_HANDOFF.md

Use Adviser Mode. Inspect before editing. Do not use Unity MCP. Preserve all
working Node 1, Node 2, Node 3, Hollow Knight combat, fragment, sword reward,
and Stage Complete behavior.

Implement Node 4 layer by layer. Start only with the smallest safe LIBERATE
pre-combat-gate backbone described in NODE4_LIBERATE_HANDOFF.md. Do not add
final VFX, redesign the boss AI, or continue into later layers without our
approval. After coding, compile and give us exact Inspector wiring and a manual
Unity Play Mode test checklist.
```

---

# 1. Locked Design Verdict

Node 4 will use `LIBERATE` as a **pre-combat vocabulary gate**.

The player is not damaging Hollow Knight with `LIBERATE`. The player is
breaking the magical binding that controls or restrains Hollow Knight.

The successful target-word impact must:

- break the binding;
- deal **zero HP damage** to Hollow Knight;
- restore the exact saved Normal Combat Tile Pool;
- trigger Hollow Knight's disturbance/hit reaction;
- then start the already working normal boss battle.

This is consistent with the working Stage 1 vocabulary architecture:

- Node 1: `MITIGATE`
- Node 2: `FRACTURE`
- Node 3: `NULLIFY`
- Node 4: `LIBERATE`

Do not add another target word to a second boss phase.

---

# 2. Locked Runtime Flow

```text
Player completes Node 3
→ player enters the Hollow Knight arena
→ boss arena closes
→ Hollow Knight is enabled but remains dormant
→ Hollow Knight cannot begin normal attacks yet
→ LIBERATE Teach panel appears
→ player reads the word, part of speech, definition, and contextual sentence
→ player clicks/taps anywhere to continue
→ the saved Normal Combat Tile Pool remains preserved and hidden
→ temporary cyan-and-gold Target Challenge tiles appear
→ player forms words from the temporary tiles

If the submitted word is invalid:
→ existing invalid-word behavior applies
→ challenge continues

If the submitted word is valid but is not LIBERATE:
→ spell reaches the binding
→ binding absorbs the spell
→ Hollow Knight receives 0 damage
→ temporary challenge tiles are not consumed permanently
→ explanatory feedback appears
→ Teach panel is shown again for review
→ player retries using the same Target Challenge system

If the submitted word is LIBERATE:
→ projectile hits the active binding/barrier target
→ binding breaks
→ Hollow Knight receives 0 damage
→ temporary Target Challenge pool is removed
→ exact saved Normal Combat pool is restored
→ Hollow Knight performs a BeingHit/disturbance reaction
→ existing Hollow Knight normal boss battle starts

Player defeats Hollow Knight
→ existing fragment unlock remains unchanged
→ existing sword reward remains unchanged
→ existing fragment collection and Stage Complete flow remain unchanged
```

Important technical clarification:

`VocabularyEncounterFlowController` currently saves and hides the Normal
Combat pool before it displays the Teach panel. Preserve that verified
transaction order even if the player-facing flow describes the Teach panel
first. Do not rewrite the reusable transaction merely to change invisible
internal timing.

---

# 3. Approved Player-Facing Content

## Teach Panel

Target word:

```text
LIBERATE
```

Part of speech:

```text
Verb
```

Working definition:

```text
To set someone or something free from control, confinement, or restraint.
```

Working contextual sentence:

```text
Van Aert used the spell to liberate the knight from the dark binding.
```

Instruction:

```text
Click anywhere to continue.
```

These are implementation candidates. They must still receive qualified English
or education expert validation before final research use.

## Active Challenge Instruction

Do not display the answer again after the Teach panel closes.

Use:

```text
BREAK THE BINDING
Form the target word, then STRIKE.
```

## Valid Non-Target Feedback

Recommended three-line structure:

```text
SPELL ABSORBED

The binding absorbed WORD.

WORD is valid, but it is not the target word.

Click anywhere to review.
```

`WORD` must be replaced dynamically with the submitted valid English word.

---

# 4. Existing Architecture That Must Be Reused

## Reusable Educational Flow

File:

```text
Assets/Scripts/VocabularyPractice/VocabularyEncounterFlowController.cs
```

Already supports:

- Normal Combat snapshot/hide;
- Teach presentation;
- temporary Target Challenge tiles;
- exact-word checking;
- valid non-target feedback;
- Teach review and retry;
- confirmed projectile-impact completion;
- temporary-pool cleanup;
- exact Normal Combat restoration;
- input restoration;
- completion and failure events.

Do not duplicate this system.

## Reusable Binding/Barrier Resolution

File:

```text
Assets/Scripts/VocabularyPractice/VocabularyBarrier.cs
```

Already supports:

- target word from a `StageVocabularyProfile`;
- zero-damage valid non-target absorption;
- zero-damage exact-target break;
- challenge visual reveal/hide;
- action-only combat instruction;
- barrier-break event.

Use this as the logical binding seal. The final visual may be different from
the Node 1 sphere.

## Vocabulary Data

Expected profile:

```text
Assets/Data/Vocabulary/Stage1_Vocabulary_Candidate.asset
```

Expected Node 4 record:

```text
Word index: 3
Word: LIBERATE
Encounter owner: Hollow Knight
```

Before wiring, verify the actual asset record and do not assume its index solely
from this document.

## Hollow Knight Arena Trigger

File:

```text
Assets/Scripts/EnemyScripts/HollowKnightTriggerController.cs
```

Current behavior:

```text
StartBossEncounter()
→ ConfigureTileSpawner()
→ EnableArenaBoundary()
→ EnableAndStartBoss()
→ hollowKnight.StartBattle()
```

This immediate `StartBattle()` call is the smallest integration point that must
change. The arena should still close and the TileSpawner should still be
configured, but the boss must not begin combat until LIBERATE completes.

## Hollow Knight Controller

File:

```text
Assets/Scripts/EnemyScripts/HollowKnightController.cs
```

Current verified facts:

- `battleStarted` gates the normal AI loop.
- `StartBattle()` enables the existing battle behavior.
- `TriggerBeingHit()` currently returns immediately when the battle has not
  started.
- `OnBeingHitFinished()` enters the existing hit-recovery routine.
- death already calls the existing Stage 1 boss reward/progression controller.

The implementation needs a small, controlled disturbance-to-battle handoff.
Do not rewrite Hollow Knight's AI.

## Existing Reward Flow

File:

```text
Assets/Scripts/EnemyScripts/Stage1BossController.cs
```

Preserve:

- Hollow Knight death callback;
- boss fragment unlock;
- sword reward;
- fragment collection;
- arena-boundary release;
- Stage Complete presentation.

Node 4 LIBERATE must end at the existing boss-combat entry point. It must not
replace the reward system.

---

# 5. Required Layer Order

Only complete and test one layer before continuing.

## Layer 1 — Functional Encounter Backbone

**Code status:** Implemented and compiled on 2026-08-01; Unity scene wiring and
Play Mode validation are still pending.

Goal:

- arena closes normally;
- Hollow Knight is active but dormant;
- reusable LIBERATE encounter flow begins;
- normal boss combat begins only after `EncounterCompleted`;
- flow failure does not permanently soft-lock the stage.

Likely smallest changes:

1. Extend `HollowKnightTriggerEncounter` with serialized references to:
   - `VocabularyEncounterFlowController`;
   - optionally the Node 4 `VocabularyBarrier` for validation/presentation.
2. Subscribe safely to:
   - `EncounterCompleted`;
   - `EncounterFailed`.
3. Replace immediate boss `StartBattle()` with:
   - enable dormant boss;
   - begin the LIBERATE flow.
4. On successful flow completion:
   - trigger a controlled disturbance;
   - transition into the existing `StartBattle()` behavior.
5. On unrecoverable flow failure:
   - restore input/pool through the reusable controller;
   - use a documented fallback that starts the existing boss encounter rather
     than trapping the player inside a closed arena.

Do not add final art in Layer 1.

## Layer 2 — Hollow Knight Disturbance Handoff

Goal:

```text
LIBERATE impact
→ zero damage
→ BeingHit reaction
→ BeingHit completes
→ normal combat starts
```

Implement the smallest safe method, such as a dedicated
`StartBattleDisturbed()` or an equivalent narrowly scoped handoff.

Requirements:

- no Hollow Knight HP loss;
- no attack during the BeingHit reaction;
- no permanent animator lock;
- no second call that resets a battle already in progress;
- clear logs confirming the sequence.

Do not globally remove the `battleStarted` protection from
`TriggerBeingHit()` without controlling the pre-combat case.

## Layer 3 — Scene and Inspector Wiring

Expected Node 4 objects:

```text
NODE 4
├── Hollow Knight
├── HollowKnight_TriggerEncounter
├── HollowKnight_ArenaCenter
├── HollowKnight_ShrineCenter
├── HollowKnight_ArenaBoundary
├── LIBERATE_EncounterFlow
├── LIBERATE_Binding
│   ├── BindingVisual
│   └── VocabularyBarrier
└── LIBERATE_VocabularyCanvas
    ├── TeachPanel
    ├── CombatInstructionPanel
    └── FeedbackPanel
```

Names may differ in the actual scene. Inspect first and preserve working object
names and references.

## Layer 4 — Functional Binding Visual

Use a simple placeholder first.

Approved functional visual direction:

- the fragment remains embedded in Hollow Knight's chest;
- a dark magical seal or restraint indicates that the fragment is bound;
- `LIBERATE` targets the binding, not Hollow Knight's HP;
- the binding visual disappears only after confirmed exact-target impact.

Do not make the first functional version dependent on complex custom shaders.

## Layer 5 — Teach and Feedback Presentation

Reuse the established Stage 1 visual language:

- dark navy/black translucent panel;
- gold headings and target word;
- readable white definition;
- cyan contextual sentence;
- click/tap-anywhere button covering only the intended panel;
- nonessential images must have `Raycast Target` disabled.

Validate on PC and Android aspect ratios later.

## Layer 6 — VFX and SFX Polish

Deferred until the functional encounter passes.

Possible final visual:

- fragment embedded in chest;
- dark rune rings;
- crossed magical bands;
- restrained dark particles;
- seal flashes/cracks on valid non-target impact;
- seal collapses on `LIBERATE`;
- Hollow Knight disturbance animation;
- dark binding SFX followed by a release burst.

Do not block core implementation on these effects.

---

# 6. Inspector Wiring Checklist

The exact fields may change after the Layer 1 patch, but the intended references
are:

## HollowKnight_TriggerEncounter

- `Progression Manager` → existing Stage 1 progression manager
- `Hollow Knight` → existing Hollow Knight controller
- `Boss Arena Center` → existing Node 4 arena center
- `Boss Shrine Center` → existing Node 4 shrine center
- `Boss Arena Boundary` → existing Hollow Knight arena boundary
- `Vocabulary Encounter Flow` → Node 4 LIBERATE flow controller

## LIBERATE_EncounterFlow

- `Teach Controller` → LIBERATE Teach controller
- `Vocabulary Barrier` → LIBERATE binding/barrier
- `Tile Manager` → existing Stage 1 `TileManager`
- `Attack Controller` → Van Aert's existing `AttackController`
- `Movement Controller` → Van Aert's existing movement component
- feedback panel references → Node 4 feedback UI
- maximum pool tiles → `16`, unless the verified profile/system requires
  otherwise

## LIBERATE Teach Controller

- profile → Stage 1 candidate vocabulary profile
- word index → verify actual `LIBERATE` index, expected `3`
- panel and TMP references → Node 4 Teach panel
- click-anywhere button → transparent full-panel button
- TileManager, AttackController, and session → existing Stage 1 references

## LIBERATE Binding

- profile → same Stage 1 profile
- word index → same verified `LIBERATE` index
- barrier/binding visual → Node 4 binding visual
- combat instruction panel → Node 4 action-only instruction

The Teach word and barrier word must match. The reusable encounter flow refuses
to start when they do not match.

---

# 7. Mandatory Manual Test Checklist

## Entry and Dormancy

- [ ] Complete Node 3.
- [ ] Enter Node 4.
- [ ] Confirm the arena boundary activates.
- [ ] Confirm the TileSpawner uses the boss arena settings.
- [ ] Confirm Hollow Knight is visible/active.
- [ ] Confirm Hollow Knight does not chase, attack, or begin normal AI.
- [ ] Confirm the player cannot bypass the vocabulary gate.

## Teach

- [ ] Confirm LIBERATE Teach panel appears once.
- [ ] Confirm word, part of speech, definition, and context are correct.
- [ ] Confirm movement and STRIKE are locked during Teach.
- [ ] Click/tap anywhere.
- [ ] Confirm Teach closes.
- [ ] Confirm the action instruction does not reveal `LIBERATE` again.

## Temporary Pool

- [ ] Record the exact Normal Combat Tile Pool before entering Node 4.
- [ ] Confirm it is saved and hidden.
- [ ] Confirm 16 temporary cyan-and-gold challenge tiles appear.
- [ ] Confirm they contain all letters required for `LIBERATE`.
- [ ] Confirm the target letters are shuffled.
- [ ] Confirm normal world pickups cannot corrupt the saved pool.

## Valid Non-Target

- [ ] Form a valid English word other than `LIBERATE`.
- [ ] STRIKE.
- [ ] Confirm the projectile reaches the binding.
- [ ] Confirm Hollow Knight loses 0 HP.
- [ ] Confirm Hollow Knight does not start normal combat.
- [ ] Confirm feedback dynamically shows the submitted word.
- [ ] Confirm clicking/tapping feedback returns to Teach review.
- [ ] Confirm retry resumes the same safe Target Challenge.

## Exact Target

- [ ] Form `LIBERATE`.
- [ ] STRIKE.
- [ ] Confirm the projectile hits the active Node 4 binding.
- [ ] Confirm the binding breaks.
- [ ] Confirm Hollow Knight loses 0 HP.
- [ ] Confirm temporary tiles are removed without creating replacements.
- [ ] Confirm the exact saved Normal Combat pool is restored.
- [ ] Compare the before/after pool signatures.
- [ ] Confirm Hollow Knight performs the intended disturbance reaction.
- [ ] Confirm normal boss combat begins after the reaction.

## Boss and Rewards

- [ ] Defeat Hollow Knight normally.
- [ ] Confirm the existing death flow still works.
- [ ] Confirm the fragment unlocks.
- [ ] Confirm the sword reward still works.
- [ ] Confirm collecting the fragment releases the arena boundary.
- [ ] Confirm Stage Complete still appears.
- [ ] Confirm there is no second target word or second LIBERATE requirement.

## Failure Cases

- [ ] Missing Teach reference produces a clear error and no corrupted pool.
- [ ] Missing barrier reference produces a clear error and no corrupted pool.
- [ ] Missing target enemy does not consume the saved Normal Combat pool.
- [ ] Re-entering the trigger does not start a duplicate encounter.
- [ ] Repeated `EncounterCompleted` callbacks do not start the boss twice.
- [ ] Disabling/re-enabling the flow does not accumulate duplicate event
  subscriptions.

---

# 8. Non-Negotiable Guardrails

- Do not use Unity MCP in this repository.
- Do not rewrite `TileManager`.
- Do not create a second vocabulary encounter manager.
- Do not mutate the saved Normal Combat pool to inject `LIBERATE`.
- Do not consume temporary challenge tiles permanently after a wrong target.
- Do not damage Hollow Knight when the binding is struck.
- Do not modify the already working Hollow Knight death/reward flow.
- Do not add a second boss target word.
- Do not rename public fields, Animator parameters, or working scene objects
  casually.
- Do not modify Nodes 1–3 unless a demonstrated shared-system bug requires it.
- Do not claim Node 4 is complete until Unity Play Mode passes the full
  checklist.
- Do not claim that the game improves vocabulary based solely on this feature.
  It is a vocabulary-practice and in-game review mechanism.

---

# 9. Known Documentation Mismatch

`docs/DECISIONS.md`, decision `DEC-016`, currently describes LIBERATE as:

```text
one scripted fragment-liberation event during the boss encounter
```

The newly approved flow makes LIBERATE a **pre-combat binding-release gate**.

Do not silently treat these as identical. After the implementation is accepted
and tested:

1. amend `DEC-016`;
2. update `docs/ARCHITECTURE.md`, where LIBERATE is currently classified as
   `ScriptedBossState`;
3. update `docs/PROJECT_STATUS.md`;
4. add the completed layer and verification result to `docs/CHANGELOG.md`;
5. add any discovered failure risk to `docs/TECH_DEBT.md`.

Until then, this handoff records the approved implementation direction while
the canonical documents continue to describe the last verified project state.

---

# 10. Required Report from the Implementing AI

After each layer, require this exact report:

```md
## Verdict

## Files Changed

## What the Layer Now Does

## Unity Setup Required

## Manual Testing Checklist

## Compilation Result

## Known Risks or Assumptions

## Documentation Updated

## Recommended Next Layer
```

If Unity Play Mode was not run, the AI must say:

```text
Compiled/static-checked only. Unity Play Mode validation is still pending.
```

---

# Final Adviser Note

The correct engineering move is not to invent a new Node 4 system. Node 4 is a
new configuration and handoff around the already verified reversible
vocabulary transaction.

The high-risk areas are:

1. starting Hollow Knight combat too early;
2. failing to restore the exact Normal Combat pool;
3. applying HP damage on the LIBERATE impact;
4. duplicating event subscriptions;
5. breaking the existing death, fragment, or sword-reward flow.

Solve those risks first. Art and VFX come afterward.
