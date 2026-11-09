# Stage 2-4 official combat HUD: Edit Mode visual parity

Date: 2026-10-21

Objective: make the actual Stage 2, Stage 3 and Stage 4 production scenes appear like the approved Stage 1 ornate HUD in Unity Edit Mode, with the same artwork placement in Play Mode. Source: Assets/Scenes/SampleScene.unity, Unity Android build target, 2400x1080 A05s Game View.

## Production changes

- Assets/Scenes/Stage2Scene/Stage2Scene.unity: approved complete ornate Word Bar, Tile Pool, Mage HP/EXP, STRIKE and authoring transforms; inactive non-encounter enemy HP preview.
- Assets/Prefabs/PersistentGameplayHUD.prefab: approved reusable artwork, player-health Canvas sizing, suppressed redundant legacy red player HP Graphics. No new combat UI managers or Sliders.
- Assets/Scenes/Stage3Scene/Stage3Scene.unity and Assets/Scenes/Stage4Scene/Stage4Scene.unity: scene-local visual transforms for their existing shared HUD prefab instances.
- Assets/Scripts/Tutorial/TutorialResponsiveLayout.cs: explicit production Stage 2-4 Edit Mode layout sync; retain approved ornate STRIKE scale in runtime when that sprite is in use.
- Assets/Editor/Stage234HudEditModeParityPromotion.cs, Stage234HudParityFineTune.cs and Stage2EnemyHudEditParity.cs: guarded, source-controlled one-shot visible Editor menu utilities.

## Verification

| Check | Outcome |
| --- | --- |
| Stage 1 authoritative scene bytes unchanged | PASS |
| Stage 2 ornate HUD in actual Unity Edit Mode | PASS |
| Stage 3 ornate HUD in actual Unity Edit Mode | PASS |
| Stage 4 ornate HUD in actual Unity Edit Mode | PASS |
| Stage 4 Play Mode with 16 live tiles and ornate HUD | PASS |
| Unique frame, backdrop, STRIKE and Mage skin per canonical authored HUD | PASS, serialized inspection |
| Existing scene GUIDs, HUD prefab GUID, Build Settings and save SHA | PASS, unchanged |
| Stage 2 and 3 Play Mode after final corrections | NOT TESTED |
| Physical Samsung A05s and FPS | NOT TESTED |
| Stage 4 Potion Bag click | KNOWN ISSUE, not fixed by artwork task |

The Stage 4 focused Play Mode test did not mutate the user's authoritative save. SHA-256 before and after: 2289E13A7B7F53D187743635F07089FA31894FB6156B29CB0B8913D424AB066D.

Before/after backups are under Library/GameplayHudEditModeParity_20261021, including OfficialParityBefore, ArtScaleFineTuneBefore and Stage2_BeforeEnemyBarDefaultHide.unity. Preserve these; the menu utilities reject blind repeat runs. No original Stage 1 gameplay, input, progression or other stage gameplay was rewritten.
