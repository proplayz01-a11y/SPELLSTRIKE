# SPELLSTRIKE Minigames Architecture

## Compatibility-first refactor

`MinigamesSceneController.cs` is still the original Unity `MonoBehaviour` with
its original `.meta` GUID and serialized fields. All existing scene components
and Inspector references remain valid. Its methods are grouped into **partial
class** files while individual gameplay rules are extracted into plain C#
classes. This is an intermediate architecture, not a claim that every mode
already has an independent controller.

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
