# SPELLSTRIKE Study Chandelier, Brass Ring v01

Created directly in Blender 5.0.1 from the circular suspended candle
chandelier above the table in the user's finalized Prologue Panel 05.
Zero Rodin credits used. No existing Unity scenes touched.

## Source and model exports

- Editable source: ArtSource/LibraryProps/Chandelier/StudyChandelier_BrassRing_v01.blend
- Rebuild source: ArtSource/LibraryProps/Chandelier/build_library_chandelier_v01.py
- Cinematic LOD0: Assets/Cinematics/Storyboard3D/Environment/Props/Chandeliers/StudyChandelier_BrassRing_v01.glb
- Reduced LOD1: Assets/Cinematics/Storyboard3D/Environment/Props/Chandeliers/StudyChandelier_BrassRing_v01_LOD1.glb
- Distant LOD2: Assets/Cinematics/Storyboard3D/Environment/Props/Chandeliers/StudyChandelier_BrassRing_v01_LOD2.glb
- QA images and reports: Library/StoryboardChandelierPreview_20261022/

**Important:** Running the builder overwrites only its previously generated
chandelier assets, renders and saved source version; do not
rerun after making manual edits in the Blender source without creating a copy.

## Model design / Unity handoff

- Mount the Empty root at ceiling level, e.g. near the study table ceiling.
- Source uses Blender Z-up and places the ceiling mount at Z=0; chandelier
  hangs toward -Z. The GLB importer converts Z-up to Unity Y-up.
- One 1.46 m iron/brass circular ring, about 1.94 m across the eight candles.
- Four chains, eight holders and eight **unlit** candle wax meshes.
- No modeled flames, glow meshes, baked illumination or Unity lights.
- Each candle has a separately named Empty node, FlameSocket_01 through 08,
  for a reusable Unity flame VFX prefab. A single restrained amber realtime
  light or bake is preferable to eight shadow-casting lights for Android.
- Existing production StoryScene, Timeline, Stage scenes and Build Settings
  remain unchanged. Explicitly wire FlameSocket targets and an actual Unity
  LODGroup after import. File naming alone does not configure LOD.
- All preview cameras/studio lights are in the source Blender file only, and
  excluded from both GLBs.

## QA status

Background Blender source render from storyboard-like lower angle, top and
three-quarter: passed. GLB mesh/material and eight anchor round-trip test via
validate_chandelier_glbs.py: see GLBRoundtrip.json.
Not yet tested in Unity URP nor on a physical Galaxy A05s; assess those before
claiming mobile performance or fully finished scene integration.
