# SPELLSTRIKE One Connected Library (Study + Rear Archive), V01

## Source of truth

- Assets/Story/References/Library_TopDown_Layout_REFERENCE.png
- docs/LIBRARY_ENVIRONMENT_BIBLE.md
- Approved/finalized storyboard panels 05, 16, 17, and 34

The front study and rear archive are ONE continuous building. The Gothic
Gateway opens directly into the archive, not into an invented long hallway.
One crimson runner connects the study rug, arch, and rear archive doors.

## Files created on the user's PC

- Editable Blender source: ArtSource/LibraryProps/LibraryShell/LibraryStudyArchive_OneBuilding_v01.blend
- Deterministic build script: ArtSource/LibraryProps/LibraryShell/build_library_shell_v01.py
- Independent validator: ArtSource/LibraryProps/LibraryShell/validate_library_shell_v01.py
- Proof images and QA: Library/StoryboardLibraryShell_20261022/

Unity GLB output directory:
Assets/Cinematics/Storyboard3D/Environment/LibraryShell/

- StoneWall_2m_Modular.glb
- StoneFloor_Study_2m_Modular.glb
- StoneFloor_Archive_2m_Modular.glb
- Library_StoneWallsAndFloors_v01.glb
- Library_ArchiveShelvesAndMarkers_v01.glb
- Library_StudyFurnitureGatewayAndDecor_v01.glb
- LibraryStudyArchive_OneBuilding_v01.glb

Use modular/zone files for independent renderers, material tuning and
collision. The full GLB is a complete static preview only. Do not import both
zone pieces and the full preview in the same place, as this duplicates them.

## Layout

- Draft building dimensions: 14m wide x 20.5m long x 5.5m tall.
- Study: Y -11.5 to 0; Archive: Y 0 to 9. Central arch at Y 0.
- Main table, two chairs, 8-candle chandelier, 10 study bookshelves.
- The right-side scholar corner now contains independent antique parchment
  globe and walnut side table GLBs (no longer the initial cube/sphere proxies);
  model sources and two LOD files per asset live in
  ArtSource/LibraryProps/ScholarCorner/ and
  Assets/Cinematics/Storyboard3D/Environment/Props/ScholarCorner/.
- StudyArchive placement revision: the walnut table root sits at Blender
  (5.32, -7.80, 0) and is rotated **-90 degrees about global Z**, clockwise
  when viewed from above. The globe remains its own GLB/model but its placed
  scene root is parented to the table and follows it to world
  (5.34, -8.11, 0.85). On October 23 the **entire** table parent, including
  all four material-group child meshes and the globe, was moved +0.28m in
  world X toward the east study bookshelves, reducing the visual gap from
  about 40.1cm to 12.1cm without splitting the furniture or moving its legs
  independently. Safety snapshot in Library/StoryboardLibraryShell_20261022.
- 20 archive bookshelf instances (two side wall and two interior rows).
- Southern center warm Gothic-window placeholder and lower-right entry door.
- The TWO navy/gold banners flanking that window are individually rotated
  180 degrees around local Z, preserving positions and hanging rods, so their
  gold compass-embroidered FRONT faces the study interior (+Y). The source
  banner prefab is built front-facing -Y and is intentionally unchanged.
- Rear double doors, flanking narrow windows, research tables.
- Two guardians outside the arch; two independent wall torches beside it.
- Separate target Empty for retrieving the Living Spellbook from an archive rack.
- Open ceiling/roof is intentional for cinematic blockout review.

All dimensions are engineering proposals, not exact measured picture geometry.

## QA, caveats and Unity setup

- All seven GLBs independently round-trip imported into Blender 5.0.1.
- Imported full assembled GLB: 331 mesh nodes, about 297k triangles,
  52 materials and 16 embedded image resources; test PC/Android performance.
- Initial misplaced template wall blocked the gateway. Corrected by hiding
  source-kit objects. The final study-to-archive interior proof shows an
  unblocked passage. Corrected the interrupted red runner as well.
- Source .blend and all exported GLBs exist in separate authoring folders.
- The front Gothic stained glass and wood doors remain placeholders.
- Ceiling vaults, collision proxies, hero-book animation, authored Unity
  prefabs/LODs, proper Unity URP conversion, character-scale walking checks,
  and Timeline scene are NOT done.
- No gameplay scene, StoryScene, Build Settings or saves were edited.
- No Rodin credits spent on new walls, floors or this assembly. Original Rodin
  download ZIP files remain unchanged.
- Re-running the builder deliberately overwrites only its own V01 output
  models, renders and source .blend. Make a copy before manually editing it.
