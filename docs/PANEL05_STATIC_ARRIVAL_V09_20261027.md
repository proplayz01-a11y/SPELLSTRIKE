# SPELLSTRIKE Panel 05: static arrival staging v09

**Status:** Completed Blender-only prototype, pending user visual approval.

## User-locked scope
- Panel 05 only, no Panel 06 work.
- **Static frame only**: no walking clips, no acting animation, no active deforming rig in the saved scene.
- Younger Van already inside Vander Poel's library, near the front-right doorway.
- Existing canonical staff, not a substitute.
- Camera inside the library looking toward the doorway.
- No physical library expansion; preserve approved doorway-dressing scene v03.

## Files
- Latest static Blender output: ArtSource/LibraryProps/LibraryShell/LibraryStudyArchive_P05_StaticArrival_v09.blend
- Primary review: ArtSource/Cinematics/Panel05_v03/Reviews/Blender_v09/01_Interior_MainStatic.png
- Wide alternate: ArtSource/Cinematics/Panel05_v03/Reviews/Blender_v09/02_Interior_WideStatic.png
- Read-only QA evidence: Library/Panel05Audit/P05_StaticArrival_v09_QA.json
- Earlier versions retained: doorway v03; animated blocking v04-v07; static staging v08.

## Implementation
- Young Van's textured character from ArtSource/Characters/YoungVanAert/Working/YoungVanAert_ActionForge_Textured_v04.blend.
- Evaluated still pose baked to a plain mesh, removing live armature and Mixamo animation.
- Original Assets/Character/STAFF.glb geometry and materials used; separate, static prop, positioned beside right palm. Original scale 1.9.
- Existing door, ornate shelves, lamp, small travel props, runner, warm outdoor appearance, study table, Gothic arch and original floor/walls preserved.
- Camera: P05_CAMERA_StaticArrival_MainInside_v08, still inside study toward the entrance. InteriorWide_v08 alternate available.

## Verified and limitations
- 0 armatures, 1-frame scene, 8,485 Young Van vertices and 3,623 canonical staff vertices; floor-contact proxy heights within 0.1m.
- Original 42 study-floor + 35 archive-floor modules, 47 WALL_ objects retained.
- Right-hand fingers/staff grip remain **approximate** in close-up. Do not claim final contact or pose polish.
- No Unity scene, Timeline, Android performance or final character animation validation done. This is deliberately static Blender staging.
- Approved Panels 01-04, doorway environment v03 and all source character/staff assets unchanged.
