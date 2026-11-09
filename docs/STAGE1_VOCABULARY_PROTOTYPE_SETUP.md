# Stage 1 Vocabulary Prototype Setup

## Status and Boundary

The C# foundation for the one-word Stage 1 prototype is implemented and
compile-checked. It is intentionally opt-in: existing Stage 1 behavior does not
change until the components below are added and assigned in Unity.

Prototype word: `MITIGATE`

This content is a development placeholder. It must not be described as
expert-reviewed until an English or education expert has reviewed the word,
definition, context sentence, distractors, and assessment item.

## Intended Player Flow

```text
-> Bramble Sprite is visible but dormant beside the fragment
-> Enter the Node 1 shrine trigger
-> Save and hide the exact Normal Combat pool
-> Teach: MITIGATE + part of speech + definition + context
-> Click/tap anywhere
-> Create the shuffled temporary cyan-and-gold Target Challenge pool
-> Reveal the barrier and action-only objective
-> Valid non-target word: absorbed-hit feedback
-> Click/tap feedback, review Teach, then retry the same temporary pool
-> MITIGATE breaks only the barrier for zero HP damage
-> Restore the exact Normal Combat pool
-> Disturb Bramble Sprite and start full-health combat
-> Later valid words use normal combat damage
-> Stage cleared
-> Mandatory vocabulary question
-> Immediate explanatory feedback
-> Wrong first answer: reconstruct MITIGATE from shuffled tiles
-> Recorded review summary
-> Stage Selection Scene
```

## 1. Create the Prototype Data Asset

After Unity finishes compiling:

1. Open `SPELLSTRIKE > Vocabulary > Create or Refresh Stage 1 Candidate Profile`.
2. Confirm that Unity creates and selects:
   `Assets/Data/Vocabulary/Stage1_Vocabulary_Candidate.asset`.
3. Inspect the asset. Keep `Stage Number = 1` and `MITIGATE` at index `0`.

The menu is safe to run more than once. If the asset already exists, it selects
the existing asset instead of overwriting it.

## 2. Add the Runtime Session

Create a scene object named `VocabularyPracticeSession` and add the component
with the same name.

- Leave `Persist Across Scenes` disabled for the first vertical-slice test.
- This records attempts in memory only. SQLite/JSON persistence is not part of
  this prototype layer.

## 3. Wire Teach, Feedback, and the Reusable Encounter Flow

Create a Teach canvas panel and add `VocabularyTeachController` to a stable
active scene object. Do not place the controller under an inactive canvas root.
Assign:

- `Profile`: `Stage1_Vocabulary_Candidate`
- `Word Index`: `0`
- `Show On Start`: disabled
- `Require Guided Reconstruction`: disabled
- `Panel Root`: Teach panel root
- `Word Text`: large word display
- `Part Of Speech Text`: part-of-speech label
- `Definition Text`: meaning label
- `Context Text`: example-sentence label
- `Instruction Text`: player instruction label
- `Feedback Text`: immediate feedback label
- `Primary Button`: may remain unassigned for this flow
- `Hint Button`: unassigned
- `Click Anywhere Button`: transparent full-panel Button
- `Tile Manager`: the existing Stage 1 `TileManager`
- `Attack Controller`: the existing player `AttackController`
- `Session`: `VocabularyPracticeSession`
- `Pause Gameplay While Teaching`: enabled

Keep `VocabularyPracticeCanvas` active. Its Teach and Feedback panel roots may
start inactive. Disable `Raycast Target` on decorative images and TMP labels;
only the transparent full-panel Buttons should receive clicks/taps.

On `Node1_Controller`, add `VocabularyEncounterFlowController` and assign:

- `Teach Controller`: the configured MITIGATE Teach controller
- `Vocabulary Barrier`: Bramble Sprite's barrier
- `Tile Manager`: Stage 1 `TileManager`
- `Attack Controller`: the player `AttackController`
- `Movement Controller`: the player's `MovementofPlayer`
- `Feedback Panel`: absorbed-hit feedback panel root
- `Feedback Text`: TMP label inside that panel
- `Feedback Continue Button`: transparent full-panel Button
- `Maximum Pool Tiles`: `16`

Assign this component to `Node1Controller > Vocabulary Encounter Flow`. If both
components are on `Node1_Controller`, the reference can also resolve
automatically at runtime.

Do not add a separate `VocabularyTeachTrigger` for Node 1. The existing physical
shrine trigger calls `Node1Controller.TriggerEncounter()`, which now starts
Teach first.

## 4. Wire Bramble Sprite's Vocabulary Barrier

Add `VocabularyBarrier` to the Bramble Sprite prefab root or the same ancestor
that owns `BrambleSpriteController`. Assign:

- `Profile`: `Stage1_Vocabulary_Candidate`
- `Word Index`: `0`
- `Barrier Active`: enabled
- `Block Non Target Damage While Active`: enabled
- `Non Target Damage Multiplier`: `0.35`
- `Target Word Damage Multiplier`: `1.25`
- `Barrier Visual`: shield/VFX child; required for Layer 2 visual acceptance
- `Combat Clue Text`: world-space or HUD clue; required for Layer 2 visual
  acceptance
- `Reveal Challenge On Start`: disabled
- `Tile Manager`: existing Stage 1 `TileManager`
- `Maximum Pool Tiles`: `16`
- `Guarantee Target Letters On Start`: disabled; the encounter flow creates a
  separate temporary Target Challenge pool instead
- `Session`: `VocabularyPracticeSession`

Expected behavior:

- Before the shrine encounter, Bramble Sprite remains visible and dormant near
  the locked fragment but is hidden from normal enemy targeting.
- Entering `Node1_ShrineTrigger` saves/hides Normal Combat and opens Teach.
- The barrier and temporary challenge tiles remain hidden until Teach is
  dismissed.
- A valid word other than `MITIGATE` is absorbed and deals zero damage while
  the barrier is active, then feedback and Teach review appear.
- `MITIGATE` breaks the barrier for zero HP damage, restores Normal Combat, and
  starts Bramble Sprite's full-health disturbance/combat flow.
- After the barrier breaks, all later valid words use normal combat damage.

`MITIGATE` is the required retrieval key for beginning this encounter. It is
not the only valid word for the rest of the fight.

On `Node1_Controller`, verify:

- `Require Barrier Break Before Combat`: enabled
- `Keep Bramble Visible While Dormant`: enabled
- `Hide Dormant Bramble From Targeting`: enabled
- `Block Non Target Damage While Barrier Active`: enabled
- `Vocabulary Barrier`: may be assigned directly; otherwise it is resolved from
  the Bramble Sprite object at runtime
- `Fragment Feeding Visual`: optional looping VFX that is active while Bramble
  feeds from the fragment and stops when `MITIGATE` breaks the barrier
- `On Barrier Challenge Started`: optional shield/clue/SFX presentation
- `On Bramble Disturbed`: optional wake-up animation, reaction VFX, and SFX

Layer 1/2 wiring rule: keep the shield child and clue object assigned even if
they are inactive in Edit Mode. `VocabularyBarrier.HideChallenge()` hides them
before the shrine, and `BeginChallenge()` reveals them on shrine entry.

Position Bramble Sprite manually beside the fragment and use its dormant/idle
animation before combat. The code gates targeting and AI; it does not choose the
final feeding pose or create the parasite VFX.

## 5. Wire the Mandatory Stage Review

Create a review root under the Stage 1 canvas and add
`StageVocabularyReviewController`. Assign:

- `Profile`: `Stage1_Mitigate_Prototype`
- `Prototype Word Index`: `0`
- `Review Panel`: the complete review root
- `Question Text`: question label
- `Answer Buttons`: four buttons
- `Answer Labels`: the four TMP labels matching those buttons by index
- `Feedback Text`: immediate explanatory feedback label
- `Retry Instruction Text`: retry definition/instruction label
- `Continue Button`: shared Retry/Summary/Continue button
- `Continue Button Label`: its TMP label
- `Hint Button`: optional retry hint button
- `Summary Panel`: summary-only child content
- `Summary Text`: recorded-result label
- `Tile Manager`: existing Stage 1 `TileManager`
- `Attack Controller`: existing player `AttackController`
- `Session`: `VocabularyPracticeSession`

Hierarchy constraint: keep the shared Continue button inside `Review Panel` but
outside `Summary Panel`. `Summary Panel` begins inactive.

On the existing `StageCompleteUI`:

- Enable `Require Vocabulary Review`.
- Assign the `StageVocabularyReviewController`.
- Leave `Show Legacy Stage Complete Panel After Review` disabled to follow the
  manuscript flow directly from review summary to `StageSelectScene`.
- Enable it only if the team deliberately wants the old Stage Complete panel as
  an extra step.

## 6. Manual Unity Test Flow

1. Enter Play Mode and start Stage 1.
2. Before entering the shrine, confirm Bramble Sprite is visible, idle beside
   the fragment, and cannot be selected as an attack target.
3. Record the current 16-letter Normal Combat pool.
4. Enter `Node1_ShrineTrigger`.
5. Confirm Normal Combat tiles disappear, movement/STRIKE lock, and Teach shows
   MITIGATE, verb, definition, contextual example, and click-anywhere copy.
6. Confirm the barrier and cyan-and-gold tiles are still hidden during Teach.
7. Click/tap anywhere.
8. Confirm Teach closes, the barrier/action objective appears, and exactly 16
   shuffled temporary challenge tiles appear.
9. Form and STRIKE `ATE`.
10. Confirm the projectile hits the barrier, Bramble loses zero health, the
    temporary letters return, and absorbed-hit feedback names `ATE` without
    displaying MITIGATE.
11. Click/tap feedback; confirm Teach reopens as review.
12. Dismiss Teach; confirm the same temporary challenge resumes.
13. Form and STRIKE `MITIGATE`.
14. Confirm the barrier breaks, Bramble loses zero HP, temporary tiles disappear,
    and the original 16-letter Normal Combat pool restores exactly.
15. Confirm Bramble plays `BeingHit`, remains at full health, then enters
    Approach and ordinary combat.
16. Cast another valid word; confirm normal combat damage is used.
17. Defeat Bramble; confirm the fragment unlocks only after its death.
18. Finish Stage 1; confirm the review opens before scene transition.
19. Choose a wrong distractor; confirm the feedback explains both the distractor
    and `MITIGATE`, including a context sentence.
20. Press `RETRY MISSED WORD`; reconstruct `MITIGATE` from shuffled tiles.
21. Confirm the summary records first-attempt result, missed word, retry attempt,
    and combat retrieval.
22. Press `CONTINUE`; confirm `StageSelectScene` loads.
23. Repeat once with the correct first answer to test the no-retry path.
24. Repeat the full flow on an Android build before calling the prototype
    cross-platform ready.

## Acceptance Criteria

- Teach cannot be skipped by walking into combat after the trigger has opened.
- Target Challenge tiles do not exist until Teach is dismissed.
- The target letters are available but never arranged as the answer.
- The exact saved Normal Combat pool returns after target success.
- Non-target valid words cannot bypass the barrier before combat begins.
- After the barrier breaks, non-target valid words remain usable in combat.
- The target word produces an observable combat consequence.
- Review occurs after stage completion and before Stage Selection.
- Feedback is immediate, explanatory, contextual, and actionable.
- A wrong answer requires corrective retrieval before completion.
- Attempt records exist for Teach, Combat, first review answer, and retry.
- No claim of vocabulary improvement is made from this prototype alone.

## Known Limitations

- Only one prototype word is configured.
- Content has not yet been expert-validated.
- Attempt records are runtime-only and are lost when the session object is
  destroyed or the application closes.
- No automated Unity Play Mode test or Android device test has been completed.
- Final UI art, animation, sound, and accessibility treatment are not included.
