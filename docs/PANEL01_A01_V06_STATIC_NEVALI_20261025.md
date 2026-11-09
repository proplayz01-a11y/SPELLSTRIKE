# SPELLSTRIKE — Panel 01 A01 Timeline v06, static Nevali

Date: October 25, 2026 | Unity 6.3 LTS | Defense rough-cut revision

## Open and Play

- **Unity scene:** `Assets/Scenes/Cinematics/Prologue_P01_FamilyHome_v06_A01_StaticNevali.unity`
- **Timeline:** `Assets/Cinematics/Storyboard3D/Timelines/Panel01_A01_v06/A01_PeaceBeforeGreatWar_StaticNevali_v06.playable`
- **Frozen Nevali pose mesh:** `Assets/Cinematics/Storyboard3D/Characters/Panel01_v06/Nevali_v13_Frozen_Frame3p5_v06.asset`
- **Camera animation (reframed):** `Assets/Cinematics/Storyboard3D/Timelines/Panel01_A01_v06/A01_Camera_Thirds_Push027_v06.anim`
- **Full preview:** `ArtSource/Cinematics/Panel01_v06/Reviews/P01_A01_v06_StaticNevali_VanThirds_10p5s.mp4`

Open the isolated v06 scene, select `A01_TIMELINE__PEACE_BEFORE_THE_GREAT_WAR__10P5S`, and use **Window > Sequencing > Timeline** to inspect the three remaining tracks. Press **Play** for a 10.5-second preview. No edits were made to the working v05/v04 source scenes.

## User-directed corrections applied

1. **Nevali absolutely stationary, same existing pose and visual position.** Sampled the approved v05 seated v13 model at its Timeline **3.5-second** pose. Baked the exact 83,350-vertex skinned result into a new static MeshFilter/MeshRenderer asset. Geometry bounds matched the v05 skinned renderer with a **0.00000 m** difference in center and size. Disabled the old animated Nevali instance and removed her animation Timeline track. The visible frozen Nevali contains **no Animator**, skeleton playback or root-motion script.
2. **All toy blocks stationary.** Deleted `04 | TOY BLOCK 09 | Rough lift and place` from the new Timeline and removed block 09's Animator. Unity Play Mode measurements at 0, 2.5, 5.25, 8 and 10.5 seconds found **0.00000000 m displacement across all 20 wooden blocks**.
3. **Van keeps his subtle turn.** Reused the existing v05 Van motion clip, without modifying it. Play Mode showed the world yaw shift from approximately **333.00° to 329.76°** and return to 333.00°. His crossed-arms visual pose is maintained.
4. **Van on left rule-of-thirds line.** Copied, rather than modified, the original camera animation and shifted the shot by about 6.5 cm along the camera's right vector. Measured Van's upper-body focal X position at **0.328 of the 20:9 viewport** near the shot start (ideal left third: 0.333); at the final pushed-in frame X ≈ 0.307. Nevali remains clearly visible to the right and the doorway-to-play-area sightline remains open.
5. **Camera push and narration preserved.** Native Timeline camera motion still moves about **0.27 m** over 10.5 seconds, with the approved upper-left locked narration text and existing warm PBR home v03.

## Timeline tracks and technical validation

The saved v06 Timeline contains exactly **three** output tracks:

| Track | Binding | Motion |
| --- | --- | --- |
| 01 CAMERA | `P01_STATIC_HERO_CAMERA__MATCH_32MM` Animator | 0.27 m gentle push, Van framed on left third |
| 02 VAN | `YOUNG_VAN_AERT__ARMS_CROSSED_V06` Animator | Existing approximately 3-degree watching turn |
| 03 NARRATION | `A01_NARRATION__LOCKED_TEXT` GameObject | Existing activation and timed fade |

**Important recovered issue:** Unity's duplicated Timeline initially lost its PlayableDirector track bindings. We restored and saved all three v06-specific bindings; subsequent runtime testing confirmed both the camera and Van move at the expected times. No Nevali or moving-block tracks remain.

Independent live Play Mode checks:

- Timeline duration: 10.5 seconds; 3 valid bound tracks.
- Camera moved from approximately `(2.69,2.05,5.23)` to `(2.56,2.01,5.00)` while retaining A01 rotation and gentle push.
- Frozen Nevali stayed completely stationary in world space; no Animator attached.
- All 20 blocks remained at constant positions throughout the complete shot.
- Unity camera still shows Van at the left third, child on the right, round rug, blocks, flowers, bookshelf and sunlight. The central doorway beam is unchanged and is not a solid partition.
- Final targeted Unity Console check: **0 errors** at the time of this pass. No new Panel 01 scripts were introduced for runtime motion.
- The active scene was returned to Unity Edit Mode after Play Mode validation.

## Visual proof and video caveat

- `ArtSource/Cinematics/Panel01_v06/Reviews/P01_v06_StaticNevali_VanThird_GameView.png` — actual 2400×1080 Unity Play Mode Game View, including locked narration.
- `ArtSource/Cinematics/Panel01_v06/Reviews/P01_v06_Van_LeftThird_GridEvidence.png` — review-only thirds overlay; not in the game.
- `ArtSource/Cinematics/Panel01_v06/Reviews/P01_A01_v06_VideoQA_ContactSheet.png` — decoded beginning/mid/end video proof.
- `ArtSource/Cinematics/Panel01_v06/Reviews/P01_A01_v06_StaticNevali_VanThirds_10p5s.mp4` — **real Unity Play Mode preview**, 10.55 seconds, 66 frames, about 6.26 FPS, silent, 1920×1080.

Original Unity Game View runs at 2400×1080 (20:9). The MP4 **letterboxes the complete full-width 20:9 image inside 1920×1080**, rather than cutting off its right edge and inadvertently moving Van away from the thirds composition. The low video frame rate is screenshot capture overhead, not a validated in-game performance benchmark. A full-quality Recorder export has not been attempted.

## Remaining limits

- Nevali's pose is visibly seated and frozen, with hands near blocks; this is not an interactive pickup, and no hand-to-block contact was validated.
- Young Van's existing hands/crossed-arms finger placement and precise gaze could still benefit from art polish.
- Narration is readable screen text only. No VO recording, music, StoryScene transition or Android device validation has been added.
- Earlier Panel 01 scenes (v01-v05), original character FBX and Blender sources, gameplay scenes, and original camera animation remain unchanged; v06 is a new isolated cinematic branch.
