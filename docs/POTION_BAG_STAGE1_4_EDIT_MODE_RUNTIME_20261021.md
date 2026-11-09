# Stage 1–4 official Potion Bag Edit Mode and runtime integration

Date: 2026-10-21.

## Files changed

- Assets/Scenes/SampleScene.unity and Assets/Scenes/Stage2Scene/Stage2Scene.unity: authored Bag UI on existing persistent combat HUD Canvas.
- Assets/Prefabs/PersistentGameplayHUD.prefab: the same authored Bag on the existing Stage 3/4 shared HUD.
- Assets/Scripts/PotionSidebarUI.cs: runtime rebinding of authored Bag, existing three potion slots and three passive slots, with recovery of optional Stage 2 passive tooltip handlers. The original fallback BuildUI runs only if there is no saved Bag.
- Assets/Scenes/Stage4Scene/Stage4Scene.unity: enabled StandaloneInputModule on existing EventSystem and disabled, but retained, old InputSystemUIInputModule.
- Assets/Editor/OfficialPotionBagEditModePromotion.cs, Stage4BagEventSystemParity.cs, Stage4PotionBagInputFix.cs: guarded visible Editor authoring, reversible Stage4 input change and read-only diagnosis.

## Checks

| Check | Result |
| --- | --- |
| Stage1 scene Bag serialized | PASS |
| Stage2 scene Bag serialized and visible in Edit Mode | PASS |
| Shared Stage3/4 HUD Bag serialized and visible in Stage4 Edit Mode | PASS |
| Stage4 actual Play Mode Bag opens and closes via mouse | PASS |
| Stage2 actual Play Mode Bag opens and shows potion counts/passive equipment | PASS |
| Stage2 duplicate legacy bag | No duplicate visible in actual Play Mode |
| Stage1 fresh runtime click after authoring | NOT TESTED |
| Stage3 fresh runtime click after authoring | NOT TESTED |
| Real Samsung Galaxy A05s touch/joystick/Bag | NOT TESTED |
| Potion consumption, progression, boss gameplay | NOT TESTED / out of scope |

Stage4 previous input action GUID actually resolves to the Unity Input System packaged DefaultInputActions. It is not a missing GUID. A read-only runtime raycast found the Bag Button active, interactable, and hit by GraphicRaycaster. However mouse clicks did not open the Bag until the Stage4 EventSystem used the same StandaloneInputModule already proven on Stage2/3. The original module is still serialized disabled for rollback.

The authoritative JSON save SHA-256 was unchanged throughout actual Stage 2/4 tests: 2289E13A7B7F53D187743635F07089FA31894FB6156B29CB0B8913D424AB066D. No potion was consumed. Play Mode stopped.

Exact backup locations: Library/GameplayHudEditModeParity_20261021/OfficialBagAuthoringBefore/ and Library/GameplayHudEditModeParity_20261021/Stage4_before_StandaloneUIInput_20261021.unity. Do not overwrite older snapshots. Stage1/2 scene GUIDs, Stage3/4 scene GUIDs, Build Settings and source prefabs unrelated to HUD were not changed in this task.
