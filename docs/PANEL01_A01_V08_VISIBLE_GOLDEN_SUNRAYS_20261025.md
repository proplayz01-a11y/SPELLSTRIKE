# SPELLSTRIKE Panel 01 — v08 Visible Golden Window Rays

**Date:** October 25, 2026  
**Status:** Authored, imported, visually checked in Unity camera and Play Mode, rough-cut preview exported; **PC Editor proof, not Android-device validation**.

## Open the scene

`Assets/Scenes/Cinematics/Prologue_P01_FamilyHome_v08_A01_GoldenHourRays.unity`

This is a **new isolated copy of approved v07**, which remains unchanged. The actor positions, room geometry, complete roof, v06 static Nevali pose, static toys, rule-of-thirds camera, small Van turn, 0.27 m push, and narration have not been altered. The PlayableDirector still uses the approved existing three-track Timeline.

## Contract and visual result

User request: make visible, bright, warm **sunrays coming through the right-hand window and reaching into the family home**. v07 already cast golden light and real window-frame shadows, but there was **no visible airborne shaft**. v08 adds this missing visual layer plus brighter ambient/bounced illumination.

The window is a real aperture in the imported Blender home (east wall around x=-5). No window or roof geometry was cut or hidden. Three soft, tapered, static transparent mesh cards begin at **x=-4.61**, inside the window opening, and extend into the room. Their materials are transparent/additive, softly faded at the ends and across their width, and are independently editable.

| Visual layer | Mesh/scene name | Source → target (meters) | Art intent |
| --- | --- | --- | --- |
| Primary shaft | `V08_01_MAIN_WINDOW_SUN_SHAFT` | (-4.61, 2.86, 1.12) → (-0.92, 0.54, 1.38) | Bright, directional gold across interior |
| Secondary shaft | `V08_02_SOFT_PARALLEL_WINDOW_SHAFT` | (-4.61, 2.91, -0.68) → (-1.23, 0.62, -0.52) | Wide, quieter warm beam |
| Air glow shaft | `V08_03_INTERIOR_HAZE_SHAFT` | (-4.61, 2.31, 0.37) → (-0.91, 0.35, 0.63) | Lighter complementary haze |

The shafts are static, not animated particles; there is **no VFX Graph lifetime/spawn**, because this shot calls for a fixed late-afternoon atmosphere. The light volume is an **artist-directed illusion using alpha-blended planes**, not real participating-media volumetric ray marching or physically traced atmospheric dust. Existing v07 real directional sunlight and shadows remain underneath the visual beam cards.

## Shader Graph and editable assets

**Shader Graph:** `Assets/Cinematics/Storyboard3D/Environment/Lighting/Panel01_v08/A01_GoldenRayTransparent.shadergraph`

- Versioned copy of the project's known-good URP **unlit transparent Shader Graph**, originally used for `BasicChargeStreakAppearance.shadergraph`, without modifying that original effect.
- The graph supplies UV width falloff and vertex-alpha modulation, with exposed `_StreakTint`, `_EdgeSoftness`, `_EmissionLow`, `_EmissionHigh`, and `_Charge01` material controls. The names retain their original inherited graph nomenclature; all are inspectable from the Unity Material Inspector.
- Three serialized `.mat` assets, plus three separate 35-vertex authored mesh `.asset` files, are saved in the same v08 directory. Alpha fades across five mesh-width samples and seven longitudinal samples. Each render mesh has shadows disabled and uses the graph in transparent render queue 3000.
- Mesh cards use white vertex RGB and varying vertex alpha, with gold/cream material tints. More than one ray renders without spawning particles or scripting per-frame geometry.
- All three visual layers are children of `V08_GOLDEN_HOUR_VISIBLE_WINDOW_RAYS` inside the existing Panel 01 lighting root. Each child can be disabled or its material alpha/intensity changed independently.

**v08-only brightness:** scene sky bounce directional intensity raised from .62 to .85; window bounce point intensity 1.45 to 1.75; face fill .83 to .93; scene ambient adjusted to RGB (.55,.455,.375). These values are not changes to global URP or Quality configuration.

## Verification

- The new Shader Graph imported successfully as `SPELLSTRIKE/VFX/A01_GoldenRayTransparent`; Unity reported `shader.isSupported = true`. All three authored mesh renderers bind materials using this Shader Graph.
- A real Unity camera capture of the saved Edit-mode scene showed **clearly visible warm white-gold angled sun shafts at the window, glowing into the interior and play area**, and greater room midtone readability than v07.
- A real 3.5-second Game View screenshot also includes the existing narration and same camera framing.
- Play Mode sampled 0, 3.5, 7, and 10.5 seconds: **Van yaw 333.00° → 329.92° → 329.76° → 333.00°**. Camera push remains **0.270 m**. The three original timeline tracks remained bound.
- Nevali remains the static baked v06 mesh with **0 Animator components**, with **0.00000 m movement** through samples. All **20 toy blocks** measured **0.000000 m movement**.
- The 10.7-second actual Unity Game View capture contains **77 screenshot frames** encoded to a verified silent **1920×1080** MP4 at approximately **7.2 FPS**, with full 20:9 camera preserved as letterbox (not cropped). This is capture overhead, not a 30-FPS gameplay benchmark.
- Latest targeted Unity Console error query reported **0 errors**. No device build/physical Android check or standalone PC performance benchmark was performed.

## Evidence, previews, and review

All final visuals are under `ArtSource/Cinematics/Panel01_v08/Reviews/`:

- `P01_v08_WindowRays_GoldenHour_GameView_3p5s.png` — actual 2400×1080 Unity Game View at Timeline 3.5s, with narration and rays.
- `P01_v08_VisibleGoldenWindowRays_10p5s_UnityPreview.mp4` — actual Unity 10.7s silent screenshot-based preview, 77 frames at ~7.2 FPS.
- `P01_v08_VisibleRays_VideoContactSheet.png` — decoded video beginning/middle/end.
- `P01_v07_vs_v08_WindowSunrays_BeforeAfter.png` — v07 vs v08 camera before/after, same composition.
- `Unity_Timeline_Frames/` — underlying native 2400×1080 screenshot sequence.

## Future artistic options, not included

1. Author animated low-density dust motes in a separate VFX Graph layer if particles become desired. This would require full graph lifecycle, timing and device-performance validation under `docs/VFX_PIPELINE.md`.
2. If the ray surfaces read too theatrical on the final recording display, reduce per-material tint intensity or collapse to 2 subtle shaft layers. Do not remove the actual real directional sun/shadows.
3. Record voice narration/ambience and render a native 30 FPS Recorder export later. Not part of this visual-clarity request.

**Source preservation:** no modifications to v06/v07, imported .blend, Nevali source character/animation, original v05 Timeline, player/gameplay scenes, global rendering settings or Build Settings.
