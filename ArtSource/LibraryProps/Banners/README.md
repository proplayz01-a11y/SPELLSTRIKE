# SPELLSTRIKE Library Banner: Navy/Gold v01

**Source reference:** user-designated finalized storyboard, especially the
gold-compass navy hanging banner visible behind Vander Poel in Panel 08.
Built procedurally in Blender 5.0.1, no Rodin credits.

## Source files

- LibraryBanner_NavyGold_v01.blend: fully editable geometry/materials, with a
  BANNER_NAVYGOLD_ASSET collection and separate preview-only camera/lights.
- build_library_banner_v01.py: repeatable source builder. Re-runs overwrite
  only the banner files it generates; do not re-run accidentally.
- Banner_Navy_Woven_512x1024.png: shared woven cloth diffuse texture packed
  into the .blend and embedded in GLB.

## Ready-for-Unity output

- Assets/Cinematics/Storyboard3D/Environment/Props/Banners/LibraryBanner_NavyGold_v01.glb
- Assets/Cinematics/Storyboard3D/Environment/Props/Banners/LibraryBanner_NavyGold_v01_LOD1.glb

Z-up authoring coordinates, facing Blender -Y, with an asset Empty root at
world origin. Import using the project's ordinary glTF/GLB importer. Keep
URP material conversion/verification in Unity, then create a prefab. Place
identical prefabs along the appropriate library walls and behind Vander in
the front study.

Set up a Unity LODGroup explicitly if using the LOD1 file. Naming a file
LOD1 does not create an automatic LODGroup. Confirm cutoffs with an actual
Android performance test; none was carried out during authoring.

**No dynamic cloth simulation or Timeline animation is included.** FABRIC_01
is independent in source so motion can be authored later. The ornament
currently remains static; do not enable cloth simulation without deciding how
to attach the heraldry and embroidery.

## QA previews

Library/StoryboardBannerPreview_20261022/

Contains front, three-quarter, back, LOD1 renders and metrics JSON. The
source Blender scene and GLB files were built without editing Unity scenes.
