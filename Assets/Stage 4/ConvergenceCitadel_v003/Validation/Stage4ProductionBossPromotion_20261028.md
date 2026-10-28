# Stage 4 production boss promotion | October 28, 2026

## Status: SAVED TO ACTUAL PRODUCTION SCENE, PERSISTED EDITOR QA PASSED

- **Production destination:** `Assets/Scenes/Stage4Scene/Stage4Scene.unity`
- **Scene GUID:** `b69985f54b0efbb40b3bb1ff68432600` (unchanged)
- **Build Settings:** index **14**, enabled (unchanged)
- **Post-promotion production SHA-256:** `96310EDE2D4067AFA2D673ABDBB7A227CE36FDE83971F271AF42B0D27FA14FA0` (587,401 bytes)
- **Immutable original backup:** `RecoveryBackups/Stage4BossEncounters_ProductionPromotion_20261028_Immutable_v01/Stage4Scene_BEFORE_BOSSES_20261028.unity`, read-only. SHA-256 `7EA2C51E8D00E29E04491DBB876688FB5CD197B36F613E5E89B71A38321E506E`.
- **Original .meta backup:** the same path followed by `.meta`, read-only. Both original and backed-up meta hash `CD76014AEFF7720E624B489EB9B7567D8BE859DAF3AEB59050B4DE62C251086E`.
- **Source preview unchanged:** `Assets/Scenes/Stage4Scene/Stage4_BossEncounters_Preview_20261027_v03.unity`, SHA `A4B5D3E9BF76AC2753460DA25E90810EF35AD9DFDE36DCA5B198499D17D11448`. v01/v02 previews unchanged and match original SHA.

## Migration

The existing 307-object/394-component v03 encounter subtree was cloned into **the actual production scene**, with root renamed `_Stage4BossEncounters_PRODUCTION`. No map, spawn marker, player, HUD, camera, or existing manager was replaced. Nineteen serialized references to original preview-scene player/progression/arena marker objects were rebound by exact scene-root/hierarchy/component mapping to the original production equivalents.

One initial guarded migration stopped **before saving** because a validator mistakenly counted boss body BoxColliders as bridge gates. Its added clone was removed; the original production scene remained clean and its original SHA matched the backup. The corrected scoped validator counted only `LockedBridgeGates`, then production migration succeeded and the saved production scene was reopened.

## Read-back QA on the **saved, reloaded** Stage4Scene

| Encounter | Production actor | Spawn (world XYZ) | Components |
| --- | --- | --- | --- |
| Node 1 | `STAGE4_Node1_HollowKnight` | (-276, 6, -166.5) | 1 EnemyHealth / 1 Animator / 1 NavMeshAgent |
| Node 2 | `STAGE4_Node2_DrownedCaptain` | (-624, 12, -343.5) | 1 EnemyHealth / 1 Animator / 1 NavMeshAgent |
| Node 3 | `STAGE4_Node3_TitanEarthbreaker` | (-1280, 18, -620) | 1 EnemyHealth / 1 Animator / 1 NavMeshAgent |
| Finale | `STAGE4_Final_VanderPoel_PREVIEW_AI` | (-2020, 30, -1040) | 1 EnemyHealth / 1 Animator / 1 NavMeshAgent |

- Four `StageNodeController` components, indices 1–4, each `completeNodeOnEnemyDefeat=true`, each bound to the canonical **production** `Stage4ProgressionManager`.
- Four `StageNodeTrigger` trigger colliders, indices 1–4, each bound to the correct local actor and local progression manager. Node3 `StartAwakening` targets the original Titan's **child** `S3N4_Titan_EarthbreakerV017_BOSS` controller; the other nodes use `StartBattle`.
- Three active, non-trigger bridge gate BoxColliders, Gate 02 / 03 / 04, all bound to the local `Stage4BridgeGateBinderV01` and original progression manager.
- **89 in-scene serialized object references; 0 foreign-scene references; 0 missing scripts.** Titan's saved `player` and `defeatReceiver` point to the actual production Stage4Scene, not Stage3 or the v03 preview.
- **Map unchanged:** 11 LODGroups; 21 map MeshColliders, 20 enabled; `Independent_StaticMeshColliders` contains 20 colliders, 19 enabled; the separate enabled `Bridge_Node1_Node2_wood_light` remains.
- Exactly one player `PlayerHealth`, one `StageProgressionManager`; original HUD/camera and protected manager roots retained without duplication.
- Unity Editor ended with **Stage4Scene sole loaded, active, clean**, no Play Mode, compile/update busy state or VALORANT process; Unity Console **0 errors** after reopening.

## Not yet approved or tested

No Play Mode smoke test, real combat hit/death/gate unlock demonstration, AI NavMesh runtime, collision traversal, Android performance, or actual end-to-end boss gauntlet QA was performed. **Vander Poel remains the explicit temporary `Stage4VanderPoelPreviewEncounterV01` consultation AI**, not a production-final boss combat implementation. These are follow-up functional acceptance requirements; this report establishes accurate production scene integration and serialized persistence only.
