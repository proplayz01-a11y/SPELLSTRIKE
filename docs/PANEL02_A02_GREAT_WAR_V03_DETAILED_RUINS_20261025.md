# SPELLSTRIKE Panel 02, A02: Great War Detailed-Ruins Rough Cut

Date: October 25, 2026 | Unity 6.3 LTS / Blender 5.0.1

**Status:** Playable, editable **3D cinematic rough cut**, not yet approved or finished production art.

## Start here

- **Unity scene:** `Assets/Scenes/Cinematics/Prologue_P02_A02_GreatWar_v03_DetailedRuins.unity`
- **Unity Timeline:** `Assets/Cinematics/Storyboard3D/Timelines/Panel02_A02_v02/A02_BrokenLanguageWar_v02.playable`
- **Blender source:** `ArtSource/LibraryProps/DefenseSets/Prologue_GreatWar_02_03_v02_DetailedRuins.blend`
- **Blender-to-Unity GLB:** `Assets/Cinematics/Storyboard3D/Environment/DefenseSets/Prologue_GreatWar_02_03_v02_DetailedRuins.glb`
- **Live Unity video:** `ArtSource/Cinematics/Panel02_v03/Reviews/P02_A02_DetailedRuins_8p5s_UnityPreview.mp4`
- **Live Game View still:** `ArtSource/Cinematics/Panel02_v03/Reviews/P02_A02_DetailedRuins_GameView_4p2s.png`
- **Story comparison:** `ArtSource/Cinematics/Panel02_v03/Reviews/P02_ApprovedStoryboard_vs_DetailedRuins_v03.png`

The approved reference remains `Assets/Story/Approved/Prologue_Panel_02_V2_SOURCE_APPROVED.png` and the originally finalized board is at `C:\Users\David\OneDrive\Desktop\FINALIZED STORYBOARD\Prologue_Panel_02.png`.

## Story and motion, verified

Panel 02 establishes the Great War and Broken Language, with Young Van at the **left foreground** holding one staff and devastated fantasy city behind him. The abstract purple corruption must not become legible words or a defined monster. **Panel 03's `WORD_MAGIC_*` tiles and `ENTITY_*` proxy have deliberately not been included** in the Panel 02 scene.

Locked narration:

> ...until Broken Language spread across the kingdoms, twisting Words into weapons of ruin.

The existing 2-track native Unity Timeline remains bound:

1. High-wide establishing camera, eased **0.50 m lateral reveal** over **8.5 seconds**.
2. Activated narration caption with timed fades.

Actual Unity Play Mode recheck for the v03 scene: **715 environment renderers**, **0 Panel-03-only proxies**, **2 correctly bound Timeline tracks**, **0.500 m camera translation**, stationary Young Van, and preserved on-screen narration. Runtime screenshot and video were captured from the real Unity Game View, not a simulated mockup. The captured 64-frame, 8.6-second silent MP4 uses a 1920x1080 letterbox preserving the complete 20:9 camera rather than cropping it. The ~7.4 FPS preview is screenshot-capture throughput, **not** a device FPS measurement. Targeted Console query found a Unity Remote requirements failure unrelated to this shot; the whole project console was **not** clean.

## Blender v02 art enhancement

Starting from existing `Prologue_GreatWar_02_03_v01.blend`, created a fully versioned **new** `.blend` and `.glb`, preserving v01 byte-for-byte (source SHA256 `d2c878d3784a78cdc32064f306f0a72cce4e3bf1349bc201cafc2b1e35d864d8`).

- **18 old ruined building shells** preserved for blockout shape, now decorated with Gothic pointed-arch window recesses, fractured pilasters, ruined upper battlements, slumped roof fragments and charred rafters.
- **Three distant Gothic towers** added to the skyline, with crenellations, window voids and snapped timber spires.
- **160 individual scattered street rubble shards** placed mostly at the road edges, leaving the central shot line and actor stage open.
- Charred street beams, static orange fire fragments and tattered burgundy banners add devastation cues. **These are static set-dressing geometry, not a new VFX Graph simulation or animated flames.**
- Source collection `P02_v02__RUINED_CITY_DETAIL_EDITABLE` contains **587 independently editable detail objects**. New P02-only exported GLB contains **128 original compatible meshes + 587 detail meshes = 715 render meshes**, with Panel 03 word tiles and entity placeholders deliberately excluded.
- Added asset-specific Blender PBR mats for soot-stained masonry, arches, window voids, roof slate, rubble, banners and emissive ember shards. All 715 meshes imported successfully into Unity using the existing glTF pipeline.

The previously prepared v02 scene also contains a **static storm-sky image matte**, night/warm fire lighting, one stylized Young Van prototype and one staff. It was copied into v03 before replacing **only** the old city environment instance. The source v01 war scene, v02 cinematic scene, and approved Panel 01 v08 scene remain preserved.

## Known fidelity gaps / next artistic pass

- The approved storyboard contains heavily destroyed medieval masonry, burning architecture, thick smoke, more detailed rubbles, and stronger chiaroscuro. Current v03 is still **visibly low-poly** with intact box-shaped wall masses beneath added facade detail.
- Distant fire is represented primarily by **static emissive sculpted mesh shards** and point-light spill. Real animated smoke/fire VFX have **not** been built or validated.
- Young Van is the available rigged stylized 3D **prototype**, significantly simpler than the approved cinematic illustration, and his face/robe/staff still need art continuity and scene-scale polish. The separate staff is approximated to the hero pose, without authored wrist/hand constraints or an animation clip.
- The abstract violet spires are straight placeholder geometric columns. The approved 2D story depicts more intricate atmospheric corruption, so the final VFX language still needs development without readable words or monsters.
- The war-sky matte is a **static illustration on a 3D quad**; it is not volumetric storm simulation.
- No 30 FPS native Unity Recorder output, recorded voiceover, mobile/Android performance verification or Panel 02-to-03 transition was tested.

## Scope and safety

This pass does **not** modify the existing approved Panel 01, original Blender v01, old original P02/P03 v01 shared scene, v02 P02 Timeline, gameplay systems, global renderer/Quality/Build settings, or unrelated user files. No VALORANT process was running when automation was performed.
