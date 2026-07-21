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
Enter Stage 1
-> Teach: word + part of speech + definition + context
-> Guided reconstruction from shuffled Tile Pool letters
-> Normal arena combat
-> MITIGATE breaks Bramble Sprite's vocabulary barrier
-> Other valid words still work, but deal reduced damage while the barrier is active
-> Stage cleared
-> Mandatory vocabulary question
-> Immediate explanatory feedback
-> Wrong first answer: reconstruct MITIGATE from shuffled tiles
-> Recorded review summary
-> Stage Selection Scene
```

## 1. Create the Prototype Data Asset

After Unity finishes compiling:

1. Open `SPELLSTRIKE > Vocabulary > Create Stage 1 MITIGATE Prototype Profile`.
2. Confirm that Unity creates and selects:
   `Assets/Data/Vocabulary/Stage1_Mitigate_Prototype.asset`.
3. Inspect the asset. Keep `Stage Number = 1` and the prototype word at index `0`.

The menu is safe to run more than once. If the asset already exists, it selects
the existing asset instead of overwriting it.

## 2. Add the Runtime Session

Create a scene object named `VocabularyPracticeSession` and add the component
with the same name.

- Leave `Persist Across Scenes` disabled for the first vertical-slice test.
- This records attempts in memory only. SQLite/JSON persistence is not part of
  this prototype layer.

## 3. Wire the Teach Activity

Create a Teach canvas panel and add `VocabularyTeachController` to a stable
scene object. Assign:

- `Profile`: `Stage1_Mitigate_Prototype`
- `Word Index`: `0`
- `Panel Root`: Teach panel root
- `Word Text`: large word display
- `Part Of Speech Text`: part-of-speech label
- `Definition Text`: meaning label
- `Context Text`: example-sentence label
- `Instruction Text`: player instruction label
- `Feedback Text`: immediate feedback label
- `Primary Button`: Practice/Continue button
- `Primary Button Label`: TMP label inside the primary button
- `Hint Button`: optional hint button
- `Tile Manager`: the existing Stage 1 `TileManager`
- `Attack Controller`: the existing player `AttackController`
- `Session`: `VocabularyPracticeSession`
- `Pause Gameplay While Teaching`: enabled
- `Require Guided Reconstruction`: enabled

Create a trigger object before the first combat encounter:

1. Add a trigger collider.
2. Add `VocabularyTeachTrigger`.
3. Assign the Teach controller.
4. Keep `Player Tag = Player` and `Trigger Only Once` enabled.

During Teach, the complete word is presented first. Pressing `PRACTICE` hides
the answer and makes the player rebuild it using the real Tile Pool. Required
letters are guaranteed but shuffled; the system does not display them in answer
order.

## 4. Wire Bramble Sprite's Vocabulary Barrier

Add `VocabularyBarrier` to the Bramble Sprite prefab root or the same ancestor
that owns `BrambleSpriteController`. Assign:

- `Profile`: `Stage1_Mitigate_Prototype`
- `Word Index`: `0`
- `Barrier Active`: enabled
- `Non Target Damage Multiplier`: `0.35`
- `Target Word Damage Multiplier`: `1.25`
- `Barrier Visual`: optional shield/VFX child
- `Combat Clue Text`: optional world-space or HUD clue
- `Tile Manager`: existing Stage 1 `TileManager`
- `Maximum Pool Tiles`: `16`
- `Guarantee Target Letters On Start`: enabled
- `Session`: `VocabularyPracticeSession`

Expected behavior:

- A valid word other than `MITIGATE` damages Bramble Sprite at 35% while the
  barrier is active.
- `MITIGATE` breaks the barrier and applies the configured target-word bonus.
- After the barrier breaks, all later valid words use normal combat damage.

Do not make `MITIGATE` the only valid attack. It is a guided retrieval
opportunity, not a hard spelling lock.

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
2. Walk through the Teach trigger.
3. Verify movement/combat is paused and the Attack button is locked.
4. Verify word, part of speech, definition, and example are visible.
5. Press `PRACTICE`; confirm the answer becomes blanks.
6. Confirm the Tile Pool contains the letters for `MITIGATE` in shuffled order.
7. Form a wrong eight-letter arrangement; confirm immediate reset/feedback.
8. Form `MITIGATE`; confirm Teach completes and gameplay resumes.
9. Fight Bramble Sprite using another valid word; confirm reduced damage.
10. Form `MITIGATE`; confirm the barrier visual deactivates and bonus damage is applied.
11. Finish Stage 1; confirm the review opens before scene transition.
12. Choose a wrong distractor; confirm the feedback explains both the distractor
    and `MITIGATE`, including a context sentence.
13. Press `RETRY MISSED WORD`; reconstruct `MITIGATE` from shuffled tiles.
14. Confirm the summary records first-attempt result, missed word, retry attempt,
    and combat retrieval.
15. Press `CONTINUE`; confirm `StageSelectScene` loads.
16. Repeat once with the correct first answer to test the no-retry path.
17. Repeat the full flow on an Android build before calling the prototype
    cross-platform ready.

## Acceptance Criteria

- Teach cannot be skipped by walking into combat after the trigger has opened.
- The target letters are available but never arranged as the answer.
- Non-target valid words remain usable in combat.
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

