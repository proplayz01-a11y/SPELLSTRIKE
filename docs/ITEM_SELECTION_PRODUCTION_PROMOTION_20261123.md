# SPELLSTRIKE — Item Selection Production Promotion

Date: 2026-11-23 (Philippines)

## Production status

The manually approved Item Selection UI has been **promoted in the Unity
project**. This does not represent a new Android APK or PC distribution build.

- Production scene: `Assets/Scenes/WizardsVault_ItemSelectionRevamp.unity`
- Production prefab: `Assets/Prefabs/UI/WizardsVault_ItemSelectionRevamp.prefab`
- Stage Select route: `StageSelectManager.itemSelectionSceneName =
  WizardsVault_ItemSelectionRevamp`
- Production scene is **enabled** in Unity's Editor Build Settings.
- Stage 1–4 scenes now use the production route. Stage 5 remains a non-build
  placeholder and retains the existing legacy fallback.
- Original legacy scene and prefab are retained without modification.
- The isolated test scene and test prefab are retained without modification.

## Approved visual and interaction behavior

- Library environment, parchment-and-gold frame, baked solid-gold title ribbon
- The **exact Stage Select animated library background** now runs behind the
  Item Selection UI: a cloned `HallOfChroniclesBackgroundCanvas` with the
  original `HallOfChroniclesAmbient` controller, 19 flame sprites, 20 glow
  sprites, and 39 animated bindings. Existing warm candle flicker, wick
  sway, local glow variation, and 36 soft dust motes are reused. The static
  `LibraryBackground_RevampTest` Image component is disabled (not deleted)
  in the production scene, so it cannot cover the animation. The separate
  animated background canvas renders at sorting order -50, beneath the UI.
  The Stage Select source scene and its art/systems are unchanged.
- The **exact scene-instance manual layout overrides** were preserved:
  Slot 1/2/3 occupied names use TMP font size 18 with anchored Y -98;
  item icons are 140x117 with anchored Y 1.4 / 0.6 / 0.1 respectively.
- Equipment frame, item icon and name grow together on hover.
- Empty slots display their numbered heading; occupied slots hide it.
  A production-only empty-heading positioning fix keeps all three headings
  inside the frame (anchored Y 50) without modifying occupied layouts.
- Four actual passive items in a three-visible-row scrolling list
- `StatePlate` appears for locked items only; owned/equipped items use
  plain-text labels while preserving equip and unequip actions.
- Button/card/slot pressed dim feedback with hover effects retained
- Windows: `Right-click to unequip`; Android/iOS:
  `Tap and hold to unequip` (0.6 seconds)
- Stage preview images: Stage 1 `stage_art`, Stage 2 `IS_Stage02Art`,
  Stage 3 `IS_Stage03Art`, Stage 4 existing
  `stage4_cursed_gothic`. Stage 4 sprite is assigned to the
  production prefab through the optional `cursedGothicPage` presenter field.

## Implementation and source-of-truth

The production scene and prefab were copied from the saved, approved test
scene and prefab using distinct asset GUIDs; **not rebuilt**, to protect
the manually adjusted scene-instance overrides.

Only the copied scene's prefab references were remapped (454 references)
to the copied production prefab. The asset retains the original prefab
internal file IDs and all 452 scene-instance override records.

The existing `ItemSelectionSceneController`, `PassiveItemInventory`,
`GameDatabaseManager` and `PotionInventory` still own gameplay state.
Presentation components do not independently persist or calculate equipped
items.

The production route is supplied by the existing serialized Stage Select
scene. `StageSelectManager.OpenItemSelectionForStage` now includes Stage 4,
while the unloaded Stage 5 retains the previous fallback.

## Checks completed

- Confirmed Stage Select serialized route is exactly
  `WizardsVault_ItemSelectionRevamp`.
- Confirmed the Stage Select scene's disk content differs from its
  pre-promotion backup **only in that route value**.
- Confirmed the new scene is registered and enabled in Build Settings;
  the old live scene remains registered for rollback.
- Validated the production scene's prefab reference resolves to the
  **production** prefab, not the test prefab.
- Verified all three manual occupied-slot icon positions and TMP font
  properties were preserved in the **production scene instance**.
- Verified the four stage preview sprites resolve through
  `WizardsVaultStagePagePresenter`.
- In Unity Play Mode confirmed: production scene loads, four passive rows,
  scrollbar binding, three unequip handlers, nine hover relays, eleven
  pressed-feedback controls, and platform hint are present.
- Verified and fixed empty-slot headings inside their frames; reloaded
  the saved production scene and confirmed all heading Y positions = 50.
- Unity compiled successfully. A separate Unity Remote device-check error
  may still be visible, unrelated to the Item Selection scripts.
- Reopened the saved production scene after transferring the animated library:
  `HallOfChroniclesAmbient.IsAnimating = true`, `LayerCount = 39`,
  `visibilityGuard = ItemSelectionUI_Prefab`, static image disabled.
  In Play Mode independently verified flame alpha, height, and rotation,
  localized glow alpha changing over successive frames, 13 runtime near-flame
  highlights, and 36 runtime dust Image objects. Game View confirms
  the animation renders behind the Item Selection UI without covering it.
  No new animation script or image generation was needed.

## Release qualification

This is a **project-side production promotion**. No new exported
PC/Android build, physical Android long-press test, Stage Select-to-stage
end-to-end device test, or full regression run was completed here.
Validate these on target devices before shipping a store release.

No Git commit or push was performed for this promotion.

## Safe rollback

To restore the previous stage entry routing:

1. In `Assets/Scenes/StageSelectScene.unity`, restore
   `itemSelectionSceneName: WizardsVault_ItemSelectionLive`.
2. In `Assets/Sprites/StageSelectAssets/StageSelectScript/StageSelectManager.cs`,
   change the Stage Index 4-inclusive condition from
   `stageIndex <= 3` back to `stageIndex <= 2` so Stage 4 uses the old
   Item Selection fallback.
3. Leave the old scene registered in Build Settings. Do not delete the
   new scene until after rollback verification.

Pre-promotion copies of the original route, build settings, and test
scene/prefab are in `Temp/ItemSelectionPromotionBackup_20261123/`
(local, outside `Assets` and outside version control).
The exact production scene prior to animated-library integration is also
backed up there as
`WizardsVault_ItemSelectionRevamp.before_candle_flicker.unity`.
