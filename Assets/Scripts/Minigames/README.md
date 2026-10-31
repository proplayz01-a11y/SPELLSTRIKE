# SPELLSTRIKE Minigames Architecture

## Compatibility-first architecture (Phase 2)

`MinigamesSceneController.cs` is still the original Unity `MonoBehaviour` with
its original `.meta` GUID and serialized fields. All existing scene components
and Inspector references remain valid. Its methods are grouped into **partial
class** files while individual gameplay rules are extracted into plain C#
classes. Gameplay state and transitions now live in independent session models.
Scene-facing view construction still uses partial MonoBehaviour files to avoid
breaking existing serialized UI references; the pure models do not require a
Unity scene.

| Location | Responsibility |
| --- | --- |
| `MinigamesSceneController.cs` | Serialized configuration, state, lifecycle, editor validation |
| `Core/MinigamesSceneController.Navigation.cs` | Mode switching, scene navigation, round dispatch |
| `Core/MinigamesSceneController.TileInput.cs` | Shared clickable letter input and selection |
| `Core/MinigamesSceneController.Dictionary.cs` | Shared word selection and fallback lookup |
| `Modes/MinigamesSceneController.WordMaster.cs` | Word Master flow, board, hints, keyboard |
| `Modes/MinigamesSceneController.SpellItOut.cs` | Spell It Out flow, vocabulary selection, definitions |
| `Modes/MinigamesSceneController.WordExcavation.cs` | Word Excavation flow, timer and reward UI |
| `UI/MinigamesSceneController.EditableUI.cs` | Shared Unity UI creation, caching and layout |
| `Modes/WordMasterGuessRules.cs` | Pure duplicate-letter-aware guess evaluation |
| `Modes/WordExcavationRules.cs` | Pure letter inventory, score and time-bonus rules |
| `Modes/WordMasterSession.cs` | Target, locked letters, guesses, turns, win/loss attempt validation |
| `Modes/SpellItOutSession.cs` | Timed word rounds and correct/missed history |
| `Modes/WordExcavationSession.cs` | Source inventory, valid unique finds, score and time bonus |
| `Core/MinigameTileSelection.cs` | Source letters and selected tile indices |

## Guardrails

- Keep the original MonoBehaviour and GUID until all five minigame scenes are
  deliberately migrated and individually regression-tested.
- UI prefab/hierarchy names are an existing compatibility contract.
- Preserve the main-menu scene name (`Main Menu Scene`) and scene build indices.
- Test no-dictionary fallback, pause/resume, loss/win, repeated letters, results,
  and navigation when making future changes.
- Changes to gameplay rules should be tested without a Unity canvas when possible.
- Current `OnValidate` editor UI refresh can mark a scene dirty; avoid unrelated
  scene saves during QA.

## Verification at refactor

- Unity script import and Assembly-CSharp recompilation succeeded.
- The original controller's **190 declared methods** were accounted for across
  the partial files with no missing or added controller methods.
- 11 in-editor checks passed for rule evaluation, repeated-letter handling,
  Excavation scoring/time bonuses, and Main Menu/build-index preservation.
- **Not yet validated:** complete Play Mode smoke test of all five scenes,
  full minigame UI redesign, or Android performance.

## Separation boundary

The `MinigamesSceneController` owns Unity lifecycle, scene routing, widget
references and feedback. It delegates round decisions to `WordMasterSession`,
`SpellItOutSession`, and `WordExcavationSession`. Existing private names in
the controller now forward to these models for scene compatibility.

The remaining partial mode files contain board rendering, dictionary candidate
selection and view synchronization, not the source of truth for session state.
The dedicated dictionary loading/definition curation can be extracted in a
later safe iteration without changing gameplay semantics.

## Phase 2 regression verification

- Standalone `WordMasterSession`, `SpellItOutSession`,
  `WordExcavationSession` and `MinigameTileSelection` compile in Unity.
- **28 in-editor assertions passed**, including Word Master five-turn win/loss,
  dictionary validation, locked hints and repeated guesses; Spell It Out
  timeout/correct/missed history; Excavation duplicate/inventory/reward/timer
  behavior; and unchanged Main Menu/Build Settings.
- The original scene-facing MonoBehaviour and its public serialized
  configuration remain in place; no minigame scene assets were rewritten.
- Full Play Mode navigation, visual acceptance and Android profiling remain
  explicitly scheduled for Phase 3, not inferred from unit tests.
