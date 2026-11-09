# Titan V017 motion integration: isolated observational QA

## Scope and ownership

`Assets/Stage 3/Scripts/TitanV017MotionIntegrationQA.cs` is an **opt-in, read-only observer** for the isolated Titan V017 test arena. It is intentionally **not attached to any scene or prefab by this change**. Prime owns attaching and exercising it in Unity. No motion, target, constraint, weight, source clip, Animator state, or gameplay component is modified by the observer.

This diagnostic never declares **PASS**. The original eight Animator state names it recognizes are:

1. `DormantStatueV004`
2. `AwakeningV004`
3. `AwakenedHoldV004`
4. `ColossalHeavyWalk`
5. `HammerfistV003`
6. `EarthbreakerSlamV003`
7. `TitanDeathFromRightPreview`
8. `ColossalStompRightFootV017`

An observed Animator state or valid clip reference **does not prove that the user actually saw a meaningful animation**. Visual evidence must come from the Game view or a recording.

## Attach and configure in the isolated arena only

1. Add `TitanV017MotionIntegrationQA` to the root of the **isolated** `TitanV017_ProceduralAttention_QA` instance in `Titan_TestArena/Titan_V017_SoloTestArena.unity`, not the production prefab, original Stage 3 scene, or source animation.
2. Set `Titan Animator` to the live animated Titan child. Set `Observed Root` to the outer wrapper if measuring actor/wrapper motion; otherwise the component defaults to the Animator transform. Set `Van Upper Chest` to the moving Van `mixamorig:Spine2` bone or allow a read-only lookup of the `Player` tag. Set `Face Forward Landmark` only when its actual visual forward direction is calibrated. Without one, head `+Z` is clearly reported as a **bone-axis proxy**.
3. Set `Ground Layers` to terrain/collision floor only, avoiding player, props, rig colliders and triggers. The observer ignores Titan colliders below its Animator. Optionally assign actual calibrated `Left Sole Marker` and `Right Sole Marker`. Leave `Sole Markers Calibrated` off until these are checked against the skinned underside; the fallback `ToeBase` bone is not a mesh sole.
4. Enable the `Capture Telemetry` checkbox. It defaults off to avoid spamming console or changing any existing QA workflow. Default periodic sampling is every **1 second**; state changes, replays, new transitions, and the four V017 impact-window timings are reported immediately. Stop/disable the component to print coverage over all eight states.

If mesh data is readable, the component uses the existing `internal TitanV017DeformedSoleLbs` sampler in the same `Assembly-CSharp` script assembly. It measures the **single minimum-Y, foot-weighted skinned vertex** on each foot, using world bind-pose transforms. It also raycasts under that vertex. This is stronger than reading an ankle transform but **cannot prove a whole sole contact patch, correct heel/ball/toe placement, or GPU render visibility**. If mesh data cannot be read, it emits an explicit warning and logs only bone/marker proxies. If the animation is being modified by IK, observed feet are **post-rig poses**, not an unmodified native clip measurement; set the rig weights to zero in a separate, approved diagnostic comparison to study native animation.

## Console evidence

Every diagnostic line contains UTC timestamp (`yyyy-MM-ddTHH:mm:ss.fffZ`) and Unity frame number. It reports:

- `STATE-ENTER`, `TRANSITION`, `REPLAY`, `STATE-EXIT`, and `STATE-STOPPED`, with full-path state hash, normalized time, state length, loop flag, active clip names/weights, and maximum observed root/pelvis/neck/Spine2 movement or rotation. State transitions are logged only when they change; periodic snapshots are throttled.
- `SAMPLE` observed Animator time and motion, signed world **bone +Z axis pitch** for pelvis/Spine2/neck and Spine2 yaw relative to the selected actor root, left toe/foot proxy planar drift from its reference position, right/left proxy heights and downward ray gaps, calibrated-marker status, actual skinned lowest vertices and their ground gaps when available, plus current first/second rig layer weights, left/right TwoBoneIK weights, chest/neck MultiAim weights and real-Van face/neck/chest angular errors. The bones' local +Z axes are not guaranteed to be the anatomical chest or eyes' visual forward directions.
- `ANCHOR-OBSERVED` at **frame 28 or later** for left support during V017. Frames 15–27 are the original preparatory step and must not be treated as planted support. Other states use their state-entry foot position as a movement reference.
- `IMPACT-WINDOW-OBSERVED` as the animation crosses source frames **53, 93, 133 and 176**, at approximately **1.7333, 3.0667, 4.4000 and 5.8333 seconds** into the V017 clip. The marker is a timing checkpoint, **not proof of contact**. If the frame sample is more than 1/30 second late, it warns about timing uncertainty.
- `EIGHT-STATE-COVERAGE` on disable: whether the component saw entries for each of eight states and how many frames it observed. This does not declare eight successful animations.

Any weight, bone, mesh or ground result marked `NA` is genuinely unavailable. A zero current constraint weight is shown as `0.00`, so unavailable and inactive rigging are not confused. State summaries always end with an **observed-only** qualification. Missing ground hits, missing readable skinned mesh, missing real Van chest and missing rig references produce explicit warnings.

## Prime-owned acceptance procedure

First capture the eight animations individually in the isolated full-body Game view, with fixed front and side cameras where possible. Select each of the seven menu items, then exercise the V017 stomp through its separately owned preview controls. Record state entry/exit, actual sampled bone motion, normalized time progression and recognizable animation silhouettes. In particular, verify that `ColossalHeavyWalk` remains in walking state and traverses its pose cycle, while one-shot animations reach their intended ending poses. An unchanged or looping idle pose during an attack is a user-visible failure regardless of attached clips.

For natural gaze, have Van move at front/side and different ranges. Compare `Head+Z_BONE_ONLY` with the **visually calibrated** face direction and note the chest and neck local rotation deltas, current MultiAim weights and lag. Record the user-visible head/chest silhouette independently of a high `neck.dot(target)` statistic. **Do not change the user's frozen natural-gaze constraints or weights as part of this QA.**

For V017 support and stomps, inspect the original left preparatory step (frames 15–27), left support from frame 28, the four raised right-foot descents, and actual ground contact from the camera. At each impact inspect the timestamped rig weights, vertical gaps, target/foot behavior and mesh-extremum samples. Existing preview IK remains the authority for its own detailed reach and contact abort logs; the observer neither modifies it nor replaces its multi-condition acceptance gates. Do not infer that a lowest vertex near ground means the whole 40x Titan foot is physically planted. If no readable mesh is available or a ground ray fails, record the measurement as unavailable.

At the end, classify each animation and biomechanical requirement using **real observed visuals plus runtime evidence**, recording `PASS`, `FAIL`, or `NOT TESTED` outside this component. The component produces observations, never automatic success verdicts.

## Limitations and checks before accepting telemetry

The diagnostic reads the posed hierarchy in `LateUpdate` with a late script execution order; verify the particular Unity/RigBuilder evaluation order if a measurement appears one frame behind. Scene-specific animator states may switch or be externally interrupted. Animations with static design, including Dormant/Hold, are expected to show little root movement, and motion deltas alone do not determine whether these clips are artistically correct. Raycasts ignore the Titan Animator hierarchy but can still hit props or Van when the layer mask is too broad; use a terrain-only layer. An extremum vertex can switch between toes and heel as the mesh rotates, making its horizontal position unstable even when contact area is stable. The wrapper root and Animator root may differ, so record which root was assigned. Tests against source V017 are still required to distinguish authored mocap from procedural IK motion.

No Unity Editor/Play action, compilation, scene attachment, production modification or runtime validation was performed when this file was authored. Prime must run the component in the isolated arena and review its output before making a PASS claim.
