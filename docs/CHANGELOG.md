# SPELLSTRIKE Changelog

## 2026-11-05 — Persistent Pause Button Visual

- Added the transparent ornate navy-and-gold `PauseButtonCircle` Sprite and a
  top-right `PauseButton` with two gold bars and a subtle shadow to the primary
  combat Canvas in the canonical `PersistentGameplayHUD.prefab`.
- The control is parented under the HUD root, so it shares the existing
  `DontDestroyOnLoad` lifetime in Stage 1, Stages 2–4, and TutorialScene, all of
  which reference the canonical HUD prefab.
- The button click is intentionally not wired yet. Pause panel, Options, and
  Return to Main Menu behavior are not part of this visual-only change. Unity
  Game View rendering was checked; Play Mode transitions and Android device QA
  remain pending. Backup: `RecoveryBackups/PAUSE_BUTTON_20261105/`.

## 2026-10-02 — Desert Archer Mixamo Material Package

- Extracted the original 4096² color atlas, three-material Blender library, and
  material settings; packaged an FBX with embedded texture for user-run Mixamo rigging.
- Isolated FBX reimport retained texture, 13,409 triangles, scale, and hollow quiver.
  Material-library loading passed. Source character unchanged; Mixamo rigging untested.
  See `docs/STAGE3_NODE2_DESERT_ARCHER_EMPTY_QUIVER.md`.

## 2026-10-02 — Desert Archer Character with Empty Quiver

- Preserved the matching source character's detail and UVs, removed seven arrow
  components and the filled quiver cap, and authored an editable hollow shell.
- Delivered separate Blender/GLB files, packed texture, review renders, and fresh
  export verification: 13,409 triangles and approximately 31.2 cm cavity depth.
- No rig, animation, Unity scene/prefab change, or gameplay integration was added.
  See `docs/STAGE3_NODE2_DESERT_ARCHER_EMPTY_QUIVER.md`.

## 2026-10-02 — Stage 3 Node 2 Bow/Arrow v013

- Preserved v012 and revised the Blender weapon parts: offset arrow passage,
  seated nock, volumetric string/anchors, broader limbs, curved trim, swept guards,
  hooked tips, conforming wraps, cloth clearance, rune lanes and sharper arrowhead.
- Added a linked Blender-only draw/release review scene and reproducible revision/QA
  scripts. Bow: 2,758 triangles; arrow: 460. Five draw states and 121 release samples
  showed no arrow/bow surface intersections, excluding intentional string contact.
- Final atlas/bakes, export consolidation, Unity integration and device validation
  remain deferred. No gameplay scene, prefab or C# script changed. See
  `docs/STAGE3_NODE2_WEAPONS_V013_REVISION_REPORT.md`.

> **Purpose**
>
> This file records notable project changes in plain language.
> This is not a replacement for Git. It is a human/Codex-friendly summary of what changed and why.

---

# Format

```md
## YYYY-MM-DD

### Added
- 

### Changed
- 

### Fixed
- Rewrote `$spellstrike` skill files as UTF-8 without BOM so Codex can parse skill frontmatter more reliably.

### Removed
- 

### Notes
- 
```

---

## 2026-07-15

### Added
- Added a one-word Stage 1 vocabulary data profile and editor command for the `MITIGATE` prototype.
- Added opt-in Teach, combat barrier, mandatory review, explanatory feedback, corrective retry, and runtime attempt-record components.
- Added a deterministic Stage 1 setup and manual test guide.

### Changed
- Extended attacks to carry the submitted word into confirmed projectile impact without changing existing projectile call sites.
- Added a safe Tile Pool helper that guarantees target letters while keeping them shuffled.
- Added an optional Stage Complete review gate that can proceed directly to `StageSelectScene` after the recorded summary.

### Notes
- Runtime and editor C# assemblies compile with zero errors.
- Scene/Inspector wiring, Unity Play Mode testing, Android testing, expert content validation, and persistent result storage remain incomplete.
- This is a coded vertical-slice foundation, not evidence that vocabulary improved.

---

## 2026-07-10

### Added
- Added reusable `CircularArenaBoundary` support for circular combat-lock walls.

### Changed
- Wired Stage 2 Node 2 to use `C_ArenaBoundary` instead of the Node 1 boundary.
- Connected the Stage 2 Corsair Phantom arena visual ring to the Node 2 boundary activation flow.
- Connected Corsair Phantom's existing Phantom Shot charge VFX to the Phantom Shot windup.
- Updated TD-011 to track Unity validation/tuning instead of missing boundary colliders.

### Fixed
- Prevented Node 2 Corsair Phantom combat from activating the Node 1 arena boundary.
- Fixed Corsair Phantom root tagging so player homing projectiles can target and damage it.
- Fixed shared enemy health bar syncing so Corsair Phantom damage visibly reduces HP after Barnacle Husk.

### Notes
- Requires Unity playtest to confirm the generated collider wall matches the visible ring and blocks the player cleanly.

## 2026-07-09

### Added
- Added a Stage 2 `Spawner` object with `TileSpawner` wired to `3d_Tile`, `TilePoolPanel`, Node 1 arena center, Node 1 shrine exclusion center, and the `Ground` layer mask.
- Added TD-010 to track duplicate startup tile-pool spawning between `TileSpawner` and `TileManager`.
- Added final-frame animation events to Corsair Phantom's `BeingHit` and `Die` animation clips.
- Added Stage 2 Node 2 `CP_ArenaCenter`, `CP_ShrineCenter`, and `C_ArenaBoundary` scene objects.
- Added TD-011 to track final Node 2 arena boundary collider placement.

### Changed
- Updated Stage 2 tile spawning status/debt notes to reflect the restored scene-level `TileSpawner` and remaining Unity validation.
- Wired the Node 2 trigger to configure the shared `TileSpawner` for Corsair Phantom's arena.
- Wired Corsair Phantom's serialized `TileDebuffManager` scene reference.

### Fixed
- Made `TileSpawner.SpawnTilesInPool()` tolerate the existing `3d_Tile` prefab's `WorldTile` component when `WorldTilePickup` is absent.

### Notes
- Requires Unity playtest to confirm valid-word attacks spawn refill world tiles around Node 1.

## 2026-07-08

### Added
- Connected Corsair Phantom's Piercing Scream cone hit to `TileDebuffManager.ApplyTileCracking`.
- Added player damage to Corsair Phantom's Piercing Scream when the player is inside the preserved gameplay cone hit check.
- Added a standalone Bezi SPELLSTRIKE skill prompt for Unity-only VFX and scene work.
- Added Corsair Phantom BeingHit and Death animator transitions with script fallback timers.
- Added direct enemy-defeat node completion support to `StageNodeController`.
- Wired Stage 2 Node 2 to start Corsair Phantom from the Node 1 exit trigger and complete Node 2 on Corsair death.
- Added Stage 2 Node 1 tile-refill spawning scene wiring with a `TileSpawner` and Node 1 ground spawn surface.

### Changed
- Tuned `PiercingScreamWaveVFX` into a faster mouth-origin cone-wave with expanding spectral rings.
- Adjusted Piercing Scream VFX aiming to use full mouth-to-player-body direction while preserving the existing flat gameplay cone check.
- Updated Stage 2 Node 1 combat trigger to send `StartBattle` to `BarnacleHuskController`.

### Fixed
- Fixed Corsair Phantom's missing death receiver so its defeat can advance Stage 2 progression.
- Fixed the Stage 2 player `AttackController` serialized `TileDebuffManager` reference for successful-attack tile debuff reduction.

### Removed
- None.

### Notes
- Requires Unity playtest to confirm Node 1 to Node 2 progression, Node 1 attack-refill tile spawning, Corsair hit/death animation timing, and tile cracking feedback in Stage 2.

## 2026-07-07

### Added
- Added proper project-local `$spellstrike` skill metadata and workflow instructions.

### Changed
- Reworked `.codex/skills/spellstrike/SKILL.md` to use the canonical project memory documents as source of truth.
- Synced the project-local `$spellstrike` skill to the global Codex skills folder for easier detection.

### Fixed
- 

### Removed
- 

### Notes
- No Unity gameplay files were changed.

## 2026-07-06

### Added
- Added Codex support documents to the project:
  - `AGENTS.md`
  - `.codex/skills/spellstrike/SKILL.md`
  - `docs/`
  - `checklists/`
  - `prompts/`
  - `TECH_DEBT.md`
  - `PROJECT_STATUS.md`
  - `ARCHITECTURE.md`
  - `DECISIONS.md`
  - `CHANGELOG.md`

### Changed
- Project now has a dedicated AI/Codex workflow for reading architecture, following layer-based implementation, and avoiding unsafe rewrites.

### Fixed
- Corrected Stage 2 scene `TileManager.currentStage` from `1` to `2`.

### Notes
- Codex should inspect before editing.
- Codex should update project docs when major architecture or progress changes.
- Technical debt should be documented before being fixed.

## 2026-11-05 - Set combat HUD panel fill opacity to 215

- Set the canonical Word Bar backdrop and Tile Pool fill Image alpha to 215/255 (0.84313726, approximately 84.3%), preserving white RGB tint.
- Updated `PersistentHUDRoot` serialized opacity controls so runtime initialization/validation does not restore full opacity. Mirrored the values to direct scene-authored HUD image copies where present.
- Static YAML/component checks passed; visual Game View and Galaxy A05s verification remain pending.

## 2026-11-05 - Anchor partial Tile Pool rows to designated slots

- Fixed the centered partial-row behavior caused by `GridLayoutGroup.childAlignment = MiddleCenter` when selected tiles leave fewer than 16 UI tiles in the pool.
- Set the canonical HUD prefab, direct scene copies, `TileManager`, and `TutorialResponsiveLayout` to `UpperLeft` alignment. Compensated mobile/serialized insets and calculate desktop insets so the initial full 8x2 rack keeps its existing first-slot origin.
- Remaining tiles can compact/reorder among the existing 16 background wells; selection and gameplay logic remain unchanged. Static validation passed; Unity Play Mode and physical Android checks remain pending.

## 2026-11-05 - Restore PotionBagTooltipTrigger MonoScript asset

- Extracted the original `PotionBagTooltipTrigger` implementation from the bottom of `PotionSidebarUI.cs` into its own matching Unity script asset, with a dedicated `.meta` GUID. The tooltip hover, pointer and press-and-hold behavior is unchanged.
- Relinked the six existing `PersistentGameplayHUD.prefab` components (HPotionSlot, PPotionSlot, UPotionSlot and the three passive slots) to the new MonoScript. No components were removed and no slot hierarchy, potion inventory or passive-item logic was changed.
- Unity reimport and domain reload completed; static checks confirm all six prefab references use the new GUID and no missing-script references remain in that prefab. Full Play Mode potion/tooltip interaction and Galaxy A05s validation remain pending. A local .NET build of both runtime and editor assemblies completed with zero compiler errors.

## 2026-11-05 - Production combat HUD standardized on canonical persistent prefab

- Added one `PersistentGameplayHUD.prefab` scene-root instance to production Stage 1 (`SampleScene.unity`) and Stage 2 (`Stage2Scene.unity`), matching the existing canonical prefab-instance pattern in Stage 3 and Stage 4.
- Renamed and disabled the former Stage 1/2 scene-authored HUD roots as inactive legacy fallbacks. Their complete 63-object hierarchies remain intact temporarily to preserve any old serialized references while `PersistentPlayerRoot` rebinds scene-owned combat dependencies to the canonical persistent HUD.
- Kept Tutorial's canonical prefab/bridge flow, Stage 4 encounter preview prefab instances, and isolated optimization/reference HUD copies. Archived recovery scene and unreferenced `UIs.prefab` remain untouched.
- Updated architecture and accepted decisions to describe the implemented persistent player/HUD lifecycle and scene-rebind boundary. Static YAML/prefab GUID/SceneRoots checks and `git diff --check` passed. Unity scene-load/Play Mode/Android validation remains pending. The six pre-existing `PotionBagTooltipTrigger` missing-script components were subsequently restored as a standalone script asset on 2026-11-05.

## 2026-11-05 - Combat TilePoolPanel scene coverage aligned

- Applied the approved Tile Pool panel/grid and paired frame/fill transforms, Word Bar composition, and approved STRIKE image/transform to direct scene-local HUD copies in Stage 1, Stage 2, Tutorial fallback, Stage1 optimization test, and Mage HUD reference preview.
- Removed only the stale AttackButton RectTransform and WordBar_OrnateFrame RectTransform overrides from Stage 3, Stage 4, and Stage 4 boss encounter preview v01/v02/v03 so those scenes inherit the updated canonical PersistentGameplayHUD prefab. Review SampleScene already inherits without conflicting overrides.
- Left the archived Stage2Scene recovery copy, unrelated minigame/cinematic scenes, and unreferenced UIs.prefab untouched. Added 10 SHA256-verified scene backups outside Assets. No Build Settings or gameplay code changed.

## 2026-11-05 - Approved Live HUD values persisted

- Verified the automatic Play Mode exit snapshot CombatHudLiveLayoutCapture_20261105_012208.json. Applied the approved runtime RectTransforms and GridLayoutGroups to PersistentGameplayHUD.prefab after creating and SHA256-verifying a fresh pre-edit backup.
- Updated TutorialResponsiveLayout mobile constants/layout to match the captured panel dimensions/scales, paired art proportions/scale, Tile Pool 82.6x81.18 cells and 2/3.5 spacing, Word Bar 9/12/8/8 padding and 2.4 horizontal spacing with a 66.5px dynamic base, plus unit-scale approved STRIKE art.
- Updated TileManager's mobile Tile Pool recalculation to preserve the same exact grid on tile moves/returns. Updated its Word Bar adaptive base and serialized values in TileManager.prefab and Stage2/Stage3 scene overrides. The Word Bar continues to calculate columns and cell size from actual selected tiles; snapshot Inspector count 11 is not a gameplay cap.
- Updated docs and retained pre-edit backups outside Assets. No Build Settings changes or gameplay submit/attack logic changes.

## 2026-11-05 - Runtime Live HUD Editor verified

- Opened the standalone runtime editor while Play Mode and the dirty PersistentGameplayHUD Prefab Stage were both kept open. It found PersistentHUDRoot in DontDestroyOnLoad and TutorialResponsiveLayout on the actual combat Canvas.
- Enabled Pause Responsive Layout and Auto Align. A controlled TilePoolPanel Y adjustment from 56.7 to 80.7 visibly changed the running Game View; restored the value to 56.7. No prefab or scene was saved as part of the test.
- Automatic JSON snapshot capture was exercised successfully on Play Mode exit; the resulting approved snapshot was later applied to the canonical prefab.

- Extended SPELLSTRIKE/HUD/Sync Android Edit Preview to work in the canonical PersistentGameplayHUD Prefab Stage, with an automatic pre-sync backup outside Assets and explicit Ctrl+S save. Existing Stage 1 scene preview remains supported.
- Added the standalone SPELLSTRIKE > HUD > Open Live HUD Editor window, which targets the actual PersistentHUDRoot in DontDestroyOnLoad even while the Prefab Stage Hierarchy is open. It exposes live RectTransform/GridLayoutGroup fields, STRIKE transforms and fill opacity. Pause Responsive Layout stops runtime layout writers; Auto Align keeps paired art following panel transforms.
- Added timestamped JSON capture of live HUD layout values on Play Mode exit. The snapshot is review-only and does not auto-overwrite the canonical prefab. A temporary TilePoolPanel Y edit (56.7 -> 80.7) visibly moved the running Game View and was restored to 56.7.
- Removed the experimental Game View IMGUI panel and conflicting F8/Ctrl+Shift+L key bindings from source; the dedicated EditorWindow is the supported control surface. Existing layout/gameplay behavior is unchanged when live editing is disabled.

## 2026-11-04 - Isolated Combat HUD frame/fill review scene

- Added Assets/Scenes/Reviews/SampleScene.unity, a review-only scene that instantiates the existing canonical HUD and TileManager prefabs and binds the real Tile Pool and Word Bar panels. Added scene-local dictionary/AttackController hosts, an EventSystem and a neutral camera. The scene intentionally uses the internal name SampleScene to exercise Stage 1 initialization but is not in Build Settings.
- Windows Editor Play Mode verified dictionary loading, 16/16 native tile startup, a real SOUND assembly from the pool, successful dictionary validation and the existing STRIKE interactability signal. Reflection mask/sweep objects initialized and the pulse updated at runtime; the temporary high-visibility inspection values were restored without saving changes to the canonical prefab.
- The scene validator reports zero broken prefabs. The six Potion/Passive tooltip components were subsequently repaired with a dedicated MonoScript asset; no slot component was removed. The review-only AttackController is disabled after the button-validity test because this harness has no production player Animator or projectile; no combat attack/device acceptance is claimed.

## 2026-11-04 — Combat HUD v02 audit and target-aware reflection guard

- Audited the already-present Word Bar / Tile Pool frame-and-fill wiring, separate
  opacity controls, crop-sprite imports, responsive sibling layering, approved
  STRIKE image, and PNG-alpha reflection masks. No duplicate tile wells, extra
  Canvas, custom shader or gameplay validator was added.
- Added a read-only `TileManager.IsActiveTargetChallengeAnswer` query using the
  existing `VocabularyWordData.Matches` target rule. `CombatHudWordReflection`
  now combines that exact Target Challenge match with the existing STRIKE
  interactability signal, preventing a dictionary-valid but incorrect target word
  from receiving the success sheen. Attack/submission/penalty semantics are
  unchanged.
- Static source and asset-import checks passed. Unity alpha-state, reflection,
  tile interaction, Play Mode and physical Android/A05s validation remain pending
because the six potion/passive tooltip components were subsequently repaired with a dedicated MonoScript asset. A local .NET runtime/editor build completed with zero compiler errors; Play Mode and device visual checks remain pending.
