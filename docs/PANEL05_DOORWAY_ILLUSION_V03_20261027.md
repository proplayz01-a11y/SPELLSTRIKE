# SPELLSTRIKE Panel 05: Existing Library Entrance Dressing

**Date:** October 27, 2026
**Status:** Blender doorway-environment prototype, ready for Young Van blocking.

## Locked direction

Panel 05 alone: Young Van Aert arrives at Vander Poel's library, still grieving after Panel 04. The camera is **inside the front study looking toward the front-right exterior doorway**. The library is **not expanded**, and Panel 06 is not part of this pass.

## Working source and preservation

- **New source:** ArtSource/LibraryProps/LibraryShell/LibraryStudyArchive_P05_DoorwayIllusion_v03.blend
- **Preserved original:** ArtSource/LibraryProps/LibraryShell/LibraryStudyArchive_OneBuilding_v01.blend
- Experimental v02 retained: LibraryStudyArchive_P05_DoorwayIllusion_v02.blend. v03 fixes its blocked entrance.
- Approved storyboard: Assets/Story/Approved/Prologue_Panel_05_SOURCE_APPROVED.png
- No Unity scenes, gameplay settings, approved Panels 01–04, or character rigs changed.

## Actual work

1. Opened the existing front-right door outward around its hinge, preserving the original leaf and trim.
2. Reused the detailed existing bookshelf mesh named STUDY_Shelf_E_01_model beside the entrance to preserve library style. The experimental plain shelf is hidden in v03.
3. Added shallow carved doorway reveals, a navy/gold runner, wall sconce, traveler chest, ledger, subtle books, and a daylight/foliage matte seen from the entry. The backdrop is not navigable and is not a new room.
4. Preserved the central Gothic gateway, study table, rug, main shelves, and original shell footprint.
5. Created staging empties P05_STAGE_YoungVan_ExteriorStart, P05_STAGE_YoungVan_DoorThreshold, and P05_STAGE_YoungVan_WalkStop.
6. Two Blender-only reference cameras: P05_CAMERA_InsideDoorway_Reference_v03 and P05_CAMERA_WideInsideStudy_Reference_v03. Both are inside.

## Visual review and audit

- ArtSource/Cinematics/Panel05_v01/Reviews/P05_Entrance_BeforeAfter_v03.png
- ArtSource/Cinematics/Panel05_v01/Reviews/P05_Entrance_DoorwayDressing_v03.png
- ArtSource/Cinematics/Panel05_v01/Reviews/P05_InsideStudyWide_v03.png
- Audit: Library/Panel05Audit/verify_p05_doorway_v03.py
- Verified 77 original floor tiles, 47 original wall objects, 68 local doorway objects, 3 staging markers, and zero blocking props intersecting the interior central walking lane (excluding deliberate thin floor runner/door trims).

## Remaining work, not claimed complete

- Locate and validate Young Van Aert's actual short walk animation; stage an emotionally tired entry.
- Parent or constrain the approved canonical staff Assets/Character/STAFF.glb to the correct hand, checking continuity with earlier panels.
- Refine the final Panel 05 camera angle once Young Van is placed. The current cameras are environmental visual references.
- Export Unity-ready models and build Panel 05 Unity Timeline only when moving to actor/cinematic production. No Unity/Android tests were performed in this doorway-only pass.
