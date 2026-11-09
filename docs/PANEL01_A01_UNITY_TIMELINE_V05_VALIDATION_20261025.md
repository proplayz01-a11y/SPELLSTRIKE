# SPELLSTRIKE Panel 01 — Native Unity Timeline A01 (v05)

**Date:** 2026-10-25  
**Status:** Editor-playable, visually verified **defense rough-cut**. This does not indicate completed character acting, physical block contact, voice recording, or story-sequence integration.

## Open and preview

- Open `Assets/Scenes/Cinematics/Prologue_P01_FamilyHome_v05_A01_Timeline.unity` in Unity 6.3 LTS.
- Select `A01_TIMELINE__PEACE_BEFORE_THE_GREAT_WAR__10P5S` and open **Window > Sequencing > Timeline** to inspect the five native Timeline tracks.
- Press Play for an automatic **10.5-second** sequence with hold on the final frame.
- Timeline asset: `Assets/Cinematics/Storyboard3D/Timelines/Panel01_A01_v05/A01_PeaceBeforeGreatWar_v05.playable`.
- All new v05 `.anim` assets are next to the `.playable` file. Source Blender home, v04 scene, and preexisting imported character files were preserved.

## Tracks

| Track | Authored action | Limitations |
| --- | --- | --- |
| 01 CAMERA | Approved A01 32mm view, 10.5s eased 0.27m push | One medium-wide camera setup, no additional cut |
| 02 NEVALI | Existing v13 imported MediaPipe rough motion clip, approximately 8.0s retimed to 10.5s | 57-bone Generic FBX, rough pose; no hand-contact validation |
| 03 VAN | Static folded-arm v06 textured Young Van with a minimal 3-degree turn | Hands/gaze not yet realistically polished or full-body retargeted |
| 04 TOY BLOCK 09 | A Timeline `.anim` clip lifts, moves and lowers one colored block | **Approximate visual blocking only**; not a verified real grasp, rig attachment or collider-based contact |
| 05 NARRATION | Timeline ActivationTrack drives text overlay, with fade from director.time | Approved text only; no recorded audio asset |

Locked script: “Once upon a time, a wizard named Van Aert lived peacefully with his daughter, Nevali...”

## Recovery and runtime fixes

The first v05 Play Mode exposed a real Timeline root-motion/clip-offset bug: the camera jumped to world origin and rotated to zero while Young Van lost his world position and ended up lying down. A separate Generic FBX root-coordinate mismatch also shifted Nevali off the rug. These are now corrected in the **isolated v05 scene only**:

1. `Assets/Editor/Cinematics/Panel01A01TimelineAssembler.cs`: camera animation now has explicit quaternion curves and the father track has explicit constant local-position curves. Camera/father AnimationPlayableAsset `removeStartOffset=false` retains their staged world transforms. `Repair Existing A01 v05 Timeline Offsets` safely repaired the already-created versioned assets and scene.
2. `Assets/Scripts/Cinematics/Panel01TimelineStageAnchor.cs`: late runtime reset of Nevali's scene root to the validated v04 stage anchor `(-1.28,0.45,0.45)` while retaining her Timeline-driven bone curves. This is a rough-cut workaround for the imported Generic FBX root offset.
3. `Assets/Scripts/Cinematics/Panel01TimelineNarrationOverlay.cs`: separate text overlay driven by Timeline's director time; old `Panel01CinematicPlayback` is disabled in v05 to avoid competing camera systems.

## Verified Unity Play Mode evidence

- PlayableDirector duration **10.5 seconds**, **5 output tracks**, bindings to the correct camera, Nevali Animator, father, toy block, and narration object. `playOnAwake=true`, end hold enabled.
- Sampled time `0.1`, `3.5`, `7.0`, `10.2` seconds after repair: camera retained rotation `(9.19°,210.80°,0°)` while moving from `(2.75,2.05,5.20)` toward `(2.61,2.01,4.97)`; 0.27m total push by the end.
- Young Van remained upright at `(1.95,0,2.75)` and rotated gently around his original orientation `(90°,333°,0°)`.
- Nevali remained at `(-1.28,0.45,0.45)` with moving imported body/arm rig, visible sitting by the toy blocks.
- In Game View, the warm-colored v03 home, round rug, block arrangement, complete imported roof, new vase, and restrained upper-left narration text are present.
- Targeted Console check found unrelated Unity Remote/plugin services and Unity AI generator network errors. No Panel 01-specific exception was identified in the runtime check; **do not claim the whole project has a clean console**.
- v05 was left in clean Edit Mode after testing.

## Video and screenshots

Saved in `ArtSource/Cinematics/Panel01_v05/Reviews/`:

- `Panel01_A01_Timeline_Verified_03p5s.png`: recovered proper actor/camera staging from a live 3.5s Timeline evaluation, with narration overlay.
- `P01_A01_Timeline_ActualUnity_10p5s_DefensePreview.mp4`: real Unity Game View screenshot-based silent preview. Approximately **10.6 seconds**, **70 frames**, **1920×1080**, **6.6 FPS**. This low frame rate is due to screen capture overhead; it is a prototype playback proof, not an optimized 30 FPS renderer export.
- `P01_A01_Timeline_VideoQA_ContactSheet.png`: beginning/mid/end decoded MP4 frames; codec readback and visible frame differences validated.
- `Unity_Timeline_Frames/`: original 2400×1080 intermediate screenshot sequence for review/debug.

## Remaining acceptance gates

- Nevali's right hand was previously measured approximately **0.6m from the closest toy block** during motion sampling. The block lift in track 04 is visually suggestive; it does **not** establish convincing hand-to-block contact. True reaching/grabbing/placing would need reblocking/retargeting, hand IK and prop timing.
- Young Van's pose is visually readable, but fingers and exact gaze may require additional Blender source improvement.
- No recorded narration/audio/VO, secondary music, child sound effects, full Timeline video recorder at 30 FPS, Unity Android verification, cut to Panel 02, or production StoryScene/42-panel integration.
- The central timber post remains between the figures in the original doorway-layered composition; it does not create a solid wall or block the modeled hallway sightline.

Only the versioned v05 cinematic scene/Timeline files and new Panel01-specific helper scripts were modified. No production gameplay scenes, older isolated Panel 01 versions, source Blender v01-v03, shared prefab resources, input settings or Build Settings were intentionally changed.
