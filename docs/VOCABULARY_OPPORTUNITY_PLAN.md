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
  -> A target-word opportunity supplies a clue and makes the complete target
     letter set available in the current selectable tile pool.
  -> The target word triggers a configured gameplay effect.
Checkpoint
  -> The existing Spell It Out minigame assesses the same stage word set.
Review / retry
  -> Missed words are shown again and retried; the player is not deadlocked.
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

## Data required per stage

Start with five target words per stage (25 words across five stages), subject to
English/education expert review.

```text
StageVocabularyProfile
  stage index
  target words[]

TargetWord
  word
  definition
  context sentence
  combat clue
  encounter / node assignment
  configured success effect
  checkpoint prompt
```

The expert must review the selected-word set, definitions, context sentences,
and stage mapping. Do not choose final words merely because they fit an enemy
theme.

## Stage 1 design map (gameplay effects only)

Final target words are intentionally **TBD pending expert review**. The table
defines the effect category, not the final vocabulary content.

| Encounter | Vocabulary Opportunity effect | Notes |
|---|---|---|
| Bramble Sprite | Remove a vine/thorn shield or root bind; brief stagger | One target word; normal combat continues after success. |
| Stone Sentinel | Break the existing Guardian Shield | Align this with the current shield mechanic rather than create a duplicate shield system. |
| Cursed Jester | Dispel Taunt/Confetti confusion or interrupt its special attack | One target word; effect must be readable to the player. |
| Hollow Knight boss phase 1 | Expose armor / interrupt a guarded attack | One target word. |
| Hollow Knight boss phase 2 | Reveal or free the fragment seal | One target word; serves as the fifth Stage 1 opportunity. |

The player uses ordinary valid words outside these planned moments. Vocabulary
opportunities are brief, meaningful checkpoints rather than a permanent combat
pause.

## Reusable implementation layers

1. **Stage vocabulary data**
   - Create a data owner for the profile and target-word records.
   - Do not hardcode stage words into enemy controllers.

2. **Teach UI**
   - A lightweight Spellbook/lesson screen before a stage.
   - Shows word, definition, and context sentence; this is exposure, not the
     score-bearing assessment.

3. **Vocabulary Opportunity controller**
   - Receives one assigned target word and its clue.
   - Requests the complete letter multiset from the current tile system.
   - Detects successful submission of the assigned word.
   - Sends one configured success effect to the encounter owner.

4. **Encounter effect adapters**
   - Existing enemy/node systems own the gameplay effect.
   - Example: Stone Sentinel's existing shield owner receives a shield-break
     request. Do not create five bespoke vocabulary-barrier managers.

5. **Spell It Out checkpoint adapter**
   - Existing Spell It Out receives the StageVocabularyProfile instead of
     random length-filtered dictionary entries.
   - Records correct/missed words, attempts, and completion.

6. **Learning record storage**
   - Store teach viewed, target word submitted in combat, checkpoint accuracy,
     attempts, and completion by stage/word.
   - Keep this separate from ordinary word history so it is usable for the
     study and final evidence.

## Next-week plan: July 13-19, 2026

### Monday - adviser validation and scope lock

- Present the revised learning loop: Teach -> Combat opportunity -> mandatory
  Spell It Out checkpoint -> review/retry.
- Ask approval for five expert-reviewed target words per stage and the external
  pre-/post-assessment procedure.
- Confirm the qualified English/education validator and review process.

**Acceptance evidence:** consultation notes recording the approved/revised
direction and named follow-up actions.

### Tuesday - content and technical design

- Draft the StageVocabularyProfile fields and one Stage 1 worksheet with five
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
- [ ] Teach screen presents all five words with meaning and context.
- [ ] Each target word has a planned opportunity across the Stage 1 encounters.
- [ ] At least the Bramble opportunity is fully playable and manually tested.
- [ ] Exact target letters are available together when required.
- [ ] Target-word success visibly affects the encounter.
- [ ] Normal word combat still works before and after the opportunity.
- [ ] Spell It Out uses the Stage 1 word profile rather than random words.
- [ ] Missed checkpoint items are reviewed and retryable.
- [ ] Results are recorded in a form usable for later evaluation.

## Risks to manage

- Do not call the system structured/progressive until content validation is
  complete.
- Do not let a clue, target word, or letter guarantee make combat trivial.
- Do not make an incorrect answer permanently block stage completion.
- Do not write final word lists before the expert review.
- Do not promise all five stage integrations before the Stage 1 loop passes.
