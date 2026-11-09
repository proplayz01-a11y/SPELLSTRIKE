# SPELLSTRIKE Panel 02 A02 — v05 Back-Turn and Subtle Thunder

**Date:** October 25, 2026  
**Status:** Unity Game View and Play Mode verified, preview exported. Storyboard rough cut, not final art or physical Android validation.

## User-approved scope

Keep the Great War scene, actual canonical younger Van Aert, current staff, narration, 8.5-second duration, and 0.5 m camera reveal. Turn Van **away from the camera** to face the ruined city. Add **subtle animated lightning/thunder in the violet storm sky**.

## Result

- **Isolated Unity scene:** `Assets/Scenes/Cinematics/Prologue_P02_A02_GreatWar_v05_BackTurn_AnimatedThunder.unity`
- **Independent four-track Timeline:** `Assets/Cinematics/Storyboard3D/Timelines/Panel02_A02_v05/A02_BrokenLanguageWar_BackTurnThunder_v05.playable`
- **Silent 8.6-second actual Unity preview:** `ArtSource/Cinematics/Panel02_v05/Reviews/P02_A02_v05_BackTurn_AnimatedThunder_8p5s_UnityPreview.mp4`
- **Thunder-frame comparison:** `ArtSource/Cinematics/Panel02_v05/Reviews/P02_v05_SubtleThunder_Quiet_vs_Flash.png`
- **Video contact sheet:** `ArtSource/Cinematics/Panel02_v05/Reviews/P02_A02_v05_VideoContactSheet.png`

### Back-facing Van

Rotated the canonical `P02_YOUNGER_VAN_AERT__CANONICAL_PANEL01_MODEL` through exactly **180° around world Y**, from Euler approximately `(90°, 333°, 0°)` to `(90°, 153°, 0°)`. The real Unity camera screenshot confirms the navy/gold robe's **back faces the camera** toward the ruined city. Original left-third stage at `(1,0,10)` is preserved.

The `P02_VAN_AERT_CURRENT_STAFF__CANONICAL` from `VAN AERT.prefab` retained its exact world placement `(1.55,1.05,9.82)` after the turn; cyan crystal glow and staff material are preserved. The character retains the current *static arms-crossed mesh*, as this request is a turn/atmosphere adjustment, not an animated acting/rig retargeting task.

### Atmospheric lightning animation

The original storm-matte texture already includes fixed **abstract purple lightning cracks**; v05 animates their apparent brightness and the **illumination of the actual ruins**.

Added two distinct scene-only Timeline layers:

1. **Soft cool directional lightning light**, animated via serialized native Unity AnimationClip of `Light.m_Intensity`.
2. **Synchronized storm-sky tint**, via native AnimationClip of `MeshRenderer.material._BaseColor.r/g/b` on the existing sky quad.

Exactly **three short, separated pulses** at:

| Time | Directional-light intensity at peak |
| --- | ---: |
| 2.05 s | 0.78 |
| 5.12 s | 0.62 |
| 7.20 s | 0.34 |

Each pulse begins faintly about 0.21 seconds before the peak and fades back to zero within ~0.34 seconds afterward. Light is a cold lavender-blue shade with real-time shadows disabled. This is **not a persistent strobe**, and the scene is dark again between pulses.

**Independence:** v05 has its **own copy** of the previous v02 Timeline asset, explicitly rebound to the existing camera and narration, with the additional two animation tracks. v05 has its **own copy** of `P02_StormSky_Unlit.mat`; neither the original v04 Timeline nor the v04 sky material is changed.

**Implementation boundary:** These are presentation-only Timeline animation clips on existing scene lighting and an already-authored static sky matte. No new spawning particles, impact gameplay, scripted mesh renderers, or moving lightning geometry were added, so a new VFX Graph is not needed for this limited ambient lighting cue. Existing violet crack shapes do not animate across the sky, but their *brightness pulses*. No audio thunder rumble was added.

## Unity verification

- Unity 6.3 editor, actual A02 v05 Play Mode and live camera screenshots inspected.
- Both native tracks preserved: camera 0.5 m eased translation for 8.5 seconds; exact approved war narration text and ActivationTrack retained.
- **4 native Timeline tracks** total (camera, narration, lightning light, sky tint) and all four director bindings checked.
- Scrub sampling at 0, 1.80, 1.98, 2.05, 2.16, 2.40, 5.12, 5.46, 7.20, and 8.49 seconds confirmed Directional Light intensity rises/fades as intended.
- At first 2.05 s flash, `MeshRenderer` MaterialPropertyBlock of the sky was `(1.329,1.329,1.429)`; at 1.80 s baseline it was `(.93,.93,1)`. The shared material file remains independent.
- At 2.05 s the actual camera image is visibly brighter than at 1.70 s. Sampled lower-half image mean brightness: **49.25 → 72.13**, +46.5% during the brief brightest instant. This is a *screenshot brightness comparison*, not a physically calibrated photometric test.
- Actual screenshot-based MP4 encoded with **82 captured Unity Game View frames**, **8.6 seconds**, **1920×1080**, ~**9.54 captured FPS**. Its 20:9 camera feed is letterboxed, **not** cropped. This capture-rate is not in-game FPS.
- Unity Console contained environmental `Unity Remote requirements check failed` and a temporary tool connection diagnostic; no scene-specific runtime failures observed.

## Preservation and future work

The v04 Unity scene, its existing Timeline, Panel 01 approved v08, original Blender war source and v03 detailed-ruins environment are preserved. No gameplay scripts, player models, global render settings, or Build Settings were changed.

This v05 is defense-presentable as a **rough cut**. Future separate passes could add war smoke, moving violet branching lightning VFX, environmental fire and audio thunder, plus correct character acting/hand grip if warranted, with proper graphics performance validation.
