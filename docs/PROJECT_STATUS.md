# SPELLSTRIKE Project Status

## Stage 3 Node 2 Character / Empty Quiver — 2026-10-02

Created a separate textured desert-archer asset from the exact supplied screenshot
source, with all seven arrow components removed and a real hollow quiver. Blender
and GLB outputs contain 13,409 active triangles; the cavity is approximately
31.2 cm deep. Export reimport and packed texture checks pass. Character rigging,
animation, Unity integration, and PC/A05s validation remain pending. See
`docs/STAGE3_NODE2_DESERT_ARCHER_EMPTY_QUIVER.md`.

## Stage 3 Node 2 Weapon Asset Revision — 2026-10-02

Authored `ArtSource/Stage3/Node2/S3N2_DesertWeapons_v013.blend` from the preserved
v012 source: corrected arrow passage/nock/string, refined silhouette and surface
parts, and added an isolated Blender draw/release preview. Fresh counts are 2,758
bow triangles and 460 arrow triangles. Offline draw and 121-sample release checks
found no arrow/bow surface intersections (string contact excluded). Final atlas,
enemy hand fit, export/integration, and PC/A05s validation remain pending. See
`docs/STAGE3_NODE2_WEAPONS_V013_REVISION_REPORT.md`; this does not mark the enemy
or Stage 3 gameplay complete.

> **Purpose**
>
> This file tells Codex and the team what is currently done, what is in progress, and what should be built next.
>
> Update this after every major development session.

---

# Current Development Focus

## Main Priority

**Stage 2: Sunken Seas**

Current goal:

1. Stabilize Stage 2 node flow.
2. Finish Corsair Phantom.
3. Implement Tide Wraith.
4. Implement Drowned Captain.
5. Verify Stage 2 debuff systems.

---

# Stage Progress

## Stage 1 — Enchanted Kingdom

**Status:** Mostly playable vertical slice

### Enemies

- [x] Bramble Sprite
- [x] Stone Sentinel
- [x] Cursed Jester
- [x] Hollow Knight Boss

### Systems

- [x] Basic combat
- [x] Word tile system
- [x] Enemy health
- [x] Boss health
- [x] Fragment pickup
- [x] Sword reward pickup
- [x] Node progression
- [x] Basic Stage Complete UI
- [x] One-word vocabulary-practice C# foundation
- [x] Submitted-word transport from attack input to confirmed projectile impact
- [x] Optional target-word combat barrier
- [x] Teach, review, explanatory feedback, retry, and runtime record components
- [ ] Stage 1 Teach/Barrier/Review scene and Inspector wiring
- [ ] Unity Play Mode validation of the full educational loop
- [ ] Android device validation of the educational loop
- [ ] Expert validation of vocabulary content and assessment item
- [ ] Persistent educational result storage
- [ ] Final polish
- [ ] Architecture cleanup

### Notes

Stage 1 should remain stable. Avoid refactoring unless necessary. The vocabulary
prototype is opt-in until its Unity wiring is complete; do not report it as a
finished educational system or as evidence of vocabulary improvement.

---

## Stage 2 — Sunken Seas

**Status:** In progress

### Enemies

#### Barnacle Husk

- [x] Basic AI
- [x] Melee behavior
- [x] Water Burst
- [x] Tile Locking
- [x] Death callback
- [x] Potion drop
- [x] Node 1 attack-refill TileSpawner scene object
- [ ] Balance pass

#### Corsair Phantom

- [x] Model/assets present
- [x] Controller exists
- [x] StartBattle flow
- [x] BeingHit / Death animation trigger flow
- [x] Ghost Glide
- [x] Piercing Scream damage / cone hit flow
- [x] Phantom Shot / ability foundation
- [x] Phantom Shot charge VFX activation
- [x] Player projectile targeting / damage scene tag
- [x] Player projectile damage / shared health bar sync
- [x] Tile Cracking integration
- [x] Node 2 death/progression callback
- [x] BeingHit / Death animation events
- [x] Node 2 attack-refill TileSpawner arena references
- [x] Node 2 circular arena boundary wiring
- [ ] Final balancing
- [ ] Final VFX polish

#### Tide Wraith

- [ ] Not started
- [ ] Stun
- [ ] Knockback
- [ ] Ranged pressure behavior

#### Drowned Captain

- [ ] Not started
- [ ] Boss AI
- [ ] Combined Stage 2 debuff mechanics
- [ ] Fragment/reward flow

### Stage 2 Systems

- [x] TileState system
- [x] TileDebuffManager
- [x] StageProgressionManager
- [x] StageNodeTrigger / StageNodeController
- [x] Node 1 to Node 2 encounter wiring
- [x] Stage 2 shared TileSpawner for attack-refill world tiles
- [x] Reusable circular arena boundary component
- [ ] Full node chain validation
- [ ] Stage 2 boss completion flow
- [ ] Stage 2 reward flow

---

# Core Systems

## Combat

- [x] Word submission
- [x] STRIKE button validation
- [x] Homing projectile
- [x] Damage scaling
- [x] Enemy damage receiving
- [ ] Standardized enemy damage interface

## Tile / Word System

- [x] Tile selection
- [x] Word bar
- [x] Dictionary validation
- [x] Tile refill
- [x] Tile states
- [ ] Stage-specific tile weighting verification

## Player

- [x] Movement
- [x] Health
- [x] Attack animation event
- [x] Potions
- [ ] Movement script consolidation

## Items / Rewards

- [x] Potion system
- [x] Passive item concept
- [x] Hollow Knight's Blade reward
- [ ] Full passive item database
- [ ] Full inventory persistence

## Save / Persistence

- [x] Basic persistence exists
- [ ] Unified save system
- [ ] Replace split PlayerPrefs / SQLite / JSON behavior if needed

---

# Immediate Next Recommended Tasks

1. Wire the one-word vocabulary prototype in the Stage 1 scene using `STAGE1_VOCABULARY_PROTOTYPE_SETUP.md`.
2. Unity-test both correct-first-attempt and wrong-answer/retry educational flows.
3. Build and test the same Stage 1 flow on an Android device.
4. Resume Stage 2 progression validation from Barnacle Husk through Corsair Phantom.
5. Expand the vocabulary prototype only after the Stage 1 loop passes those tests.

---

# Session Update Template

Use this format after each Codex development session:

```md
## YYYY-MM-DD Session

### Completed
- 

### Changed Files
- 

### Unity Setup Needed
- 

### Bugs Found
- 

### Next Task
- 
```

## Combat HUD panel fill opacity set to alpha 215 - 2026-11-05

Set the Word Bar `WordBar_OrnateBackdrop` and Tile Pool `TilePoolPanelDesign (1)` Image alpha to 215/255 (`0.84313726`, approximately 84.3%) while keeping RGB tint white. Updated both serialized `PersistentHUDRoot` controls in the canonical `PersistentGameplayHUD.prefab` so `Awake()`/`OnValidate()` preserve the requested opacity. Updated direct scene-authored copies in Stage 1, Stage 2, TutorialScene, the Stage 1 optimization preview, and Mage HUD reference preview where the named Image exists; Stage 3/4 inherit the canonical prefab. Static serialization checks are complete; visible Game View and physical Android visual checks remain pending. Backups are under `RecoveryBackups/HUD_FILL_ALPHA_20261105/`.

## Tile Pool partial-row slot alignment - 2026-11-05

Fixed the centering behavior when UI tiles are selected from the 8x2 Tile Pool. Root cause: the `GridLayoutGroup` used `MiddleCenter`, so when eight tiles remained the one-row group was vertically centered in the panel rather than staying on the background wells. The canonical `PersistentGameplayHUD.prefab`, all direct scene-local Tile Pool copies (TutorialScene, Stage 1, Stage 2, Stage 1 optimization preview and Mage HUD reference preview), `TileManager`, and `TutorialResponsiveLayout` now use `UpperLeft` alignment. Mobile padding is compensated to 23/23/16/12, preserving the prior full 8x2 rack's first-slot origin; desktop padding is calculated to preserve the equivalent full-grid origin. The initial 16 tiles retain their intended slot positions, while remaining tiles compact/reorder from the first available slot after selection. No tile ownership, selection, Word Bar, attack or spawning logic changed. Backups are retained under `RecoveryBackups/TILE_POOL_SLOT_ALIGNMENT_20261105/`. Static serialization/diff checks passed; visible Unity Play Mode and Galaxy A05s validation remain required.

## PotionBagTooltipTrigger MonoScript restored - 2026-11-05

Restored the existing `PotionBagTooltipTrigger` implementation as its own Unity script asset at `Assets/Scripts/PotionBagTooltipTrigger.cs`. The original class had been a second `MonoBehaviour` declared at the bottom of `PotionSidebarUI.cs`, so the six authored components in `PersistentGameplayHUD.prefab` had no stable standalone `MonoScript` reference and serialized `m_Script: {fileID: 0}`. The class body was moved unchanged, `PotionSidebarUI.cs` retains its existing binding/initialization code, and a new `.meta` GUID (`aa48fad036914d6bbd454c80b81102c1`) was assigned to all six prefab components: HPotionSlot, PPotionSlot, UPotionSlot, Slot1PassiveSlot, Slot2PassiveSlot and Slot3PassiveSlot. No slot hierarchy or potion/passive inventory logic was changed.

Unity reimported the canonical prefab and reloaded the script domain. Static verification confirms all six prefab components now reference the new script GUID and no `m_Script: {fileID: 0}` entries remain in that prefab. A local .NET build of `Assembly-CSharp` and `Assembly-CSharp-Editor` completed with zero compiler errors. Play Mode tooltip hover/hold, potion use, passive-slot display and physical Galaxy A05s validation remain pending.

## All combat TilePoolPanel scenes aligned - 2026-11-05

Applied the approved shared HUD composition across every discovered non-archival scene that owns or instantiates the combat TilePoolPanel. The production combat scenes now reference the canonical `Assets/Prefabs/PersistentGameplayHUD.prefab`: Stage 1 `Assets/Scenes/SampleScene.unity` and Stage 2 `Assets/Scenes/Stage2Scene/Stage2Scene.unity` were standardized in this pass; Stage 3 and Stage 4 already used canonical prefab instances. Stage 1/2 former scene-authored HUD roots were renamed `LegacySceneAuthoredHUD_Stage1_Inactive` and `LegacySceneAuthoredHUD_Stage2_Inactive` and disabled, but retained intact as temporary fallback hierarchies so old serialized references are not abruptly destroyed before runtime rebinding is verified. Both scenes now list one active canonical prefab instance as a scene root.

TutorialScene uses the canonical prefab with `TutorialPersistentHudBridge` to rebind tutorial `TileManager`/`TileSpawner`/`AttackController`; its legacy duplicate visuals remain fallback-only. Direct copies in `Assets/Scenes/OptimizationTests/Stage1_OptimizedMap_Test.unity` and `Assets/Scenes/MageHud_Reference_Preview.unity` remain isolated test/reference scenes and were layout-aligned, not converted into production runtime owners. Stage 4 boss encounter preview v01/v02/v03 and Reviews/SampleScene continue to use the canonical prefab. Archived `Stage2Scene_PRE_NODE34_RECOVERY_2026-09-16` and the unreferenced legacy `UIs.prefab` remain untouched.

The approved Tile Pool panel/frame/fill transforms, 8x2 grid (compensated slot-origin padding 23/23/16/12, cell 82.6x81.18, spacing 2/3.5), Word Bar panel/grid/artwork, STRIKE transform and source sprite/tints remain consistent. Runtime Word Bar column/cell sizing remains dynamic; saved 11-column count is only the serialized baseline, not a gameplay cap. Ten scene backups from the layout pass and two additional SHA256-verified pre-standardization backups are retained outside `Assets` under `RecoveryBackups/HUD_FRAME_FILL_20261104/`. YAML structure, prefab GUID/SceneRoots links and `git diff --check` passed. Unity scene deserialization, visible Play Mode transition testing, tile/attack smoke tests and physical-device verification remain pending; no Build Settings or gameplay logic changes.

## Approved Live HUD values applied - 2026-11-05

Play Mode exit snapshot verified: RecoveryBackups/HUD_FRAME_FILL_20261104/CombatHudLiveLayoutCapture_20261105_012208.json. Applied the captured Tile Pool, Word Bar, frame/fill, UI_Buttons and STRIKE RectTransforms plus both panel GridLayoutGroup values to Assets/Prefabs/PersistentGameplayHUD.prefab. The applied static layout includes TilePoolPanel (0,56.7), 720x190, scale .83; Tile Pool grid padding 23/23/16/12 (compensated slot origin), cells 82.6x81.18, spacing 2/3.5, eight fixed columns; WordBarPanel (0,215.56), 733.3334x89.7, scale 1.02/.97/.85; Word Bar art 709.13135x76.01638 at unit scale; Word Bar grid padding 9/12/8/8, base cells 66.5, horizontal spacing 2.4, captured Inspector count 11; STRIKE button scale 1. The two fill opacity settings were subsequently set to alpha 215/255 (0.84313726, approximately 84.3%).

Updated TutorialResponsiveLayout and TileManager so Android runtime layout reuses the approved values and does not replace them with the prior 70px square Tile Pool grid or old Word Bar spacing/padding. Word Bar column count/cell width remain dynamic from the actual selected tile count; the captured count 11 is not a 11-letter gameplay cap. Updated TileManager serialized adaptive values in its prefab and Stage 2/3 scenes. Created pre-edit backups for every changed asset/source/doc under RecoveryBackups/HUD_FRAME_FILL_20261104. Visible Game View regression and physical-device QA after persistence remain pending.

## Live HUD Editor runtime verification - 2026-11-05

Opened the standalone Live HUD Editor during Stage 1 Play Mode while PersistentGameplayHUD Prefab Stage remained open and dirty. The EditorWindow found the actual PersistentHUDRoot in DontDestroyOnLoad and reported TutorialResponsiveLayout present. Enabled Pause Responsive Layout For Live Editing and left Auto Align Panel Artwork During Live Editing enabled. Verified a live TilePoolPanel Y-position edit from 56.7 to 80.7 visibly moved the actual Game View rack/frame; restored the runtime value to 56.7 afterward. The canonical prefab was not modified by this test. The automatic JSON snapshot capture was subsequently verified; the approved snapshot was applied to the canonical prefab in the later layout-persistence pass.

Extended the Android HUD Edit Preview menu to support the canonical PersistentGameplayHUD Prefab Stage. The command reuses TutorialResponsiveLayout's exact Android runtime layout calculation for the Tile Pool, paired frame/fill, Word Bar, STRIKE, canvas scalers and sibling layering; it creates a pre-sync prefab backup outside Assets and marks the Prefab Stage dirty for explicit Ctrl+S. The existing Stage 1 scene sync remains available.

Added a standalone EditorWindow at SPELLSTRIKE > HUD > Open Live HUD Editor. It resolves the actual PersistentHUDRoot in DontDestroyOnLoad, so it remains usable while the Hierarchy is showing the separate PersistentGameplayHUD Prefab Stage. Its Play Mode HUD Editing toggle pauses TutorialResponsiveLayout and TileManager panel/grid writes; Auto Align keeps paired artwork following each panel by default. The window exposes panel/frame/fill transforms, Tile Pool and Word Bar GridLayoutGroups, STRIKE transforms and independent fill opacity. A timestamped JSON snapshot is written under RecoveryBackups/HUD_FRAME_FILL_20261104 when Play Mode exits. The snapshot is review-only and does not auto-overwrite the prefab; the approved values from the captured snapshot were deliberately applied to the canonical prefab. Normal gameplay layout is unchanged while the pause toggle is off.

## Isolated Combat HUD frame/fill review scene - 2026-11-04

Created Assets/Scenes/Reviews/SampleScene.unity as an isolated review harness using the existing PersistentGameplayHUD and TileManager prefabs, with scene-local bindings to the real TilePoolPanel and WordBarPanel, a dictionary host, a review-only AttackController host, EventSystem and camera. The scene's internal name is deliberately SampleScene so the existing Stage 1 TileManager initialization path runs. Its build index is -1; Build Settings and production Assets/Scenes/SampleScene.unity are unchanged.

Windows Editor Play Mode at the 2400x1080 A05s Game View verified: DictionaryManager loaded 21,004 gameplay words and 32,887 fallback words; the native pool initialized and verified 16/16 tiles; five real pool tiles assembled SOUND; dictionary validation returned true and the existing STRIKE Button became interactable. The reflection component, alpha-mask and sweep child were present, and the sweep pulse updated its runtime alpha/position. The authored effect remains subtle at maxOpacity 0.28; a temporary high-visibility inspection was run only in Play Mode, then restored to the prefab's authored 1.05s / 0.28-opacity settings.

The review harness has no player Animator/projectile, so its AttackController is disabled and its runtime button click listener is removed after the validity check; this preserves the visible interactable state for HUD review without implying a combat attack test. Scene validation reports zero broken prefabs. The six potion/passive tooltip components were subsequently repaired with a dedicated MonoScript asset, and static prefab checks confirm no missing-script references remain. TutorialResponsiveLayout also emits its expected no-gameplay-host warning because this isolated scene intentionally omits the production player/stage host. Physical A05s/device performance and production attack execution are not claimed.

## Combat HUD frame/fill v02 and target-aware reflection guard â€” 2026-11-04

The v02 Word Bar and Tile Pool artwork, independent fill-alpha controls, sibling
layering outside the counted Tile Pool, approved STRIKE crop, responsive sizing,
and gold-rim reflection presentation were already present in the working tree
when this pass began. Static inspection confirmed the supplied crop PNGs retain
their original dimensions and use Single / Full Rect sprite import, bilinear
filtering, Clamp wrapping, alpha transparency and no mipmaps. The Tile Pool fill
is one combined face-and-16-well graphic; no decorative child was added beneath
`TilePoolPanel`.

Added a read-only `TileManager.IsActiveTargetChallengeAnswer` query that delegates
to the existing `VocabularyWordData.Matches` authority. The reflection presenter
now requires that exact target match in Target Challenge mode in addition to the
existing `AttackController`-derived STRIKE interactability signal. A dictionary-
valid wrong target answer therefore keeps its existing barrier/penalty path but
does not receive the success sheen. No attack, damage, submission or target
challenge flow was changed. Unity visual/runtime checks remain pending. The six potion/passive tooltip components have since been restored with a dedicated MonoScript asset; a local .NET runtime/editor build completed with zero compiler errors.
