# Titan V017: actual Unity Play QA after agent sprint

**Date:** 2026-10-14 (Asia/Manila). **Tester:** Prime through live Unity 6.3 LTS solo-arena Play, Game View and original skinned-mesh/Animation Rigging verifier logs. **Scope:** isolated `Animation_V017/Titan_TestArena/Titan_V017_SoloTestArena.unity`. Production boss gameplay and physical Android device NOT tested.

## Outcome: six consecutive full V017 runs passed

The scene-only `TitanV017ArenaStompRegressionQA` component was installed through `TitanV017SoloRegressionMenu.Install` after Unity code compiled. Unity launched via **Play SOLO Titan Arena (isolated)**; the original native 8.767s V017 animation was replayed using the existing post-rig physical contact verifier in two independent 3-run batches. No artificial collision/contact overrides and no change to the anatomical reach 0.94 fraction.

| Replay | Verified right actual LBS mesh contact events | Native left support sole | Verifier full 8.767s outcome |
| --- | --- | --- | --- |
| Batch 1 / Run 1 | 4/4 | Grounded at sampled contact frames | PASS |
| Batch 1 / Run 2 | 4/4 | Grounded at sampled contact frames | PASS |
| Batch 1 / Run 3 | 4/4 | Grounded at sampled contact frames | PASS |
| Batch 2 / Run 1 | 4/4 | Grounded at sampled contact frames | PASS |
| Batch 2 / Run 2 | 4/4 | Grounded at sampled contact frames | PASS |
| Batch 2 / Run 3 | 4/4 | Grounded at sampled contact frames | PASS |

**Aggregate 6/6 completed replays PASS, 24/24 independently registered right-foot contacts.** No `[V017 RECOVERY IK GUARD]` events or >94%-reach aborts in these **six current trials**. Historical old-patch aborts remain visible farther back in the same Editor.log; do not misattribute them to the new six-run test.

Evidence from first run (Unity Editor log, original `S3N4_Titan_OpenStoneHands_Skinned` CPU LBS geometry, 1548 right / 2198 left vertices sampled):

- Right impact #1: planar `0.023m`, height `0.003m`; left sole planar `0.006m`, height `0.000m`.
- Right impact #2: planar `0.059m`, height `0.023m`; left sole planar `0.007m`, height `0.000m`.
- Right impact #3: planar `0.023m`, height `0.007m`; left sole planar `0.005m`, height `0.001m`.
- Right impact #4: planar `0.066m`, height `0.001m`; left sole planar `0.006m`, height `0.001m`.

The source solver logs `[V017 Stomp QA] PASS V017: four REAL right-sole contacts, planted left sole, zero gameplay effects.` at the original clip's natural end. The regression runner then logs `Run N/3: PASS 4/4 physical contacts` and `OVERALL PASS (3/3 complete 4/4 replays)` per batch.

**Precision note:** The source LBS contact gate uses actual deformed-geometry sole extrema and carefully calibrated right/left marker positions. These are physical-location evidence and are far more meaningful than playing contact sounds on a timeline; however, single mesh extremum samples still do not certify the full toe/heel contact *patch* or subjective freedom from foot pop. The test does not cover frame-rate variability, moving Van throughout stomps, gameplay damage, or production NavMesh/collider integration.

## Actual Game View original animation rehearsal

The arena's manual selector `TitanV017ArenaAnimationMenuQA` was exercised in Play while the Animation Rigging components remained active:

| Action | Observed result |
| --- | --- |
| Heavy Walk button | Walk animation visibly cycled and remained selected beyond one source loop, without unwanted return to Hold |
| Native Hammerfist trigger | Entered original raised-arm/attack pose, then the selector displayed `HammerfistV003 completed; authored recovery to Hold` after the one-shot |
| Native Earthbreaker Slam trigger | Entered source attack pose, then selector displayed `EarthbreakerSlamV003 completed; authored recovery to Hold` |
| Native Awaken trigger | Dormant→Awakening played, selector displayed `AwakeningV004 completed; authored recovery to Hold` |
| Death preview button | Entered lying/fallen death pose; manually reset with Awakened Idle button (preview only, not actual health-driven death) |

No unexpected Animator early-exit, never-entered-state, or new Titan script exception was observed in the checked log window. Prior Head/Chest aim runtime logs still demonstrate actual bone rotation, but **the user-frozen natural visual gaze FAIL stands**, and this sprint did not retune it.

## Console, file and scene safety

- Unity 6.3 LTS compilation completed and the new QA menu operated correctly. The previously reported `CharacterController.isActiveAndEnabled` compiler error is corrected in source, and no current script compile failures were seen.
- The editor's retained unrelated red error is from **Unity Remote Android** trying `adb.exe forward tcp:7201 tcp:7201` with **no devices/emulators found**. This is an Editor/mobile-remote connection issue, not evidence of a Titan animation exception; it is not equivalent to a zero-error project-wide audit.
- `Assets/Scenes/Stage3Scene/Stage3Scene.unity` SHA-256 still equals `Temp/TitanSoloPlaySafety/Stage3Scene_PreTitanSoloPlay_20261013.unity`. The original Astra `Titan_ColossalStompFourRight_V017_CrushReview.anim` SHA still equals approved `8F19397D906A0E8437130AA43188A6B14E026255DAF013A695F4E45593B5713B`.
- One-shot fallback request was disabled and its temporary prototype source removed. The stable opt-in **three-run QA menu and scene component** are retained for future regression checks. Unity Play was restored to `AwakenedHoldV004`, solo arena only.

## Not accepted in production yet

1. **Stage3 collision/stance:** solid Titan capsule ~21.21m radial clearance versus original stomp sole target ~11.21m from root; fail-closed preflight still rejects Van's legitimate physical position. Native walk-to-stance planner is not complete.
2. **Production Animator config differs:** current Stage3 production controller still has V017 `Write Defaults=OFF` whereas proven solo controller's V017 state is `ON`. Production left-foot IK fallback also differs. A successful solo result cannot be assigned to production automatically.
3. **Production NavMeshAgent currently disabled/unplaced**, so chase/attack lifecycle hasn't been exercised end-to-end; damage and crowd control remain deliberately absent from the V017 preview.
4. **Visual acceptance:** Head/Chest Aim tuning frozen per user; detailed left-foot/hip appearance, possible right foot recovery pops, animation-to-combat feel and moving-target stress tests still need the user's front/side Play review.

**Conclusion:** In the actual isolated Unity arena, the new V017 procedural IK recovery fix passed six consecutive real original full-clip replays with 24 verified contacts, and tested original animation triggers/recovery worked. This establishes a validated **solo arena animation/IK baseline**, not finished Stage3 production boss AI.

---

## Later October 14 production-geometry integration update (supersedes the older "Not accepted" section above)

The earlier section above is retained as a historical snapshot. Following the user's explicit request to get all four stomps into Stage 3, the actual Stage3 level was run with its real player, `Stage3NavMeshRuntimeBootstrap`, boss lifecycle, full Titan animation, new physical collider candidate and independent WD-ON Animator controller. The QA command runs only inside Unity Editor Play and transports the existing real Van controller **once before encounter startup**; the full ensuing movement, aiming, procedural feet, collision tests and original clip are not scripted or simulated. Testing this shortcut is **not** certification of finishing the fourth riddle through normal gameplay.

**Critical physical failure found and repaired:** Stage3 Van has a large scaled CharacterController (measured QA world radius about 5m). Targeting his collider's center made the Titan's own anatomical stance overlap his physical body. The planner now ground-rays a **near-side, outside-capsule landing rim** plus the existing 0.5m safety gap, preserves the original authored right-foot offset, navigates using the genuine NavMesh, and fixes the marker at attack commitment. The foot's grounded impact is NOT a fabricated hit against Van's body and V017 does not inflict health or CC effects. The original giant full-body blocking capsule was replaced **in a separately created production candidate prefab** by an elevated solid torso and four bone-attached solid upper/lower leg capsules; a separate trigger retains projectile hurtbox coverage. No player collision disabling, animation clip editing, hit fabrication or 94%-reach bypass was used.

**Strict left-support floor bug:** The first production-shaped Play reached V017 but aborted at frame28. Fail-only telemetry demonstrated true native/LBS left sole `y=2.8935` above genuine `Boss_Stage` ground `y=2.1688`, a ~0.725m air gap, with no root displacement. It was a 0.700m elevation step, **not** a bad IK evaluator or a NavMesh jump. `TitanV017StompStancePreflight` now fails candidates with actual left/right floor difference above **0.30m**, stricter than the unchanged **0.35m** planted-mesh requirement. The Editor-only Stage3 QA scenario searches the actual level for a naturally valid stance and refuses startup if none exists. The final valid position was Van `(-691.852,2.954,190.405)`, proposed Titan navigation root `(-680.696,1.691,202.839)`, and planned grounded right-foot rim `(-688.179,2.919,194.499)`. The moving Titan revalidated and committed actual ground at approximately `(-688.54,2.92,193.94)`.

### Three successful full original V017 Stage 3 replays

| Environment | Tested full 8.767s clips | 4 native right LBS contacts | Left native prep/frame28 & grounded support | Outcome |
| --- | ---: | ---: | --- | --- |
| Independently copied Stage3 staging scene, repeat #1 | 1 | 4/4 | Plant accepted and continuously grounded at impacts | PASS |
| Same Stage3 staging scene, repeat #2 | 1 | 4/4 | Plant accepted and continuously grounded at impacts | PASS |
| **Actual `Assets/Scenes/Stage3Scene/Stage3Scene.unity`**, after clean disk reload | **1** | **4/4** | **Plant accepted and grounded at impacts** | **PASS** |

**Aggregate Stage3: 3/3 completed, 12/12 real deformed-mesh right impacts, zero scripted V017 damage/CC.** The actual original Stage3 run logged:

- `#1 right LBS planar=0.024m height=0.002m; left height=0.000m`.
- `#2 right LBS planar=0.063m height=0.022m; left height=0.000m`.
- `#3 right LBS planar=0.021m height=0.006m; left height=0.006m`.
- `#4 right LBS planar=0.068m height=0.001m; left height=0.001m`.
- `[V017 Stomp QA] PASS V017: four REAL right-sole contacts, planted left sole, zero gameplay effects.`
- `[Titan V017] Four post-rig physical foot contacts verified. No HP/CC.`

All four are from actual `TitanV017DeformedSoleLbs` geometry, after natural Awakening -> HeavyWalk -> bounded NavMesh approach -> original V017 -> return to combat. The solver still enforces 94% anatomical IK reach and the genuine 8.767s natural clip completion. Original `.anim` SHA-256 remains `8F19397D906A0E8437130AA43188A6B14E026255DAF013A695F4E45593B5713B`.

### Deliberate original production-scene promotion and backup

The pre-promotion original `Assets/Scenes/Stage3Scene/Stage3Scene.unity` was SHA-256 `0208F2EFF48C33754573C743F25C929930EA206EEBF2729C3027F2C096B4EA37`, byte-identical to the earlier October 13 backup. It was backed up **before** intentional promotion as:

`Temp/TitanSoloPlaySafety/Stage3Scene_BeforeV017PhysicalStompsPromotion_20261014.unity`

Only **189 Unity YAML references** to the original Titan rig prefab GUID `fa874462bcaa5414bb81a3bc36339132` were replaced with the isolated, tested physical-combat candidate prefab GUID `a213f73498837b94bb00bd784b4b089c`. The final production scene is byte-for-byte equal to the successful staging scene, with SHA-256 `35FFB162CF563703F507AE196F331D54BD101AE1014DE994AF45547E29299AF3`. Node4, other scenes, all authored animations and the existing Stone Golem were not changed by this swap. Unity Editor was intentionally instructed to reload the saved original Stage3 scene so its cached old, broad collider would not cause a false production test. Unsaved scenes were safeguarded by checks, and the official production run then passed 4/4.

Useful Editor menu shortcuts (while Stage3 is clean in Edit Mode): `Ctrl+Shift+Alt+Y` **Play REAL Stage3**; after its NavMesh bootstrap completes, `Ctrl+Shift+Alt+O` **QA Stage3 Play: Position real Van and Awaken**. These are deliberately Editor-only QA affordances, not in-game attack triggers, contact generators or level progression.

**Remaining acceptance boundaries:** the user should visually review full-foot biomechanics and contact feel; the real fourth-riddle-triggered awakening, moving Van through all phases, damage/death progression, projectile collisions, device builds and non-ideal terrain variations still require separate end-to-end QA. The V017 move remains non-damaging by explicit preview safety design. The user-frozen Head/Chest artistic tuning was not changed. Do not relabel these other workstreams as passed merely because the original four-stomp animation now plays successfully in production Stage3.
