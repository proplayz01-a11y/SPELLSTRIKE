# SPELLSTRIKE Panel 01: Blender home v03 to Unity integration

Date: 2026-10-25 | Unity 6.3 LTS / Blender 5.0.1

## Outcome

Integrated the completed, roof-on `Prologue_Home_01_Peaceful_v03.blend` into a new **isolated** Unity cinematic scene, based on the working animated v03 rough cut. Both the Blender source and previous Unity cinematic scenes were preserved.

**Recommended Unity scene to open and press Play:**

`Assets/Scenes/Cinematics/Prologue_P01_FamilyHome_v04_Animated_HomeV03.unity`

New environment GLB:

`Assets/Cinematics/Storyboard3D/Environment/DefenseSets/Prologue_Home_01_Peaceful_v03_UnityPBR.glb`

## Export and assets

- Blender file: `ArtSource/LibraryProps/DefenseSets/Prologue_Home_01_Peaceful_v03.blend`. Original file hash was verified unchanged during GLB conversion: `16fb57c159adebc37aab45aca3d0720b162a4385198d9854c2bbffd55aa78deb`.
- Exported **84 visible meshes**: 74 original environment renderers, the 6 roof pieces added in v02, and the 4 pieces in the new flower/vase collection. Includes the complete roof, windows, rug and toy blocks.
- GLB size: **1,612,304 bytes**; SHA-256 `8d5a7b46f9a2f50c07c0e5e7cf3365c02448ace884e2778b0cc315e1bfd32a01`.
- Export material compatibility: unsupported procedural Noise/Normal/Color links unlinked **in-memory only** for Principled PBR base-color export; 22 links, original Blender source not resaved. Blender procedural detail is therefore simplified to fixed portable colors.
- Unity glTFast imported **84 renderers** and **46 distinct glTF PBR materials**. The `ROOF_CONTINUOUS_OPAQUE_DECK`, `ROOF_LOW_POLY_SLATE_SHINGLE_COURSES`, `V03_BLUE_CERAMIC_FLOWER_VASE`, and ivory flower petals are present in the imported prefab. Roof deck is active with valid Unity bounds.
- Unity environment instance: `P01_V03__HOUSE__BLENDER_ROOF_AND_FLOWERS` at world position `(0,0,0)`, rotation identity, and scale `(1,1,1)`.

## Safe scene swap

- Copied the clean source scene `Assets/Scenes/Cinematics/Prologue_P01_FamilyHome_v03_AnimatedRoughCut.unity` into a newly named v04 Unity scene using a versioned scene copy. **Did not modify** that source scene or its earlier static v02 counterpart.
- Replaced **only** the root old v02 Unity house prefab instance with the new v03 UnityPBR prefab instance.
- Preserved Young Van Aert's existing arms-crossed pose, animated Nevali v13, rough motion Animator/controller, the static hero camera, narration playback component, existing lighting, and four-scene-root organization.
- Did not update Build Settings, gameplay, other cinematics, Blender sources, original storyboard or source animation files.

## Unity Editor / Play Mode evidence

1. New v04 scene saved to disk, hierarchy checked, 84 environment renderers present, camera and both actors intact.
2. Camera screenshot in Edit Mode (roof present/enabled) shows colored home, upright Young Van and Nevali, toy blocks and rug. Evidence: `Library/P01UnityIntegrationQA/Panel01_v04_HomeV03_RoofOn_EditMode.png`.
3. Unity v04 Play Mode succeeded. Nevali's Animator was initialized with active clip `Scene`, and sampled right forearm rotations showed **55.89°** motion difference across clip sample times.
4. On-screen locked narration appeared in the correct translucent upper-left panel with the live actor scene. Screenshot taken with Unity `ScreenCapture` includes OnGUI: `ArtSource/Cinematics/Panel01_v04/Reviews/P01_Unity_v04_GameView_RoofAndVase.png` (2400×1080).
5. Runtime camera reached `elapsed=10.528` seconds and **0.270 m** push distance, holding its final frame as intended.
6. Console showed a Unity Remote requirements check failure and an MCP WebSocket connection failure, not a Panel 01 code exception. Do not claim 0 unrelated Unity errors.
7. Returned to Edit Mode after Play. No modification to the original v03 Unity rough cut or v02 static fallback was intended or required.

## Known limitations

- Nevali's body/arm movement is real in the v04 Unity scene; the wooden toy blocks remain static, without verified contact/placement animation.
- Young Van's folded arms are a fixed mesh pose, not currently playing the rigged FBX animation.
- No recorded narration voice clip or original 42-panel story transition has been added.
- The small v03 flower vase exists and is rendered by Unity; it can be partly hidden behind Nevali/chairs from the original A01 composition. The roof is out of view from the interior shot, as expected, but is fully represented by active roof mesh renderers.
- This is an Editor-level cinematic rough cut. No Android APK, physical-device verification, or Unity video recapture was done as part of this environment-only integration.
