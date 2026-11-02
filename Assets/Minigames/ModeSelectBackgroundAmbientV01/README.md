# Minigame tent background ambience v01

Background-only polish for `Assets/Scenes/MinigameModeSelect_Integration_v01.unity`. Original approved artwork is retained, except locally repaired silhouettes beneath the newly moving native lantern/flame cutouts. No card layout, hitbox, text, navigation, hover/pressed code or SIO model assets are changed.

## Stronger ambience v02 (2026-11-02)

The original v01 was functionally correct but too hard to notice at full-screen scale.
The active integration scene now uses approximately 4.35-degree lantern sway
on asynchronous 6.3/7.15-second cycles, ~9.5% localized lantern brightness
variation, stronger lantern glass glows (rest alpha 0.24), warmer candle
glows (rest alpha 0.17), and more visible candle flame height/lean changes.
The glow sprites were widened slightly and use irregular flicker blended
with a slow, restrained pulse. The approved background plate, sprite cutouts,
chains, all finished cards and button interactions remain unchanged.
The installed scene's serialized parameters and the installer's fresh defaults
have been aligned to v02. Windows Unity Editor QA was performed; no
physical Android performance validation is implied.

## Implementation

`Runtime/ModeSelectBackgroundAmbientMotion.cs` controls 10 small image layers and 2 pinned secondary-chain images. Two lantern body/main-chain cutouts sway asynchronously with a small secondary harmonic. Perlin-based brightness variation is independent of sway. Three native candle flames pivot at their wick bases, with width/height/lean changes. Five local warm glow images provide irregular light variation. Broad baked cast light remains original; this is 2D presentation polish, not physical relighting.

Secondary chains have fixed upper pivots and follow their original roof attachment on each animated body. Their small scale/rotation adjustments keep both supports connected. Props use source RGB; alpha/hidden background reconstruction is approximate and documented in `Art/manifest.json`.

The owner is `Tent_Backdrop_Approved/ModeSelect_BackgroundAmbient`. One nested Canvas isolates its dynamic geometry from the finished UI. All 12 Images have raycasting disabled. No additional cameras, Unity Lights, particle systems, custom full-screen shader or postprocessing is created. The original background PNG is not overwritten.

## Assets and fidelity

`Art/` contains an opaque 1672 × 941 clean plate, 2 native lantern/main-support cutouts, 2 secondary-chain cutouts, 3 candle-flame cutouts, one 64 × 64 radial glow, and an alignment/preparation manifest. Only about 2% of source pixels were locally repaired. Every source pixel outside the removal/adoption masks remains unchanged. Cutout RGB is original; ring and chain holes are transparent.

The repaired hidden background uses a built-in image_gen edit plus local boundary-color harmonization. Generated pixels outside the repair masks are discarded. Original cast illumination remains baked in its old broad pools, which is appropriate for the low-amplitude movement; larger swings would expose that limitation.

Textures retain native dimensions, no mipmaps, bilinear filtering and uncompressed source fidelity. The background is capped at 2048, small sprites at 256. Estimated RGBA32 pixel storage for the nine new textures is 6,530,332 bytes (6.23 MiB) before driver overhead. The original backdrop remains referenced by an existing transparent UI Image, so both full textures may remain loaded. The small cutouts/glow account for about 0.23 MiB of the new texture total; that figure is not net additional memory. Device memory and FPS were not measured.

## Editor entry points

Visible menu: **SPELLSTRIKE > Minigames > Background Ambient**.

1. **Open Isolated Preview** imports only owned art and assembles a separately GUIDed QA copy. It refuses duplicate ambience owners.
2. **Apply And Save Background Only** applies the reviewed background to the clean active integration scene and saves only that scene. It refuses unrelated unsaved scene state.
3. **Use Actual Scene As Play Start** temporarily isolates the actual scene for Play Mode.
4. **Capture Ambient QA - Play Mode** passively samples runtime motion/interaction state and requests actual Game View PNGs over 20 seconds. It does not generate mouse events or alter motion.
5. **Restore Previous Editor Setup** restores the previous Play/active scene and closes only a clean owned preview. Other dirty scenes remain loaded and unsaved.
6. **Runtime Card Hover Smoke - Play Mode** explicitly invokes existing EventSystem enter/exit handlers to check card motion and SIO's five-slot stop gate. This is a component test, not OS pointer hit-path verification; it sends no click or navigation event.

The source integration is already assembled. Do not run broad minigame builders or add another owner. Artist controls are the `layers` and `chains` arrays on the ambient controller. Native crop/pivot coordinates and source hashes are in the art manifest.

## Validation

Native rest-composition and contrasting-background matte checks precede actual Unity preview and source-scene Play Mode review. The QA helper stays in `Editor/` and is excluded from player builds. The QA scene is not added to Build Settings.

Final screenshots, captured pose/color data, scope/hash verification and limitations are reported in the external `BackgroundAmbient_Implementation_Report.md`. Android/A05s and standalone player performance must be reported separately; Editor preview is not hardware validation. The existing Unity Remote/adb no-device issue is not repaired through global settings changes.
