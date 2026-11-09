# YOU DIED Prototype — Scope and QA

Project: SPELLSTRIKE (Unity 6.3 LTS), 2026-10-17.

## Prototype contract

- Minimal runtime UI only. Does not alter production scenes/prefabs, unlock
  stages, award points, alter vocabulary progress or call StageCompleteUI.
- Existing `PlayerHealth.Die()` is the source of death. Movement stops and
  the existing `isDead` animation trigger fires before the timed overlay.
- `PlayerDeathPrototypeUI` pauses timescale **after** a configurable
  1.8 second unscaled grace delay so the death animation may play.
- The overlay displays stage name, **GameManager.score** (no fabricated
  points), actual `AttackController.WordSubmitted` count, longest submitted
  word length, and time elapsed since entering the active combat scene.
- Return button restores timescale, unlocks attack input, restores health,
  resets GameManager to Idle, and opens `StageSelectScene` via existing
  `SceneManager.LoadScene` routing.
- The popup uses the active scene EventSystem, blocks background HUD input,
  and renders a minimal dark/navy panel with thin gold framing.
- The optional **Editor-only** `F9` shortcut previews this same UI from
  SampleScene, Stage2Scene, or Stage3Scene without reducing health.

## Files touched

- `Assets/Scripts/PlayerHealth.cs` (preserves previous health/Animator edits,
  adds runtime prototype component and one death notification)
- `Assets/Scripts/PlayerDeathPrototypeUI.cs` (new, self-contained UI)
- `Assets/Scripts/Debug/TileGenerationDebugOverlay.cs` (targeted `Hide()`
  so the temporary DDOL word-generation text cannot remain over Stage Select)
- This QA note and canonical project status/changelog/technical debt.

## Verification and limitations

- Confirmed Unity project identity and preserved pre-existing SampleScene,
  StageSelectScene and player health edits.
- Unity C# Tundra compilation succeeded for the new script.
- Actual Stage 1 Play Mode at 2400x1080 visually displayed the YOU DIED
  panel, Score 0, Word Cast 0, Longest Word NONE and live time elapsed.
- Initial click test exposed a missing UI Image raycast target and prompted a
  targeted fix. A second 2400x1080 visual pass verified corrected title spacing
  and the actual button click. Unity logged
  `[Death UI Prototype] Returning to Stage Select. No stage completion or review triggered.`
  and `Loaded scene 'Assets/Scenes/StageSelectScene.unity'`. Game View then
  displayed the Stage Select interface. This confirms button-to-scene routing,
  not an end-to-end replay or a persistent-save audit.
- A persistent `TileGenerationDebugOverlayCanvas` was visible above the Stage
  Select book after the successful return. Targeted cleanup now destroys it
  when the death screen appears, with normal regeneration on subsequent
  combat submissions. Cleanup has compiled but needs a follow-up visible
  after-return regression check.
- **Not yet a production art pass**; no Codex PNG art or other scene assets
  required. The actual physical death path/animation event, Stage 2/3 and
  Galaxy A05s are not signed off by the Editor-only F9 preview.
- GameManager currently has no combat `AddScore` callers: the zero score is
  real, not evidence that score points are functional.

## Manual regression

1. Open any of SampleScene/Stage2Scene/Stage3Scene in Unity, enter Play.
2. Press F9 in a focused Game View; inspect popup and click RETURN TO STAGE
   SELECT. Confirm restored Time.timeScale and unlocked stage state.
3. Separately test actual HP=0 (existing H debug key or normal enemy damage),
   verify animation plays before popup, and stage selection remains unchanged.
4. Reenter the same stage: health/movement/tile combat must work normally.
5. Validate on a real Android device prior to final production promotion.
