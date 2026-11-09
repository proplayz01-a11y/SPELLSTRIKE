# SPELLSTRIKE — Panel 01 A01 Golden-Hour Lighting v07

Date: October 25, 2026 | Unity 6.3 LTS | Isolated cinematic-lighting iteration

## Outcome

**Scene:** `Assets/Scenes/Cinematics/Prologue_P01_FamilyHome_v07_A01_GoldenHour.unity`  
**Source (preserved):** `Assets/Scenes/Cinematics/Prologue_P01_FamilyHome_v06_A01_StaticNevali.unity`

This is the **golden-hour visual candidate** for the previously approved blocking. The camera, staging, imported home, hero poses, existing Timeline assets, narration and animation tracks were not changed. The cloned v07 scene only changes or adds **scene-local lighting** plus scene ambient settings.

## Why the earlier sunlight did not read

The old `WARM_WINDOW_SUN` was a 1.2-intensity directional source facing inward toward negative X (`(-0.51,-0.74,-0.43)`), opposite the real family-home window in the east wall near `x=-5`, and cast **no shadows**. The flat ambient made the shot legible but without directional sunlight through the window.

The window is an actual opening between the east-wall upper/lower and north/south segments. Imported wood mullions, floor, rug and roof all support realtime shadow casting/receiving; no geometry had to be deleted or made transparent.

## Scene-only lighting authoring

| Scene light / setting | v07 | Purpose |
| --- | --- | --- |
| `V07_GOLDEN_HOUR_WINDOW_SUN` | Direction `(0.80,-0.58,0.14)`, warm `(1,.74,.46)`, intensity `1.82`, soft shadows strength `.67` | Real shallow-angle amber sun entering through the east window and projecting mullion shadows on the rug |
| `V07_WINDOW_SUN_PATCH__GOLDEN_AMBER` | Spot from `(-4.55,2.65,.37)`, toward `(-1.64,.30,.54)`, intensity `4.2`, 60° cone | Readable warm sunlight patch along the play area |
| `V07_NEVALI_PLAYAREA_GOLDEN_EDGE` | Spot from `(-3.38,2.95,-.45)` toward Nevali's renderer center, intensity `2.8`, 68° cone | Subtle golden edge on the child/rug while preserving her frozen pose |
| `V07_SOFT_AMBER_INTERIOR_BOUNCE` | Warm point relocated to `(-3.58,2.88,.32)`, intensity `1.45`, range `9.6` | Gentle window-side indirect bounce |
| `V07_VAN_AND_NEVALI_FACE_FILL` | Warm face point, intensity `.83` | Keeps facial features readable as sun shadows deepen |
| `V07_SOFT_LATE_AFTERNOON_SKY_BOUNCE` | Soft additional directional light, intensity `.62`, no shadows | Keeps room readable without losing golden sun patches |
| Scene ambient | Flat RGB `(.48,.405,.338)` | Warm room-wide fill, without changing project-wide URP or Quality assets |

The imported **opaque roof stays enabled**. Directional soft window-frame shadows are real shadows; the localized spots and sky bounce are art-directed scene lights. There is **no volumetric beam/particle effect** yet, so airborne god rays should not be claimed. No Shader Graph/VFX Graph assets were authored for this pass.

## Approved blocking regression, verified in actual Unity Play Mode

- Exactly **three Timeline tracks** retained (camera, Van, narration), with preserved director bindings.
- Duration: **10.5 seconds**.
- Van remains on the left third. Subtle world yaw moves from about `333°` through `329.31°` and returns to `333°`.
- Camera movement: **0.270 m** total push, no reframe or cuts introduced.
- Nevali remains the exact v06 **83,350-vertex static baked seated pose**, no Animator. Measured transform displacement **0.000000 m** at 0, 2.5, 5.25, 8, 10.5 seconds.
- **All 20 blocks** measured **0.000000 m maximum displacement** at the same timestamps.
- Complete imported roof remains enabled; amber sunlight, sofa/window area, rug shadows, and approved upper-left narration are visible in the actual Game View.
- Targeted Unity Console reported `Unity Remote requirements check failed` (unrelated to Panel 01), and **no Panel 01-specific scripting error** during QA.
- Tested in Unity Editor on PC. This pass is **not an Android validation or 30 FPS performance test**.

## Visual evidence

| File | Meaning |
| --- | --- |
| `ArtSource/Cinematics/Panel01_v07/Reviews/Panel01_v07_GoldenHour_GameView_3p5s.png` | Actual Play Mode Game View at 3.5 seconds, includes narration |
| `ArtSource/Cinematics/Panel01_v07/Reviews/P01_v07_GoldenHour_10p5s_UnityPreview.mp4` | Real 66-frame Unity screen-capture preview, 10.64 seconds, ~6.2 FPS, 1920×1080 silent video |
| `ArtSource/Cinematics/Panel01_v07/Reviews/P01_v06_vs_v07_GoldenHour_BeforeAfter.png` | Source-approved v06 versus v07 lighting, same camera, for visual review |
| `ArtSource/Cinematics/Panel01_v07/Reviews/P01_v07_GoldenHour_VideoContactSheet.png` | Beginning/middle/end sampled from decoded v07 MP4 |
| `Library/P01GoldenHour_v07/P01_v07_GoldenHour_Final_EditMode.png` | Unity camera-only final light review, before playback |

The 1920×1080 video **letterboxes the complete 2400×1080 20:9 camera**, rather than cropping the right edge or distorting the rule-of-thirds layout. 6.2 FPS reflects expensive screenshot capture in the Editor, not measured in-game real-time FPS.

## Future refinements (not part of this pass)

- If the user wants visible airborne *sunbeams/dust shafts* rather than just physical sunlight/shadows, author them as a separate VFX request following `docs/VFX_PIPELINE.md` and Shader Graph/VFX Graph checks. This pass deliberately avoids inventing an unreviewed custom shader or particles.
- A 30 FPS Recorder-quality final video, recorded voiceover, ambience/music, Android device performance, and the next prologue panel remain separate tasks.

No user-approved v06 actor staging/animation, original .blend files, global URP settings, original Timeline asset, Build Settings, or production gameplay scenes were altered.
