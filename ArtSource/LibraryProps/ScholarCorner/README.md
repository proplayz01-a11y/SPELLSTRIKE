# SPELLSTRIKE: Antiquarian Globe + Walnut Side Table v01

## Separate candlestick completed — 2026-10-23

The small brass tabletop candlestick from the reference is now a third independent, unlit prop: ArtSource/LibraryProps/ScholarCorner/ScholarCandlestick_Unlit_v01.blend and Assets/Cinematics/Storyboard3D/Environment/Props/ScholarCorner/ScholarCandlestick_Unlit_v01.glb (948 triangles). It contains a named CandleFlame_ANCHOR but no modeled fire. A visible Unity Editor menu placed it next to the existing globe on the walnut side table in the isolated StudyArchive cinematic scene. In-engine scholar-close-up camera inspection passed. No original Rodin export or gameplay scene was changed.

Reference: the user's Oct 22 crop of a miniature scholar corner, showing a
dark wooden side table with gold-mounted parchment globe on the right and a
lit candle on the left. Assets are independent, not a fused statue/scene.

## Source and output

- Editable composed Blender source: ScholarCorner_TableAndGlobe_v01.blend
- Deterministic texture source: generate_scholar_textures.py
- Rebuild scene: build_scholar_corner_v01.py
- Two color-map images: Globe_AntiqueParchment_1024x512.png and
  Walnut_AgedGrain_1024x512.png
- Output folder:
  Assets/Cinematics/Storyboard3D/Environment/Props/ScholarCorner/
  - ScholarSideTable_Walnut_v01.glb
  - ScholarSideTable_Walnut_v01_LOD1.glb
  - AntiqueCelestialGlobe_Brass_v01.glb
  - AntiqueCelestialGlobe_Brass_v01_LOD1.glb
- Renders and round-trip validation report:
  Library/StoryboardScholarCornerPreview_20261022/

## Placement

- Table: about 1.48 m wide, 0.74 m deep, 0.85 m high; origin at floor center.
- Globe: around 0.80 m tall. Its GLB origin is at the bottom of its stand,
  so place it on the table at height 0.85 m, about x +0.31 m from desk center.
- Table includes a Candlestick_LEFT_PlacementOnly Empty at local -0.48 x
  for a **separate unlit candlestick prop**. The candle in the reference was
  deliberately not fused into the two commissioned principal props; a Unity
  flame particle/light should be separate if one is later added.
- Blender +Z corresponds to Unity +Y after import. Keep the globe and table as
  two separate prefab instances in Unity so they can be moved independently.

## Quality and limits

Clean individual GLBs with shared dark walnut, antique parchment,
aged/gilded brass materials and two 1024px textures. Material-group join in
GLBs reduces each model to four mesh objects; the source .blend preserves the
editable individual rings, table boards, feet and brass decorations.

This is static environment decoration; no modeled flame, animation, collider,
Unity prefab/LODGroup or PC/physical Android test. Re-running the builder
overwrites these exact v01 files. Do not rerun after manually modifying the
.blend without backing it up.
