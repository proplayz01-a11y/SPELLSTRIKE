# Stage 4 Convergence Citadel boss encounters | 2026-10-27

**State: v03 rescue builder authored and source-checked, NOT EXECUTED in Unity while prime creates Panels 31–42. Original map audit remains verified, v01 and v02 failed scene copies are pristine. No four-boss scene has yet been saved; Play Mode, animation timing, Android performance and production promotion remain PENDING.**

## v03 rescue READY FOR COORDINATED EDITOR (no Unity used in this preparation)

- **New Editor script:** `Assets/Editor/Stage4BossEncounterPreviewBuilder_20261027_V03.cs`. Distinct class and menu `Stage 4 > Convergence Citadel v003 > BUILD Boss Encounters CONSULTATION PREVIEW 20261027 V03`. Previous v01/v02 builders and preview scene files were not edited or reused.
- **New destination:** `Assets/Scenes/Stage4Scene/Stage4_BossEncounters_Preview_20261027_v03.unity`. Distinct backup at `Library/Stage4BossEncounters_20261027/Stage4Scene_before_boss_preview_v03.unity`. Both slots must be absent; builder refuses to run if either the scene, its .meta, the backup, or its .meta already exists. There is no destructive replacement path.
- **Titan correction:** The source parent GameObject `S3N4_TitanV017_ProductionRig` is instantiated as one complete rig, not the child alone. The single direct child `S3N4_Titan_EarthbreakerV017_BOSS` retains the real `TitanEarthbreakerController`, Animator, EnemyHealth, NavMeshAgent, skeleton, renderers and animation-linked components. The Stage4 node binds to the **child controller**, while its enemy root is the enclosing actor clone. Parent and child identity, uniqueness, source hierarchy, clone destination scene and placed boss position all receive explicit checks.
- **Preservation checks:** Before and after installation, verify 11 scene LODGroups; 21 map MeshColliders/20 enabled under the production root; 20/19 inside the approved `Independent_StaticMeshColliders` root; exactly one extra enabled `Bridge_Node1_Node2_wood_light`. Require original production SHA256 `7EA2C51E8D00E29E04491DBB876688FB5CD197B36F613E5E89B71A38321E506E`, both before creation and before saving the v03 scene. Require clean **active Stage3Scene**, Play Mode off, and no other dirty loaded scenes. Do not change source/production scene or build settings.
- **Pre-save integration checks:** Exactly four active placed actors with EnemyHealth and exact marker positions, four `StageNodeController` components, four entry triggers, three solid bridge lock colliders, one progression gate binder, and one correctly typed actor controller per boss. Only then is `SaveScene(previewScene, Preview)` called. Vander Poel remains a **temporary consultation encounter**, not approved production boss AI.
- **Read-only checks already PASSED:** `Tools/Tests/Stage4BossPreviewV03SourceContract_20261027.ps1` 45/45 checks; original `Tools/Tests/Stage4BossEncounterSourceContract_20261027.ps1` 36/36. Verified original Stage4 and v01/v02 copy hashes unchanged. No new preview file exists yet.
- **Coordination requirement:** Do not request Unity import, compilation, menu action, source scene loading, or Play Mode while prime's P31–42 work owns Editor. After prime explicitly releases Editor, preflight VALORANT absent, clean Stage3 active, Unity compiler clean, original hash still matching and v03 target absent. Compile/import this new script then run the exact v03 menu **once**. On any exception stop without overwriting; report Console error and original/v03 hashes. After success read-only inspect the **saved** v03 file and exact four actor/node/gate counts. Live NavMesh / combat / mobile tests still separate work.

## Original scene-wide audit and versioned v02 result (2026-10-27)

- Opened **original** `Assets/Scenes/Stage4Scene/Stage4Scene.unity` read-only/additively with the existing clean Stage3Scene remaining active.
- **Scene-wide exact counts:** 11 LODGroups, 21 MeshColliders, 20 enabled MeshColliders, all 20 enabled colliders active in the hierarchy. All 11 LODGroups and all 21 MeshColliders are inside `Stage4_ConvergenceCitadel_PRODUCTION`; there are **zero additional LODGroups or MeshColliders in sibling scene roots**.
- **Independent physics scope:** `Independent_StaticMeshColliders` has 20 MeshColliders of which precisely **19 are enabled**. The extra enabled world MeshCollider is `Bridge_Node1_Node2_wood_light`, which is inside the production root but outside that independent physics subtree.
- The correct invariant is scene-wide 11 LODGroups and world-map 21 total/20 enabled colliders, with 20 total/19 enabled within the protected independent physics subtree, and exactly one enabled legacy bridge collider outside that subtree. The previously failing guard incorrectly counted 20 enabled colliders over the whole world-map root while expecting 19. No invariant was lowered or disabled.
- Retargeted the builder to the separately versioned `Assets/Scenes/Stage4Scene/Stage4_BossEncounters_Preview_20261027_v02.unity`; the old v01 copy was not modified. The builder now enforces the exact source counts before and after installation.
- **One authorized v02 menu execution failed before saving** with `InvalidOperationException: Assets/Scenes/Stage3Scene/Stage3Scene.unity: expected one S3N4_TitanV017_ProductionRig with TitanEarthbreakerController; found 0`. The editor restored the original active clean Stage3Scene. This is an actor lookup error after the collider preservation guard passed; no boss actors/nodes/gates have been saved in v02.
- Read-only inspection found one `TitanEarthbreakerController` on `S3N4_Titan_EarthbreakerV017_BOSS`, **a child of** `S3N4_TitanV017_ProductionRig`. The child owns the controller and has lossy scale `(40.01, 40.01, 40.01)`. A future builder revision needs to copy the original parent rig and bind the controller on its child without stripping the rig hierarchy.
- No additional retry after this new exception. Both v01 and v02 scene files still match production SHA-256 `7EA2C51E8D00E29E04491DBB876688FB5CD197B36F613E5E89B71A38321E506E`. Production and Stage3 were never modified.

## Actual Unity Editor invocation 2026-10-27: guarded validation stopped generation

- The first menu invocation returned Unity error: `ExecuteMenuItem failed because there is no menu named 'Stage 4/Convergence Citadel v003/BUILD Boss Encounters CONSULTATION PREVIEW 20261027'`. Read-only reflection confirmed the C# file was on disk but still absent from Unity's Asset Database. A compilation/asset import resolved this, and the Editor loaded the menu successfully. This was a registration issue, not an actor build.
- The now-registered visible menu ran once. A full, byte-identical production scene copy was created at `Assets/Scenes/Stage4Scene/Stage4_BossEncounters_Preview_20261027.unity` with a unique .meta GUID. **No actors, encounter nodes or gates were installed** because the guarded count check failed early with `InvalidOperationException: Protected 11 LODGroups / 19 enabled map colliders are not present.` The Editor logged `[Stage4BossPreview] Setup incomplete. Production scene untouched. Preview saved=False`.
- Read-only additive Unity inspection of the scene copy found **11 LODGroups**, and **19 enabled MeshColliders under `Independent_StaticMeshColliders`**. There are 21 MeshColliders total in the broader world hierarchy (20 enabled), because a separate enabled `Bridge_Node1_Node2_wood_light` MeshCollider exists outside the independent physics group. This explains the old check's false failure. The Stage4 builder script was updated to inspect the correct protected collider group and verify the broader count remains unchanged.
- Unity confirmed marker world positions `Node1_BossSpawn=(-276,6,-166.5)`, `Node2_BossSpawn=(-624,12,-343.5)`, `Node3_BossSpawn=(-1280,18,-620)`, `Boss_BossSpawn=(-2020,30,-1040)`. These are validated marker locations **only**, not yet validated character placements.
- The builder is now targeted to v02 and refuses to overwrite a preexisting v02 preview or backup. Because the single authorized v02 execution also encountered an error, any future attempt must use another versioned output after reviewing the source hierarchy.
- Stage3Scene remained the sole loaded, active, clean scene after both the menu attempt and additive inspections. No Play Mode. Original Stage4 production retains SHA256 `7EA2C51E8D00E29E04491DBB876688FB5CD197B36F613E5E89B71A38321E506E`.

## Protected production baseline

- Scene: `Assets/Scenes/Stage4Scene/Stage4Scene.unity`
- Source scene GUID: `b69985f54b0efbb40b3bb1ff68432600`
- Unmodified scene SHA-256: `7EA2C51E8D00E29E04491DBB876688FB5CD197B36F613E5E89B71A38321E506E`
- Independent raw backup + original meta: `RecoveryBackups/Stage4BossEncounters_PreIntegration_20261027/Stage4Scene_before_boss_encounters.unity[.meta]`, matching SHA-256.
- Preserve all five islands, Node3 largest island, 11 LODGroups, 19 enabled static MeshColliders, tested Bridge04 proxy, disabled magenta portal, player, camera, HUD, managers, active NavMesh and existing build registration.

## Actual reusable actors

| Stage 4 encounter | Existing source | Source controller | Preview hookup |
| --- | --- | --- | --- |
| Node1 Hollow Knight | `Assets/Scenes/SampleScene.unity`, GameObject `Hollow Knight` | `HollowKnightController` + `EnemyHealth` | Full actor and sword copied into new preview; player rebound; `StartBattle`; death relay calls Stage4 `StageNodeController` after 3.5 seconds |
| Node2 Drowned Captain | `Assets/Scenes/Stage2Scene/Stage2Scene.unity`, GameObject `DrownedCaptain` | `DrownedCaptainController` + `EnemyHealth` | Full scene instance copied; NavMesh, player, tile area and `deathReceiver` rebound; original death callback completes node |
| Node3 Titan Earthbreaker | `Assets/Scenes/Stage3Scene/Stage3Scene.unity`, parent `S3N4_TitanV017_ProductionRig`, child `S3N4_Titan_EarthbreakerV017_BOSS` | `TitanEarthbreakerController` on child + `EnemyHealth` | Planned full V017 rig reuse; previous builder lookup failed because controller is on child, not parent. No placement has been saved |
| Final Vander Poel | `Assets/Cinematics/Storyboard3D/Characters/VanderPoel/Prefabs/VanderPoel_CinematicActor.prefab` | **No existing gameplay boss controller found** | Original real cinematic rig instantiated, with explicitly temporary `Stage4VanderPoelPreviewEncounterV01` for NavMesh pursuit/proximity hit and HP/death relay |

Stage1-3 source vocabulary barriers are disabled only on Stage4 actor clones so they cannot consume the source scenes' vocabulary objects or interfere with Stage4's tile system. The actual prior-stage scenes, prefab assets, rewards, fragments, animation clips, systems and saves are untouched.

## Preview scene generation and structure

Visible Unity Editor menu, **run only after the primary cinematic Editor owner agrees**:

`Stage 4 > Convergence Citadel v003 > BUILD Boss Encounters CONSULTATION PREVIEW 20261027`

It copies actual production Stage4Scene to a new, unique-GUID scene:

`Assets/Scenes/Stage4Scene/Stage4_BossEncounters_Preview_20261027_v02.unity` (present as untouched scene copy after aborted generation; v01 retained separately)

The menu refuses to run during Play Mode, while any loaded scene has unsaved changes, or when the target preview/backup already exists. It loads the source scenes additively, duplicates the existing scene actors under the *new* Stage4 preview scene directly, then rebinds references. It closes only source scenes opened by its run and restores the previously active scene.

Preview hierarchy:

- `_Stage4BossEncounters_20261027_PREVIEW/Actors`: 4 character actors
- `.../NodeTriggersAndProgression`: 4 `StageNodeController` nodes and 4 player-trigger collider zones
- `.../LockedBridgeGates`: 3 bridge lock colliders, for Gates 02/03/04
- `Stage4BridgeGateBinderV01`: reads the existing `StageProgressionManager`, opens gate 02 after Hollow Knight death, gate 03 after Captain, gate 04 after Titan

The Start to Node1 bridge stays open. No change to scene selection, persistence, build settings or final-stage progression storage is included. Gates are physical prototype blockers; final VFX, animations and guidance UI are still needed.

## Scene markers and scale checks

The builder consumes imported actual LOD0 marker objects and checks boss spawn positions before writing:

| Marker | Expected Unity world position (X, Y, Z) |
| --- | --- |
| Node1_BossSpawn | (-276, 6, -166.5) |
| Node2_BossSpawn | (-624, 12, -343.5) |
| Node3_BossSpawn | (-1280, 18, -620) |
| Boss_BossSpawn | (-2020, 30, -1040) |

These reflect the proven source GLB-to-Unity X-axis flip. Source actors preserve their original scale; Vander Poel preview visual scales to a 24-unit-tall consultation target from its real mesh bounds. Actual grounded foot positions, model sizes, animation speed and combat ranges require live visual acceptance.

## Validation completed

- Source scenes contain the specified real actors/controller classes.
- Stage4 production YAML still has `Stage4ProgressionManager` configured for four nodes, no Stage4 consultation actor, validated Bridge04 reference and production world root.
- Source map manifest exposes four boss positions, four encounter entries and three gate markers; five islands and four bridges are recorded.
- Raw production scene matches its preserved pre-integration backup by SHA-256.
- Run `powershell.exe -NoProfile -ExecutionPolicy Bypass -File Tools/Tests/Stage4BossEncounterSourceContract_20261027.ps1` from repository root: **PASS**, exit code 0 on 2026-10-27.
- Stage4-only scripts and installer were inspected statically. **No Unity compile, scene-asset generation or Play Mode tests were run.**

## Required shared-Editor acceptance after prime releases Editor

1. Check VALORANT is not running; save and close clean cinematic edits normally. Check Unity Console zero script compile errors, then run the visible preview-generation menu exactly once.
2. Open generated preview scene in the already-running Unity Editor, inspect Hollow Knight sword, Drowned Captain anchor, Titan V017 height/sole/Animator and Vander Poel cinematic rig appearance. Inspect the four spawn markers, NavMesh feet and proper outward-facing rotations.
3. Verify all 11 LODGroups, 19 enabled original MeshColliders, Bridge04 proxy, one player, one camera/HUD and persistent TileManager still exist. No magenta portal or duplicate Level objects.
4. In isolated preview Play Mode verify triggers 1 to 4, player damage and word attack damaging `EnemyHealth`, death events, ordered `StageProgressionManager` advancement and Gate02 → Gate03 → Gate04 unlocking.
5. For Titan specifically check `StartAwakening` transitions through real V004 animation before `StartBattle`, scripted NavMesh initialization succeeds, no accidental Stage3 riddle objects appear, and default damage flags match approved gameplay state.
6. Check PC visuals/FPS and separately profile Galaxy A05s only after PC correctness. Document source/preview scene hashes and repeat exactly the Stage3 → Stage4 player/HUD handoff check before any production promotion.

## Pending and risks

- **Not complete**: Vander Poel requires a real combat rig/controller, animator, attacks, event-timed damage, death animation, and end review/cutscene. Existing preview proximity attacks are temporary.
- **Not verified**: exact imported source actor transforms, capsule collider fit, per-island NavMeshAgent footprints, grounded Titan animation, safe cross-scene cloned component dependencies, iOS/PC/Android performance.
- Stage3 Titan combat enable flags may still be false. This builder keeps their original approved state, so no claim of full damage acceptance is made.
- Hollow Knight's Stage1 death callback is stage-specific; a standalone Stage4 relay handles demonstration progression after the animation presentation delay.
- Some existing Drowned Captain and Titan event pathways assume specific Stage2/Stage3 dependencies; validate live before promotion.
- Generated preview and production promotion require explicit coordination with prime while cinematic Editor work is in progress. Production remains unchanged as of this report.

## Files created by worker-2

`Assets/Stage 4/ConvergenceCitadel_v003/Encounters/Runtime/Stage4BossDefeatRelayV01.cs`

`Assets/Stage 4/ConvergenceCitadel_v003/Encounters/Runtime/Stage4BridgeGateBinderV01.cs`

`Assets/Stage 4/ConvergenceCitadel_v003/Encounters/Runtime/Stage4VanderPoelPreviewEncounterV01.cs`

`Assets/Editor/Stage4BossEncounterPreviewBuilder_20261027.cs`

`Tools/Tests/Stage4BossEncounterSourceContract_20261027.ps1`

No pre-existing gameplay scene/actor prefab/C# source file was edited.
