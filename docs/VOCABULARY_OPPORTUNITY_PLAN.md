# Vocabulary Opportunity Plan

**Status:** Planning only. No gameplay code or scene wiring is created by this document.  
**Purpose:** Convert the existing standalone minigames into a stage-connected vocabulary-practice loop without creating separate, one-off barrier systems for every stage.

## Design decision

Use **Vocabulary Opportunity** as the reusable system name. In player-facing UI,
it may be presented as **Vocabulary Strike**.

It is not a literal barrier in every encounter. It is a planned moment where the
player uses a taught target word to cause a meaningful combat or world effect.

```text
Teach
  -> Stage target words are presented with definition and context.
Combat
  -> A target-word opportunity saves and hides the player's Normal Combat
     pool, then presents a separate temporary shuffled challenge set.
  -> The target word triggers a configured gameplay effect.
Checkpoint
  -> A mandatory post-stage review asks which sentence correctly uses each
     stage target word.
Feedback
  -> Every answer receives immediate explanatory feedback and is recorded.
```

## Non-negotiable rules

1. A stage uses an expert-reviewed vocabulary profile before it is described as
   educationally structured.
2. Each target word has a word, definition, context sentence, assigned combat
   opportunity, and checkpoint item.
3. Target-word letters must be **guaranteed simultaneously available**. Raising
   their random spawn chance is not enough.
4. Normal valid words still work for normal combat before and after an
   opportunity.
5. A target word does not need to kill an enemy. It must create a meaningful,
   visible result such as breaking a shield, interrupting an attack, removing a
   debuff, opening a path, or exposing a weakness.
6. The post-stage checkpoint uses the stage's assigned words, not random global
   dictionary entries or fallback words without usable definitions.
7. Word Master and Word Excavation remain optional standalone practice unless a
   later design explicitly maps them to the stage vocabulary profile.
8. The in-game checkpoint is formative evidence. The study's vocabulary outcome
   still requires a defined external pre-assessment and post-assessment.
9. Assign no more than one target word to each enemy. Boss phases reuse the
   boss's assigned target word or return to ordinary valid-word combat; phases
   do not create additional target-word requirements.
10. A target challenge must never delete, reroll, replace, or otherwise mutate
    the player's saved Normal Combat tiles.
11. Target Challenge tiles are temporary encounter-provided tiles. They do not
    enter the normal world-pickup replacement flow.
12. When a target challenge ends, the player's Normal Combat pool must return
    with the same letters and relevant tile states it had before the challenge.
13. An active Word Bar selection must be safely returned to the Normal Combat
    pool before a target challenge snapshot is captured.

## Data required per stage

Use four target words per stage as the working content plan (20 words across
five stages), with one word assigned to each enemy. This total remains subject
to confirmation of the final enemy roster and English/education expert review.

```text
StageVocabularyProfile
  stage index
  target words[]

TargetWord
  word
  part of speech
  definition
  context sentence
  combat clue
  encounter / node assignment
  configured success effect
  contextual sentence-use review prompt
  sentence choices[]
  correct choice
  explanatory feedback
```

The expert must review the selected-word set, definitions, context sentences,
sentence-use items, and stage mapping. The Stage 1 implementation set below is
locked as the team's candidate set, but it must not be described as
expert-validated until the qualified reviewer has approved or revised it.

## Stage 1 candidate vocabulary and encounter map

These four words are the locked implementation candidates. Their definitions
are based on the local Open English WordNet 2025 dataset. Final player-facing
wording, examples, distractors, and review items remain subject to qualified
English/education expert validation.

| Encounter | Target word | Meaning focus | Opportunity timing and effect |
|---|---|---|---|
| Bramble Sprite | `MITIGATE` | Reduce the seriousness or harmful effect of something | Pre-combat gate: mitigate the fragment's harmful energy, collapse the barrier, awaken Bramble, then resume ordinary combat. |
| Stone Sentinel | `FRACTURE` | Break or crack something | Mid-combat armor opportunity: fracture reinforced stone armor/core, stagger the Sentinel, then resume ordinary combat. |
| Cursed Jester | `NULLIFY` | Make something ineffective by counteracting its effect | Reactive opportunity: nullify Taunt/slow or interrupt the configured special effect, then resume ordinary combat. |
| Hollow Knight boss | `LIBERATE` | Free someone or something from confinement or control | One scripted boss opportunity: liberate the fragment from the Knight's control, remove its empowerment or stagger it, then continue the same boss encounter. |

The player uses ordinary valid words outside these planned moments. Vocabulary
opportunities are brief, meaningful checkpoints rather than a permanent combat
pause.

## Stage 1 implementation checklist

Check a layer only after its implementation and required verification are
complete. Design documentation alone does not complete a runtime layer.

- [x] **1. Vocabulary data and mode contract**
   - Store the four Stage 1 records in one reusable profile.
   - Define `NormalCombat` and `TargetChallenge` behavior without changing
     current runtime tile behavior yet.
   - Do not hardcode words into enemy controllers.

- [x] **2. Reversible Normal Combat pool snapshot**
   - Return any active Word Bar selection safely.
   - Capture the exact Normal Combat letters and relevant tile states.
   - Hide/disable the saved pool without destroying it.
   - Prove that the same pool can be restored unchanged.
   - **Verification:** Unity Play Mode passed both the basic round trip and the
     active Word Bar case. Selected tiles returned safely before capture, all
     16 tiles were hidden, and the restored integrity signature matched the
     captured signature exactly.

- [x] **3. Temporary Target Challenge pool**
   - Create a shuffled temporary set containing the complete target-letter
     multiset plus configured distractors.
   - Visually distinguish temporary challenge tiles from player-owned tiles.
   - Do not spawn world replacement tiles from this temporary set.
   - **Verification:** Unity Play Mode showed 16 shuffled cyan-and-gold
     temporary tiles. The player manually selected the shuffled letters to form
     `MITIGATE`; ordinary STRIKE remained disabled; Normal Combat restoration
     was refused until the temporary pool was removed; removal created no world
     replacements; and the final Normal Combat signature matched the captured
     signature exactly.

- [ ] **4. Target submission and restoration**
   - Every dictionary-valid submission uses the normal attack animation and
     projectile, while selected temporary tiles remain frozen.
   - A valid non-target word is absorbed for zero damage on confirmed matching
     barrier impact, then returns to the temporary pool without consumption.
   - The exact target word resolves on matching barrier impact, removes the
     temporary set, restores the saved Normal Combat pool, and returns control
     to ordinary combat.
   - **Current status:** The impact-confirmed cast path, mid-flight input lock,
     zero-damage non-target absorption, unrelated-barrier rejection, temporary
     tile return, and exact restoration are implemented and compiling. Unity
     Play Mode verification is pending.

- [ ] **5. Bramble / `MITIGATE` adapter**
   - Migrate the existing prototype to the preserved-pool contract.
   - Collapse the barrier and awaken Bramble without mutating the saved pool.

- [ ] **6. Stone Sentinel / `FRACTURE` adapter**
   - Add or verify one reinforced armor/core state.
   - Target success fractures the state and visibly staggers/exposes the enemy.

- [ ] **7. Cursed Jester / `NULLIFY` adapter**
   - Connect target success to the existing Taunt/slow or chosen special-effect
     cancellation point.

- [ ] **8. Hollow Knight / `LIBERATE` adapter**
   - Add one scripted fragment-binding opportunity for the complete boss
     encounter.
   - Later boss phases do not add another target word.

- [ ] **9. Teach presentation**
   - Present the assigned word, part of speech, definition, and contextual
     sentence before its encounter opportunity.

- [ ] **10. Mandatory contextual Stage Vocabulary Review**
    - Ask one sentence-use question for each of the four Stage 1 words.
    - Give immediate explanatory feedback.
    - Do not require another spelling reconstruction after the stage.

- [ ] **11. Records and platform validation**
    - Store teach viewed, target-word success, review answer, attempts, and
      stage-review completion separately from ordinary word history.
    - Validate the complete flow on PC and an Android device.

## Next-week plan: July 13-19, 2026

### Monday - adviser validation and scope lock

- Present the revised learning loop: Teach -> Combat opportunity -> mandatory
  Spell It Out checkpoint -> review/retry.
- Ask approval for four expert-reviewed target words per stage and the external
  pre-/post-assessment procedure.
- Confirm the qualified English/education validator and review process.

**Acceptance evidence:** consultation notes recording the approved/revised
direction and named follow-up actions.

### Tuesday - content and technical design

- Draft the StageVocabularyProfile fields and one Stage 1 worksheet with four
  placeholder word slots.
- Map Stage 1 encounter effects using the table above.
- Define UI wireframe for Teach screen, Vocabulary Strike prompt, success
  feedback, and checkpoint result/review.
- Confirm how the existing current selectable tile pool will receive guaranteed
  letters without deleting player progress unexpectedly.

**Acceptance evidence:** reviewed Stage 1 vocabulary worksheet and technical
flow diagram.

### Wednesday - smallest Stage 1 proof

- Implement or prototype one Bramble Sprite opportunity only.
- Prove: clue appears, complete individual letter set is supplied, the exact
  word is accepted, the configured effect triggers, and normal combat resumes.

**Acceptance evidence:** short screen recording plus a test log.

### Thursday - reusable integration review

- Extract configuration so Stone Sentinel, Cursed Jester, and Hollow Knight can
  reuse the same controller with different effects.
- Do not build all effects until the Bramble proof passes.

**Acceptance evidence:** inspector/configuration plan showing no duplicated
manager architecture.

### Friday - checkpoint integration proof

- Pass the Stage 1 target-word list to Spell It Out.
- Confirm it no longer selects random length-based words for this stage.
- Add result/review behavior for missed words.

**Acceptance evidence:** test log showing the exact Stage 1 word list in the
checkpoint and correct/missed result output.

### Saturday - full Stage 1 loop test

- Test Teach -> Bramble opportunity -> normal combat -> remaining encounters
  planned -> checkpoint -> review/retry.
- Log every failure: missing letter, wrong clue, impossible target word, broken
  return flow, or unclear combat effect.

**Acceptance evidence:** Stage 1 loop test checklist and issue list.

### Sunday - plan the remaining stages from the proven core

- Assign effect categories for Stages 2-5 using the same reusable controller.
- Do not implement Stages 2-5 vocabulary barriers yet unless the Stage 1 loop
  has passed manual testing.

**Acceptance evidence:** five-stage effect matrix, ownership, and next sprint
cards.

## Definition of done for the Stage 1 vertical slice

- [ ] Stage 1 has an approved target-word profile.
- [ ] Teach screen presents all four words with meaning and context.
- [ ] Each target word has a planned opportunity across the Stage 1 encounters.
- [ ] At least the Bramble opportunity is fully playable and manually tested.
- [ ] The saved Normal Combat pool is restored unchanged after every target challenge.
- [ ] Exact target letters are available together in a temporary shuffled challenge set.
- [ ] Target-word success visibly affects the encounter.
- [ ] Normal word combat still works before and after the opportunity.
- [ ] The mandatory Stage Vocabulary Review uses four contextual sentence-use items.
- [ ] Every review answer receives immediate explanatory feedback.
- [ ] Results are recorded in a form usable for later evaluation.

## Risks to manage

- Do not call the system structured/progressive until content validation is
  complete.
- Do not let a clue, target word, or letter guarantee make combat trivial.
- Do not make an incorrect answer permanently block stage completion.
- Do not describe the candidate word list as expert-validated before review.
- Do not promise all five stage integrations before the Stage 1 loop passes.
