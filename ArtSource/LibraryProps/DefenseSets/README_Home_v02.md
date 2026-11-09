# SPELLSTRIKE Panel 01 — Family Home v02

Saved 2026-10-25. This is an editable **static cinematic environment** pass.

Project file: `C:\Users\David\PROTOTYPE\SPELLSTRIKE\ArtSource\LibraryProps\DefenseSets\Prologue_Home_01_Peaceful_v02.blend`

An identical deliverable copy is beside this README. Source v01 is retained unchanged.

## What changed

- Warm procedural wood and lime plaster, fitted oak floorboards, timber framing, wainscot, and softened prop edges.
- Complete pitched roof with an opaque continuous deck, staggered slate courses, closed front/back gables, wooden underside, rafters, fascia, and ridge cap. This corrects the initially missing roof surface. The roof is enabled by default.
- Tighter 8.6 × 9.6 m shell with 3.6 m walls, compared with v01's 10 × 12 m / 4.2 m shell. Actor anchor coordinates and shot axis remain usable.
- An open timber entry portal, with no solid divider between Van and the play rug.
- A round madder-red rug with an ochre border, compass stitching, and individually editable painted wooden blocks. The tallest castle is offset to leave more of Nevali's hands visible.
- A real east window aperture, linen curtains, sill plants, reading bench, and a low-cost garden glimpse.
- Dressed back family shelves with varied books, labeled bottles, trailing greenery, abstract family keepsakes, and a navy/gold star pennant.
- Completed study-chair joinery, an open grimoire, tea cups, ink/quill, a static brass lantern, and a compact back-left kitchen with kettle, pantry jars, and fruit.
- Directional golden window light, controlled warm bounce, and a soft light at the child staging position.

## Panel 01 composition

The active camera is `P01_STATIC_HERO_CAMERA` at `(-2.75, -5.2, 2.05)`, 32 mm. It frames Van on the left and Nevali's play area on the right. The timber post sits between their screen positions, while their physical eye line passes through the open portal.

- `P01_VAN_DOORWAY_STATIC`: `(-1.95, -2.75, 0)` — the v03 Van position.
- `P01_NEVALI_SEATED_CLEARANCE`: `(1.28, -0.45, 0)` — the original environment's child anchor. The temporary frozen-pose proof uses +0.045 m Z for floor clearance.
- Original `PREVIEW_CAMERA` (39 mm) and `P01_CAMERA_V03_MATCH_32MM` remain available.

Actors are **not bundled in the environment file**. The occupancy render uses existing rough-cut actors only to verify placement. The child's evaluated frozen mesh originally lay near the origin; the proof explicitly stages it at the intended child marker. No actor source, rig, animation, or rough-cut file was edited.

## Evidence and checks

- `P01_Before_v01_Matched.png` and `P01_After_v02.png`: same static camera, 1280 × 720, AgX exposure −0.2; lighting and environment changes are intentional.
- `P01_ActorOccupancy_v02.png`: temporary existing actors showing the reserved composition. Final character likeness, crossed arms, gaze, expression, and hand contact are not validated by this image.
- `P01_Roof_Exterior_v02.png` and `P01_Roof_Underside_v02.png`: complete exterior coverage and interior roof boards/rafters. The P01 after/occupancy renders were refreshed with the roof enabled. All 63 sampled upward coverage rays hit the opaque roof deck, and all nine eye-line rays remain clear with the roof included.
- `P01_Layout_Sightline_v02.png`: overhead plan with staging markers and an eye-line guide. The horizontal timber above the Van marker is an **overhead lintel**, open below.
- `P01_v02_validation.json`: nine of nine structure-only rays from the Van eye mark to child body/head samples passed; no intervening wall or structural prop.
- Final visible environment including roof: **80 mesh objects, 56,377 evaluated triangles**, about 0.57 MB compressed. Small props are grouped; redundant bevel geometry and unused actor data were removed. This is an art budget, not a measured frame-rate claim.
- Source v01 SHA-256 remains `650fbdab307f5886f4cfa71b96c5bf8ea209165555f71c01b91033c29a339e5d`.

Replaced source components remain in `SOURCE_V01__REPLACED_COMPONENTS__HIDDEN`; keep that collection hidden. The actual roof is in `V02_07_REMOVABLE_COMPLETE_ROOF`, visible by default. For a deliberate cutaway, hide that entire collection and `REMOVABLE_CEILING_TIMBERS`; the overhead plan is such a cutaway. The Blender file includes an internal README text block and roof exterior/underside cameras.

## Remaining acceptance

Use the approved Young Van model in the required arms-crossed, watching pose, then verify Nevali's exact pose and hand-to-block contact through the static camera. The current Van proof is the explicitly temporary stand-in. Complex Panel 01 animation is deferred under the user's latest direction.

No Unity scene, GLB, gameplay asset, or Build Settings changed. Blender procedural materials and lighting have not been converted or validated in Unity; PC/Android runtime acceptance remains untested.

The pre-task unrelated unsaved Blender session was preserved in `work/Blender_PreHome_Recovery.blend` before opening the home source.
