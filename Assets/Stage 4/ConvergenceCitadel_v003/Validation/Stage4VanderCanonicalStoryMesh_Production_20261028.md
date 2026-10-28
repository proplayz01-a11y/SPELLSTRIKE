# Stage4 Vander Poel canonical story-mesh integration | 2026-10-28

## Production result

- **Saved into the actual production scene:** `Assets/Scenes/Stage4Scene/Stage4Scene.unity`
- **Post-swap SHA-256:** `339F887D488D93D34B981BAC17943AF0399B2627FFFD90E631F87A82367E15E2` (654,932 bytes)
- **Stage4 scene GUID preserved:** `b69985f54b0efbb40b3bb1ff68432600`
- **Build Settings:** index **14**, enabled and unchanged
- **Immutable pre-swap backup:** `RecoveryBackups/Stage4VanderStoryVisualSwap_20261028_Immutable_v01/Stage4Scene_BEFORE_STORY_VANDER_20261028.unity` and its `.meta`, both read-only. Scene hash `96310EDE2D4067AFA2D673ABDBB7A227CE36FDE83971F271AF42B0D27FA14FA0`; meta hash `CD76014AEFF7720E624B489EB9B7567D8BE859DAF3AEB59050B4DE62C251086E`. Backup verified before and after mutation.

## Canonical appearance

- Replaced **only the active visual** of `STAGE4_Final_VanderPoel_PREVIEW_AI`. Functional boss root, `EnemyHealth`, `NavMeshAgent`, `CapsuleCollider`, `Stage4VanderPoelPreviewEncounterV01`, progression triggers, nodes, and gates remain intact.
- **Source of truth:** `Assets/Cinematics/Storyboard3D/Panels07_10_v02/Panel09/P09_VanderPoel_CinematicRefinement_Animated_v02.glb`, nested complete `P09_VanderPoel_GatewayStanding_Rig` skeleton.
- New active child: `VanderPoel_StoryP09_65BoneRig_ACTIVE`. Exact source `VanderPoel_Old_TPose_Model` skinned mesh, **19,225 vertices**, **65 non-null bone bindings** and matching source root bone, canonical `VanderPoel_Mixamo_Diffuse` material with supported `Shader Graphs/glTF-pbrMetallicRoughness` shader. No bones were transferred to the older, incompatible 52-bone FBX skeleton.
- Existing old visual `VanderPoel_VisualRig_Mixamo` (19,245 vertices / 52 bones) retained **inactive** under the same boss as rollback fallback, not deleted. The retained fallback's Animator is inactive; the new active visual has **no added Animator or Timeline**, and only one active skinned renderer exists.
- Original rig orientation retained and new story visual aligned to prior boss world bounds (24-unit height), without moving the boss root or altering collider configuration.
- A neutral standing posture was sampled statically from the canonical GLB's original short `Armature|mixamo.com|Layer0.002` animation at 0.04 seconds, applying rotation-only curves to the 65 source bones. All **65/65 bone localRotation values were verified persisted after reopening**.

## Saved-scene QA

- **Reopened the saved production scene** in Edit Mode and verified exact source `sharedMesh` asset identity, vertex count, 65/65 correctly linked bones, root bone, original story material, supported shader, renderer active state, intact inactive original fallback, and original functional components.
- Four total boss actors, four nodes, four triggers, and three solid bridge gates remain; one canonical player/progression manager and unchanged protected HUD/camera/manager roots.
- Protected production map invariants: 11 LODGroups; 21 map MeshColliders / 20 enabled; `Independent_StaticMeshColliders` retains **19 enabled**, including unchanged extra enabled bridge collider outside that subtree.
- The saved encounter subtree has **0 cross-scene object references and 0 missing scripts**. No Stage3, story source assets, prior v01/v02/v03 previews, scene GUID, or Build Settings edits.
- Unity remained in **Stage4Scene sole active, clean Edit Mode**; no VALORANT process. No Unity Console errors were returned after reopening; separate pre-existing menu conflict/MCP port/AI relay warnings were present.

## Visual evidence and known limitation

- Pre-save standing P09 pose, actual arena: `Assets/Stage 4/ConvergenceCitadel_v003/Validation/Screenshots/Stage4_StoryCanonicalVander_P09_ClipLayer002_PreSave_20261028.png`
- Saved/reopened scene viewport: `Assets/Stage 4/ConvergenceCitadel_v003/Validation/Screenshots/Stage4_StoryCanonicalVander_P09_PRODUCTION_SAVED_QA_SecondPass_20261028.png`
- **Important:** The saved 65 local bone rotations match the sampled short standing clip exactly, but the **reopened Unity Scene View renders the skinned mesh in its imported T-pose**, rather than the pre-save standing preview. This is a visual/render-pose initialization issue not yet resolved. It does **not** change the verified exact source mesh/material/bone references. For consultation, account for this visible T-pose; a properly configured **65-bone idle/attack Animator** and Play Mode/Android rendering tests are still required before calling this a production-ready boss animation.
- Vander Poel still uses **temporary consultation preview AI**. No functional final-boss animation system, actual fight, gate unlock, Android optimization, or Play Mode smoke test was certified here.
