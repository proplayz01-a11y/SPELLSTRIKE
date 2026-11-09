# Stage 3 Titan V017: integration and QA record (2026-10-13)

**Status: PARTIAL PRODUCTION INTEGRATION. NOT FULL ENCOUNTER PASS.**

This page is the source-of-truth navigation aid for the October 13 V017 work. Do not confuse historical review-scene success with actual AI-driven combat success.

## Canonical versus review/experimental assets

| Role | Unity asset | Rule |
| --- | --- | --- |
| **Production scene** | `Assets/Scenes/Stage3Scene/Stage3Scene.unity` | Only canonical Stage 3 for gameplay/build validation |
| **Production prefab** | `Assets/Stage 3/Enemies/STAGE 3 BOSS - TITAN/Animation_V017/S3N4_TitanV017_ProductionRig.prefab` | Prefab and rig source; not changed for the collider experiment |
| **Production Animator** | `Assets/Stage 3/Enemies/STAGE 3 BOSS - TITAN/Animation_V017/Titan_V017_Stage3Boss.controller` | Contains V017 state plus legacy Awakening, walk, Hammerfist and Earthbreaker Slam |
| **Ground-contact QA scene** | `Assets/Stage 3/Enemies/STAGE 3 BOSS - TITAN/Animation_V017/Titan_Node4_V017_ProductionTerrainQA_20261013.unity` | Copy of real Stage 3 map and NavMesh; baseline physical collider restored after QA |
| **Collider experiment** | `Assets/Stage 3/Enemies/STAGE 3 BOSS - TITAN/Animation_V017/QA_ColliderExperiments/Titan_Node4_V017_ColliderSplit_20261013.unity` | **Experimental only. Not production-approved.** Broad hit trigger separated from narrow physical collider |
| V016 legacy review | `Assets/Stage 3/Enemies/STAGE 3 BOSS - TITAN/Animation_V004/Titan_Node4_V016_ColossalStompReview.unity` | Preserve, including any user-authored unsaved state |
| V017 flat-ground animation review | `Assets/Stage 3/Enemies/STAGE 3 BOSS - TITAN/Animation_V017/Titan_Node4_V017_ColossalStompReview.unity` | Historical source for isolated mesh-contact proof, not production boss QA |

Do not move old Unity assets via filesystem operations or bulk-delete backup scenes. Preserve `.meta` GUIDs and serialized references. The `Mocap_V017_ColossalStomp_Force_Workbench/` folder holds source animation artifacts, diagnostic snippets, and `/_BACKUPS_20261013/` copies of the canonical scene and `StageNodeTrigger.cs` from before the October 13 changes.

## Changes installed October 13

1. `Stage3Scene.unity`: assigned the **existing** `Assets/Scenes/Stage3Scene/Stage3Scene/NavMesh-Plane.001.asset` to the active `Plane.001` NavMeshSurface. The asset was already baked but the surface had a null `NavMeshData` reference. Verified loaded triangulation **8,770 triangles**. The disabled legacy map surface remains untouched.
2. `Stage3Scene.unity`: positioned the instantiated V017 production-rig root at **(-681, 2.89, 202.5)** instead of **(-690, 2.8683, 213)**. The QA scene established that the first right/left rendered soles and frame-28 left support are grounded at this stationary authored orientation. This **does not** solve foot contact after navigation/turning.
3. `Assets/Scripts/StageNodeTrigger.cs`: when a Node 4 `Stage3RiddleController.BeginRiddleTrial()` refuses initialization, the entry latch is released and `OnTriggerStay` may retry after one second. Nodes 1-3 and other receivers retain the existing generic `StartBattle` message path. Unity standard script validation returned zero errors/warnings; confirmed the new field was loaded in the running compiled assembly.
4. Created a separate physical-collider QA experiment scene without changing the production prefab or production collider. Restored the baseline terrain QA scene after copying the experiment.

## Verified tests and evidence

- **PASS, isolated V017 on actual Stage 3 terrain:** The baseline terrain QA copy with a stationary root, fixed QA aim reference near `(-688.70, 2.919, 194.35)`, real RigBuilder, and deformed mesh LBS reported `4/4` physical right-sole contacts, planted left support, and `LastStompPassed=true`. Final reported right deformed-mesh error was **0.072 m planar / 0.001 m vertical**. No damage or crowd control applied. The earlier flat-ground QA also independently reported 4/4.
- **PASS, Node 4 physics entry in terrain QA:** Existing Stage 3 runtime QA bootstrap completed Nodes 1-3 and unlocked Node 4, then the actual player CharacterController entered the authored BoxCollider. Unity logged `Riddle 1/4 active` and `Four-riddle trial started`; TileManager and HUD initialized.
- **PASS, canonical Node 4 retry regression:** During canonical Play Mode, deliberately blanking first-riddle answer caused a clean startup refusal, tile restoration and movement unlock. Restoring the answer to `WIND` while Van remained inside the BoxCollider caused a new successful riddle startup. Console confirmed `Riddle startup declined. Trigger remains retryable.` followed by `Riddle trial started.` Runtime-only test edits were not saved.
- **PASS, limited AI smoke in collider experimental QA:** Manually requested QA awakening, and Unity reported Awakening completion, NavMesh-enabled combat and Heavy Walk startup. This is **not** a four-riddle-to-Awakening end-to-end pass.
- **FAIL, dynamic AI V017 grounding:** Following natural navigation and turn, the V017 solver denied startup for unequal or ungrounded first soles. The experimental QA actor moved from `(-681, 2.89, 202.5)` to approximately `(-661.36, 2.89, 205.77)` and yaw changed from about **154.4° to 235.5°**. Stationary placement alone cannot guarantee grounded feet throughout combat.
- **FAIL, attacks beyond current body collider:** Existing physical capsule has an approximate **21.21 m** world-space radius and Van's CharacterController has **5 m** world-space XZ half-width. Production V017 automatic attack range is **18 m**, so the physical separation and initiation thresholds conflict. QA ground targets about 22-23 m from the root failed. One explicit denial recorded **30.219 m required reach against 28.618 m maximum** (94% anatomical cap). A roughly 16 m fixed target also failed physical contact. **Do not raise the 94% cap or claim these attempts passed.**

## Remaining acceptance blockers, in priority order

1. **P0: Physical target/engagement geometry.** Preserve the large combat hitbox and reliable projectile collisions, but design separate anatomically appropriate body/leg blocking shapes or a reachable, visually honest V017 landing policy. Validate the full-sized Van collision radius. Do not silently shrink the production hitbox or use fake timed contacts.
2. **P0: Moving Titan stance calibration.** Add a guarded pre-stomp approach/stance stage to acquire navigable ground where BOTH first-frame soles and the frame-28 left foot remain genuinely grounded at the final Titan orientation. Freeze XZ/yaw during the committed four-stomp move. Avoid abrupt teleports and do not use raised platforms outside leg reach. Recheck slope and support on real Boss_Stage colliders.
3. **P0: Full production playthrough.** Real Nodes 1-3 progression -> Node 4 BoxCollider -> all four riddles using the normal UI and physical magic projectile -> final sky-blue Titan laser -> Awakening -> Heavy Walk -> Hammerfist/Earthbreaker Slam -> four V017 deformed-mesh contacts -> safe return to chase. This is **not yet passed**; automatic riddle-submission QA was blocked, and no synthetic impact is a substitute for projectile contact.
4. **P1: Defeat completion.** `TitanEarthbreakerController.TriggerDeath()` holds pose; `OnDeathFinished()` is the only final Node 4 defeat notification. Wire it when the actual approved Death animation/event exists; do not invent a clip or force an unapproved completion.
5. **P2: Workspace organization.** Keep production/review/experiments clearly separated, index QA evidence, and only use Unity-aware moves after functionality is green. There are many unrelated dirty/untracked files across the repository: no `git reset --hard`, `git add .`, broad delete, or careless reimport.

## Current safe default

Production V017 is wired and guarded by physical contact validation; it has **no new HP or crowd-control effects**. The attempted move fails closed rather than inventing four hits. The canonical scene remains the build scene, and the editor should be left focused on it, not an isolated review or collider experiment.
