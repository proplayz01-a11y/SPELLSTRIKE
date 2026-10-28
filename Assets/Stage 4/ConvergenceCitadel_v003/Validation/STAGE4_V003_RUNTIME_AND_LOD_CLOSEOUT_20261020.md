# SPELLSTRIKE Stage 4 Convergence Citadel v003 — bounded validation closeout

**Date:** 2026-10-20. **Overall status:** isolated environment import, LOD assembly, static navigation/collision and player-sized physical bridge traversal **PASS**. **Not production gameplay accepted** until canonical player/controller, Titan animation/physical combat, gate/quest sequencing, ending and real PC/A05s device tests.

## What actually changed (Stage4 files only)

- Two pre-existing newly authored Stage4 scenes (`Assets/Scenes/Stage4Scene/Stage4Scene.unity` and `Stage4Scene_ConvergenceCitadel_v003_Validation.unity`) now contain **11 scene-local LODGroups**, each with matching LOD0/LOD1 renderers, **110 matching pairs** across 5 islands, 4 bridges and supporting environment. LOD1 gameplay marker copies remain inactive; 36 primary gameplay markers remain active. Editor visible-menu forced LOD0/LOD1 comparison screenshots are stored in `Validation/Unity_Stage4_FiveIslands_ForcedLOD0_20261020.png` and `...ForcedLOD1_20261020.png`. Automatic selection was restored after inspection.
- Created a **third separate** scene, `Assets/Scenes/Stage4Scene/Stage4Scene_ConvergenceCitadel_v003_RuntimeQA.unity`, for explicit Play Mode collision testing. Both canonical player and Titan calibration prefab instances remained inactive. The new `Stage4V003RuntimeTraversalProbe` added a temporary real, correctly sized CharacterController (height 21.42, radius 5.355), used the approved QA NavMesh and performed actual `CharacterController.Move()` physics across each bridge both ways. This was a dedicated probe, not the Van Aert input, animations or health system.
- An initial real Play Mode test passed **6/8 directions**, exposing a true Bridge04 collision obstruction that NavMesh `PathComplete` and earlier raycast samples did not catch. The original raw GLB collision surface locked capsule motion at world Unity positions near x=-1710/-1731, z=-829/-840.
- Authored **Bridge04 smooth collision-only mesh** `Navigation/Bridge04_SmoothCollision_QA.mesh.asset` using the *approved existing* linear bridge centerline and clear width **30.72** and the intended step/ramp profile (Unity floor top **15 -> 15 -> 27 -> 27**). The original visual GLB, imported collision GLB and Blender source remain unchanged. A QA-only menu first disabled just the original Bridge04 deck MeshCollider and added the proxy in RuntimeQA. Physical QA confirmed **2/2** directional Bridge04 crossings with **zero side-collision** and unsupported frames after a real-capsule-sized endpoint tolerance was used.
- A clean **all-four-bridges retest passed 8/8** in `Validation/UnityRuntimeAllBridges_AfterBridge4Proxy_20261020.md`: no unsupported frames, real on-deck collisions in each direction. The earlier failures and diagnostic runs have been retained as source evidence, not overwritten.
- After 8/8 evidence was recorded, the validated collision proxy was promoted by a guarded, visible Editor menu into **both new Stage4 scenes only**. The old imported `COL_Bridge_04_Deck_PHYSICS` collider was **disabled** only on those Stage4 scene instances; the new `Bridge04_SmoothCollision_Validated_Traversable` has an active static non-convex MeshCollider. Unity verified **19 enabled** colliders in each scene after replacement. Original GLB and other 18 physics meshes untouched. Each stage file has a dedicated original-before-proxy backup in `Library/Stage4Bridge04_ValidatedPromotion_20261020` and pre-LOD backups in `Library/Stage4ConvergenceV003_LOD_Backup_20261020`.
- Independent, Editor-only Titan island **static floor and stance clearance preflight passed 88/88** locations in `Validation/UnityTitanNode3SurfacePreflight_20261020.md`: radial stations 0/7/10/14/20/28.2/49.3/72.1/98/140/200 world units at eight bearings, simulated left/right floor supports 20 units apart, maximum difference **0.0000**, all upward normals **1.0000**, no missing floor. This **does not** stand in for the V017 rig/animation/true heel-sole impact validator.

## Verified baseline and actual evidence

| Validation gate | Status | Source |
|---|---|---|
| Original v003 file integrity | PASS | `ConvergenceCitadel_FileInventory.json`, 66/66 SHA-256, imported GLBs unchanged |
| Actual imported player/Titan dimensions | PASS (Edit Mode) | `UnityImportAudit_20261020.md`; Titan original six CapsuleColliders/disabled nominal NavMeshAgent; player 21.42 x radius5.355 |
| URP imported material compatibility | PASS (Edit Mode) | 53/53 supported shaders, URP rendering tags, two 2048² texture atlases, `UnityImportAudit_20261020.md` |
| Markers and bridge static raycasts | PASS | Unity 36/36 imported markers and 36/36 initial bridge deck rays |
| Collider and bridge rail inventory | PASS | 19 enabled after Bridge04 replacement, 4 bridges and four rail meshes |
| Player-sized NavMesh | PASS | Standalone QA asset, five island center samples and 8/8 directional `PathComplete` routes |
| LOD0/LOD1 source parity and scene-local LODGroups | PASS | 110/110 renderer pairs, 11 groups each scene, forced LOD0/LOD1 screenshots |
| Original Bridge04 physics in real capsule motion | **FAIL** | `UnityRuntimeCapsuleTraversalQA_20261020.md`: only 6/8 first run |
| Replaced Bridge04 physics in real capsule motion | **PASS** | `UnityRuntimeBridge4CollisionProxyCorrectedQA_20261020.md`: 2/2 |
| Final complete real runtime capsule crossings | **PASS** | `UnityRuntimeAllBridges_AfterBridge4Proxy_20261020.md`: **8/8** crossings, 0 unsupported frames |
| Stage4 Node3 static floor surface for representative Titan stance | PASS (static only) | `UnityTitanNode3SurfacePreflight_20261020.md`: **88/88**, flat floor, no gaps |
| Canonical Van Aert keyboard/joystick movement, camera and combat | **NOT TESTED** | Neither canonical actor nor original input stack was activated |
| Titan V017 actual 4/4 physical sole contacts, frame28, 94%-reach, authored attacks and 98m slam waves | **NOT TESTED** | Original Titan and animations untouched; dedicated Stage4 V017 harness not present |
| Boss gauntlet, objective gates, vocabulary review and final ending | **NOT IMPLEMENTED / NOT TESTED** | Stage4 scene remains an environment/collision scaffold |
| Android A05s and PC FPS/memory | **NOT TESTED** | Editor in Android project target with DX12; no physical A05s launch or benchmark |

## Safety

- Current authoritative `spellstrike_save.json` SHA-256: `474D570C6F865BDCC9E84C3AED0E61A07C5440A8B9EAA6E7ED7A08DE40816246`, **exactly equal** to the dedicated pre-runtime backup in `Library/Stage4ConvergenceV003_RuntimeQA_20261020` after all Play Mode runs.
- Project original `SampleScene`, `Stage2Scene`, `Stage3Scene` and canonical `VanAertPlayer.prefab` were **not changed in this pass**. Do not infer their baseline was clean: they already contained other authored modifications. Neither any original prefab nor Blender/GLB source was overwritten. Existing save/player progression was not reset. This validation never changed global build settings or project-wide navigation defaults.
- Unity source code changes are confined to new Stage4-targeted Editor QA scripts and the new `Assets/Scripts/OptimizationTests/Stage4V003RuntimeTraversalProbe.cs` (used only in the separate RuntimeQA scene). A compile/reload occurred through the visible Unity Editor; no hidden Unity MCP invocation was used.

## Remaining acceptance gates before production integration

1. **Stage4 Titan V017-only dedicated test scene** cloned from the validation scene (not the existing bridge-traversal RuntimeQA), with save-state protection. Execute the original V017 solver/animation four actual LBS right-sole contacts, 0.30/0.35 m stance gates, frame28 support, 94% reach, real HP5/3/1 hit/miss, wave radii to 98m and visual side/front captures. Stage3's historical 4/4 pass is not proof on Node3 Stage4 geometry. The Titan is too wide for the narrower Stage4 bridges; it belongs in Node3 alone.
2. Genuine player keyboard/controller/Android touch traversal and camera plus under-player staging, including Start-to-Node1 and Node3-to-finale around gates, without changing canonical player dimensions.
3. Stage4 gameplay production-authoring pass: reuse original three bosses, build Vander Poel-specific rig, animations, AI and VFX, objectives and sequential bridge gates, stage review and final cutscene. Authorize changes to StageSelect/build indices/saves separately only after backups; no migration/reset is part of the current QA.
4. Full Unity Game View camera/light/water/ORM/portal VFX aesthetic review, PC benchmark and physical Samsung A05s FPS, memory, startup, gameplay and persistence regression before defense-ready signoff.

**Current status:** The user-requested bounded **Stage4 environment import / geometry / LOD / player-scale collision and navigation acceptance** now has complete, honest PASS/FAIL/NOT TESTED evidence. The larger gameplay and final-boss completion project is **not yet finished**.
