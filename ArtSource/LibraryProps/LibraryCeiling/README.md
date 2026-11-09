# SPELLSTRIKE Gothic Vault Ceiling v01

This ceiling follows the canonical one-building library layout: a front study directly connected through the existing Gothic Gateway to a rear archive, without an invented intermediate hallway.

## Source and exports

- Editable Blender source: ArtSource/LibraryProps/LibraryCeiling/Library_GothicVaultCeiling_v01.blend
- Rebuild script: ArtSource/LibraryProps/LibraryCeiling/build_ceiling_v01.py
- Combined roof GLB: Assets/Cinematics/Storyboard3D/Environment/LibraryCeiling/Library_GothicVaultCeiling_v01.glb
- Individual alternatives in the same folder: ROOF_STUDY_BAY_01 through 04 and ROOF_ARCHIVE_BAY_01 through 03, each .glb
- Proof renders and geometry audit: Library/StoryboardLibraryCeiling_20261023/

The completed roof contains seven independently removable bays (four over the study, three over the archive), three triangular upper-wall gable infills, dark ribbed blue-slate vaulting, wood supports, subtle gold ornamentation, and a longer chandelier suspension chain. Its combined GLB contains 11,124 triangles and uses zero Rodin credits.

## Unity integration

The combined roof is already imported through glTFast into the isolated scene Assets/Scenes/Cinematics/StudyArchive_CinematicSet_v01.unity. The imported hierarchy is named REMOVABLE_GOTHIC_CEILING__HIDE_BAYS_FOR_CAMERA. The ceiling can be hidden using the visible Editor menu SPELLSTRIKE > Cinematics > Library > 2 Toggle All Roof Bays; each individual bay is also a separate transform in the scene. Do not place the seven standalone bay GLBs on top of the combined roof, or geometry will be duplicated.

Windows Unity Editor Play Mode static-scene smoke testing passed. A separate Unity structural audit records 382 renderers, 319,588 triangles including the building, seven roof bays, three gable caps, seven simple colliders, five cameras, zero missing material slots and zero missing scripts. See Library/StoryboardLibraryCeiling_20261023/UnityStudyArchive_SetAudit.json.

## Remaining work

The roof and static environment are in Unity, not a finished animated or voiced cutscene. Camera-aware roof bay visibility, character path tests, Storyboard Timeline shots, door and Living Spellbook animation, graph-authored practical flame VFX, detail polish, and physical Samsung Galaxy A05s profiling still require validation. Do not assume the current 382-renderer environment achieves 30 FPS on mobile.
