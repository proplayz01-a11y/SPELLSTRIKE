# Convergence Citadel v003 — isolated Unity Stage 4 validation

Date: **2026-10-20**. Project: Unity **6000.3.10f1**, Android editor target, URP **17.3.0**, glTFast **6.18.0**, AI Navigation **2.0.11**.

**Overall disposition: PASS for bounded, Edit Mode source/import, collision and navigation checks. NOT accepted for production gameplay, Titan animation, complete LOD switching or Android device performance.**

## Scope and user-locked stage design

The user explicitly expanded the isolated v003 import task to create a new **Stage 4** folder, scene and organized Stage 4 assets. Therefore this pass used `Assets/Stage 4/ConvergenceCitadel_v003` and `Assets/Scenes/Stage4Scene`, instead of the earlier handoff's proposed `Assets/FinalStage` path. The saved production scenes were not used as output destinations.

The linear route is Start / Last Gate -> Node 1 Hollow Knight -> Node 2 Drowned Captain -> **Node 3 Titan Earthbreaker, largest island** -> final Vander Poel throne courtyard. The earlier removed Cursed Gothic stage is not recreated. Gameplay wiring, Stage Select migration, save IDs, review gating and cutscenes are outside this bounded validation.

## Created scenes

1. `Assets/Scenes/Stage4Scene/Stage4Scene.unity` — standalone visual and collision **scaffold**, no actors or progression.
2. `Assets/Scenes/Stage4Scene/Stage4Scene_ConvergenceCitadel_v003_Validation.unity` — isolated scale-validation scene, with the real canonical player and Titan V017 prefabs **inactive** and placed at manifest-aligned Unity locations.

Both were authored by invoking `Stage 4 > Convergence Citadel v003 > Create Isolated Scenes (Preserve Open Scenes)` in the **visible Unity Editor**, using additive scenes. Only the newly created scenes were saved and closed. A later visible-menu axis-alignment operation updated only these two new scenes. No original boss rig, prefab, clips, production scene or authored collision objects were overwritten by this task.

Scenes contain an active imported visual LOD0 model, an inactive LOD1 reference model, a disabled imported collision-source model and **19 independent static non-convex MeshColliders** referencing the imported collision mesh subassets (19 meshes, 3,816 triangles). An unbaked disabled NavMeshSurface is present. The standalone QA NavMesh asset is separate, not auto-wired to either scene.

## PASS / FAIL / NOT TESTED evidence

| Gate | Status | Exact evidence |
|---|---|---|
| Source integrity | **PASS** | Original v003 inventory **66/66 SHA-256 hashes matched**; copies of main, LOD1 and collision GLBs matched the source hashes. |
| Unity glTF import | **PASS** | glTFast ScriptedImporter imported all three GLBs; 110 visual mesh instances, 281,090 triangles; LOD1 110 mesh instances, 199,754 triangles; collision 19 mesh subassets. |
| Material format / shader compatibility | **PASS (Edit Mode)** | All **53** imported materials use supported `Shader Graphs/glTF-pbrMetallicRoughness` shaders with `RenderPipeline=UniversalPipeline`; **0 unsupported**. Two bound 2048x2048 atlas textures: BaseColor and ORM. No Unity pink-material compile evidence in the audit. Does **not** establish an approved per-island in-game visual match. |
| All imported gameplay markers | **PASS** | **36/36** marker transforms match manifest after glTFast handedness conversion `Unity X = -glTF X`; maximum discrepancy **0.00001** Unity units. |
| Player real scale | **PASS (inactive scene instance)** | Actual Unity QA instance of `VanAertPlayer`: CharacterController **height 21.4200, radius 5.3550** at scene actor scale **10.71**. Original prefab unchanged. |
| Titan real collider scale | **PASS (inactive scene instance)** | Real Titan V017 prefab imported with **six** CapsuleColliders: torso **12.003** world-radius, four attached solid leg capsules, and separate projectile trigger **21.2053** world-radius. Runtime deformed mesh/stance bounds unmeasured. |
| Island sizes | **PASS (actual Unity collider bounds)** | Node3/Titan floor AABB **823.59 × 940.21**, final throne island **521.03 × 509.31** horizontal world units. Node3 is largest. These are collider bounding boxes, not measured walkable areas. |
| Static collision setup | **PASS** | **19/19** independent collision meshes are static, enabled, non-convex, non-trigger MeshColliders; 0 collision renderers visible; all **4 bridge deck** plus all **4 bridge rail** collider meshes present. Imported glTF hierarchy remained immutable. |
| Bridge floor physics rays | **PASS (Editor fixture)** | **36/36** Unity Physics raycast samples on imported deck collision meshes, 9 samples per bridge. Sample points are evaluated with the observed Unity axis conversion; collision is filtered to the QA source hierarchy. This is **not** player-controlled traversal. |
| NavMesh bake | **PASS (standalone QA only)** | **19/19 physics sources** collected; local agent radius **5.355**, height **21.42**, climb **3.213**, slope **45°**, voxel **1**, tile **128**; standalone `NavMeshData` asset saved in `Navigation/`. No global agent settings were changed. |
| All five areas reachable by NavMesh | **PASS (Editor fixture)** | Five arena/spawn samples found; **8/8** directional adjacent paths `PathComplete`: Start↔Hollow, Hollow↔Captain, Captain↔Titan, Titan↔Vander. |
| LOD geometry/source labels | **PASS (asset parity)** | 110 matching named meshes in LOD0 and LOD1; triangle reduction **28.94%** (281,090 -> 199,754). LOD1 copy is inactive to avoid double-rendering. |
| Actual runtime LOD switching | **FAIL / NOT IMPLEMENTED** | **0 authored LODGroup components**; LOD1 is not wired. Production cannot claim dynamic LOD until renderer pairing and real camera/Android performance are validated. |
| Full isolated visual screenshot | **NOT TESTED to clean acceptance** | Visible Unity scene preview was opened additively, but the existing SampleScene geometry also appeared in the view; screenshot is explicitly named `Unity_Editor_AdditivePreview_MixedScenes_20261020.png`, **not** a clean Stage4-only render. No per-island Unity screenshots captured. Prior Blender visual renders remain **Blender-only** evidence. |
| Character-controlled bridge traversal / retreat / camera | **NOT TESTED** | No Play Mode. NavMesh complete paths and raycasts cannot establish animation, walkability, camera clearance or collision seam acceptance. |
| Titan full V017 stomp/stance IK/contact | **NOT TESTED** | No frame-28 support, four right-sole genuine contacts, repeated original-clip replay, 94% reach cap behavior, actual actor movement, 98m wave hit testing or near-rim combat was executed on this map. No original tolerances modified. |
| Objective gates / boss gauntlet / review / ending | **NOT TESTED, NOT WIRED** | Objective and encounter folders and `Encounters/Stage4_EncounterObjectiveReference.json` describe integration targets only. Hollow/Captain animation sources identified; Vander-specific rig/controller/animations not established by this pass. |
| PC runtime performance and Galaxy A05s | **NOT TESTED** | No measured frame time, draw calls, memory, runtime material-VFX parity or 30FPS claim. |

## Unity actual island collider floor AABB measurements

| Island | Horizontal collider bounds X × Z (Unity units) | Comments |
|---|---:|---|
| Start / Last Gate | 159.44 × 162.10 | Intro island |
| Node 1 / Hollow Knight | 308.74 × 249.17 | Ruined Kingdom courtyard |
| Node 2 / Drowned Captain | 323.90 × 317.39 | Turquoise dock floor |
| **Node 3 / Titan** | **823.59 × 940.21** | **Largest**, level main combat floor still requires live foot IK test |
| Vander Poel / throne courtyard | 521.03 × 509.31 | Finale / actual throne marker present |

## Evidence files

- `Validation/UnityImportAudit_20261020.md` — actual Editor material, import, real player/Titan capsule, all-36 markers, collider bounds, bridge sample evidence.
- `Validation/UnityNavigationQA_20261020.md` — actual Unity BakeNavMeshData sources, agent settings, five samples, 8/8 path statuses.
- `Navigation/ConvergenceCitadel_Player535_UnityQA_NavMesh.asset` — **QA-only** baked NavMesh, not wired to production.
- `Validation/Unity_Editor_AdditivePreview_MixedScenes_20261020.png` — real visible Editor screenshot with **existing SampleScene in frame**; not a clean screenshot of the standalone stage.
- `Validation/ConvergenceCitadel_MapManifest.json`, `ConvergenceCitadel_GLBVerification.json`, `ConvergenceCitadel_SourceVerification.json` — prior Blender-only checks, not Unity acceptance.
- Actual native Unity `Editor.log` records initial glTFast import, corrected scene build, corrected axis alignment, repeated import audit and separate NavQA. An initial attempt to attach `MeshCollider` directly to imported glTF child failed with `MissingComponentException`; it was corrected by **separate editable collider GameObjects** and did not save a broken scene. The final successful build reported **19 colliders**.
- Builder and read-only audit menu scripts in `Assets/Editor/Stage4ConvergenceV003SceneBuilder.cs`, `Stage4ConvergenceV003Audit.cs` and `Stage4ConvergenceV003NavigationQA.cs` (new files, not original gameplay scripts).

## Stage 4 asset inventory / intentionally absent assets

`Assets/Stage 4/ConvergenceCitadel_v003` contains real v003 GLBs, two original atlas PNGs, reference image, technical reports, and organized folders: `Characters` with Hollow Knight / Drowned Captain / Titan / Vander Poel `Animations` source-reference READMEs, `Environment` (Visual/LOD/Collision/Textures/Reference), `Objectives`, `Encounters`, `Navigation`, `Validation`, `UI`, and `VFX`.

**No new actual `.anim` clips or character models were invented.** Original bosses' scene/clip sources are preserved. The Stage 4 encounter/animation references are intentional documentation only. Boss prefabs, Stage4 objective controllers, gates, water shaders, cinematic transitions and production HUD wiring are not created by this validation.

## Production/persistence safety

- **No Play Mode** used; no Stage Select, Build Settings, gameplay, existing `.prefab`, save file or PlayerPrefs edits by the new Stage4 scripts.
- Pre-existing dirty/untracked work in `SampleScene`, `Stage2Scene`, `Stage3Scene`, `VanAertPlayer` and `EditorBuildSettings` was **not reset, copied over or committed**. The active Editor displayed `SampleScene`; Stage4 was opened in `NewSceneMode.Additive` for build/QA, then closed. All new output paths were absent before construction.
- There were earlier Editor launch/reload transitions while the project was already dirty. We did **not** independently establish whether any pre-existing *in-memory*, unsaved Unity changes survived those Editor transitions. Do not claim that guarantee. The on-disk original files still exist and remain dirty/untracked at closeout.
- No live production scene backup/restore was performed because the stage scaffolding workflow did not edit those scenes. All output scenes are independently named. A separate authoring phase must back up the exact production scene before any integration.
- A global player input backend warning was declined without restarting Unity through that dialog or changing project input settings. No V017 rig/animation or Stage2 MeshColliders changed.

## Remaining minimum integration sequence

1. On a safe future session, independently back up and verify the current original scenes and any unsaved Editor buffers. Establish proven save isolation **before** Play Mode.
2. Open **only** `Stage4Scene_ConvergenceCitadel_v003_Validation.unity` for a clean Editor/Game View visual review, capture full map plus five island screenshots, check all material/ORM channels, water/portal/fog placeholders and camera scale. Do not alter production SampleScene in the process.
3. Pair the 110 matching LOD0/LOD1 renderers into appropriate renderer-level or island-level LODGroups without duplicating active marker hierarchies; measure draw calls, memory, visual pop and PC/A05s impact.
4. Perform genuine player-controller traversal both directions on **all four bridges**, islands, boundaries, stairs and gate approaches. Calibrate camera and character capsule without shrinking the production actor.
5. Replay actual Titan V017 in this specific new arena using its existing foot-contact harness. Verify every existing stance/contact/damage requirement and all 98m wave positions. Any abort/failure is a geometry or stance issue, **not** permission to loosen animation gates.
6. Only after these gates pass, start a separately reviewed production gameplay integration: canonical boss scene instances/prefabs, encounter objectives, bridge gates, vocabulary review, final story cutscene, save-safe Stage Select update, then PC and physical A05s validation.

**Current handoff:** Stage 4 isolated environment and imported collision/NavMesh geometry are ready for the *next validation phase*, not for defense-ready production gameplay.
