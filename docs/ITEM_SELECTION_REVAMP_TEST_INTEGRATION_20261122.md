# SPELLSTRIKE — Isolated Item Selection Revamp Test

## Status

The redesigned Item Selection UI is integrated in a **test-only** scene and
prefab. **Stage Select still routes to the existing production
`WizardsVault_ItemSelectionLive` scene.** Do not promote this test scene until
the player has approved the rendered layout and full transition QA is complete.

## Open in Unity

- Test scene: `Assets/Scenes/WizardsVault_ItemSelectionRevamp_Test.unity`
- Test prefab: `Assets/Prefabs/UI/WizardsVault_ItemSelectionRevamp_Test.prefab`
- Separated art: `Assets/Sprites/ItemSelectionRevamp/`
- Hover masks: `Assets/Sprites/ItemSelectionRevamp/Hover/`
- Editor rebuild menu: **Tools > SPELLSTRIKE > Item Selection > Build Revamp TEST Scene**
- Hover-only installer: **Tools > SPELLSTRIKE > Item Selection > Apply TEST Mini-Game Hover**

The build menu reauthors **only the test prefab and test scene**, then reapplies
hover effects. Any hand-made changes to the test prefab may be replaced by a
subsequent rebuild. Do not run the original, production-focused hover installer.

## Integrated UI and bindings

- Stage Select library background, referencing the existing `library_base_2400x1080` sprite
- Separated parchment, frame, title ribbon, dividers and decorative footer
- Three simplified parchment-and-gold equipment slots; slot number, equipped
  name and icon are rendered dynamically
- When occupied, each slot hides its `SLOT N` heading and divider, displays
  a 140x117 passive icon above its short name, and scales its frame, icon and
  name together under the existing 4% hover animation. Empty slots retain
  their original numbered heading and `Empty` label.
- Editable slot composition: each `EquippedSlot1/2/3` contains an
  `EmptyLayout` and an `EquippedLayout`. The latter contains **separate**
  Unity `Image` (`ItemIcon`) and TextMeshPro (`ItemName`) children. These
  are manually resizable/repositionable per slot via `RectTransform` and
  independently adjustable in the Inspector. The runtime presenter updates
  only the name, icon sprite, count, and layout visibility. It does **not**
  reset the icon's width/height or position, the label's position, or its
  font size. Font auto-sizing is disabled for `ItemName`.
- Four actual passive catalog items, with only three visible rows in a masked
  vertical `ScrollRect`; the fourth is accessible by wheel, drag or arrows
- Separate `Sky Sword`, `The Great Attractor`, `Purifying Pearl` and
  `Tidecarved Shell` icon sprites
- Owned/locked/equipped status badges and labels from actual inventory state
- Health, Purify and Power-Up potion counts from `PotionInventory`
- Back / Continue actions remain attached through
  `ItemSelectionSceneController`
- All visible text uses the installed Cinzel / Alegreya TMP font assets
- The original `WizardsVaultStagePagePresenter` selects the already approved
  Stage 1 image and the clean Stage 2/3 images
- Reused `ModeSelectGoldBorderHover`: nine targets (3 equipment slots,
  4 passive rows, 2 navigation buttons); slots grow 4%, passive icons 6%
- Platform-aware unequip: Windows/PC right-clicks an equipped slot;
  Android/iOS touch holds the slot for 0.6 seconds. The label below
  the subtitle automatically changes from `Right-click to unequip` to
  `Tap and hold to unequip` according to `Application.platform`.
  Early touch release, exit, or drag cancels the long-press. The interaction
  invokes the relevant passive-row Button so the existing controller
  handles save and UI refresh; no independent inventory state is introduced.
- Test-only pressed-state feedback on 11 controls (Back, Continue, four
  passive rows, three equipment slots, and the scroll arrows): pressing
  applies a translucent dark tint that follows the **original sprite's
  transparent shape**, then smoothly fades out on release, exit, or drag.
  PC equipment slots respond to right-click presses only; Android/iOS
  slots respond to touch. No transform, icon size, or TMP changes are made.
- A test-only scale component fits the approved 1448x1086 composition inside
  the canvas without touching the legacy canvas/controller

The existing `ItemSelectionSceneController` still owns passive equipping,
unlocks, save data, and navigation. New test-only components render the
information without adding another source of truth.

## Unity runtime QA completed

- Opened the actual test scene in the Unity Editor and viewed the Game View
  against the library background at 2400x1080
- Verified all 11 controller lookup paths, 37 TMP nodes, 4 passive badge
  presenters, one stage-art presenter and all 9 hover relays
- Verified 338px-high passive viewport, 444px-high scroll content, 4 rows,
  scrollbar binding and visibility of the fourth row when scrolled
- Verified stage-art switching: `stage_art`, `IS_Stage02Art`,
  `IS_Stage03Art`
- Dispatched test pointer enter/exit on Sky Sword; observed a 1.060 icon
  scale and return to 1.000
- Separated occupied-slot icons and labels after visual QA
- Verified all three occupied slots (Sky Sword, Attractor, Pearl) show large
  icons without heading clutter; parent-child transform nesting makes the
  icon and text grow with the gold frame (1.040x on hover, 1.000x on exit)
- In Unity Play Mode, manually changed the second slot's icon size to
  164x136 and its TMP label to font size 33 and anchored Y -48. Confirmed
  the values persisted after runtime refreshes. Exited Play Mode to
  discard these temporary QA adjustments.
- Verified platform label selection for `WindowsPlayer`, `WindowsEditor`,
  `Android` and `IPhonePlayer`. Tested PC pointer input in Play Mode
  with the actual passive-row button temporarily replaced by an in-memory
  callback: left click 0 calls, right click 1 call, owned/equipped data
  unchanged. Android physical-device gesture still requires device QA.
- Tested the pressed tint in Unity Play Mode: passive card and Continue
  button immediately darken to 0.168 alpha, reach 0.240 while held, then
  return to 0.000 on release. On PC, slot left-click leaves the tint at
  zero; right-click activates it. Press-feedback overlays are
  non-raycastable and use their source art sprites so the frame corners
  remain transparent.
- Prevented the legacy 64% equipped-row fade from dimming new parchment copy
- Latest compiled Unity Console check returned no errors

## Manually adjusting each slot in Unity

1. In Edit Mode, open `WizardsVault_ItemSelectionRevamp_Test.unity`.
2. Expand **Canvas > ItemSelectionUI_Prefab > WizardVaultContent >
   Body > ItemPage > EquippedSlots > EquippedSlot1** (or 2 or 3).
3. Expand **EquippedLayout**. Select **ItemIcon** to adjust the
   `RectTransform` width, height, X/Y position, or the Image properties.
   Select **ItemName** to adjust TMP font size, alignment, color, and
   `RectTransform` position/width. They remain separate, editable objects.
4. Use the `ItemSelectionRevampTestDisplay` component on **ItemPage** for
   **Preview 3 Equipped Items** and **Preview Empty Slots** buttons in its
   Inspector. These are editor-only visual previews; they do not change the
   inventory or equipping status.
5. Save the modified scene with Ctrl+S. For reusable base adjustments,
   double-click `WizardsVault_ItemSelectionRevamp_Test.prefab` to enter
   Prefab Mode and make the changes there. Existing scene-instance overrides
   take precedence over prefab defaults, so clear/apply those overrides
   deliberately when necessary.

**Never adjust while in Play Mode and expect persistence**, because Play
Mode changes are temporary. Also note that the test-scene **Build Revamp
TEST Scene** menu command reconstructs the prefab from defaults and will
overwrite manually tuned test-prefab layout values. Do not run a full
rebuild after editing the slot proportions.

## Before production promotion

1. Player reviews the new scene in **Edit and Play Mode** and approves the
   current layout and animation.
2. Test normal item equip/unequip, locking, potion counts, stage transitions,
   buttons and scroll wheel/drag/touch on the target device.
3. Test PC 1920x1080 and Android landscape resolutions, including long passive
   names and three occupied slots.
4. Stage 4 preview is **not yet separately mapped** in the existing
   `WizardsVaultStagePagePresenter` (which handles indexes 0-2). Verify or
   implement this before redirecting Stage 4 through the test scene.
5. The test scene currently uses the actual game's `PassiveItemInventory`
   and save path. Test equip/unequip on an expendable/backup save rather than
   assuming actions are sandboxed.
6. After approval, update the **Stage Select scene's**
   `itemSelectionSceneName` from `WizardsVault_ItemSelectionLive` to the
   promoted scene name, and ensure the scene is present in Build Settings.
   **Neither step has been performed here.**

## Production safety

Neither the original live scene, original `WizardsVault_Layered_Working.prefab`,
nor any inventory/controller or Stage Select scripts were edited by this task.
The test scene and prefab use new Unity asset GUIDs. No Git commit or push was
performed.
