# SPELLSTRIKE Prologue Panel 01 — Family Home v03

**Scope:** Blender-only, static cinematic environment. Completed on 2026-10-25 with Blender 5.0.1.

**Open:** `ArtSource/LibraryProps/DefenseSets/Prologue_Home_01_Peaceful_v03.blend`

## Why v03 is deliberately small

We inspected `Prologue_Home_01_Peaceful_v02.blend` and the Codex v02 README, actual validation JSON, matched before/after camera images, temporary-actor occupancy preview, overhead sightline, complete-roof exterior, complete-roof underside, and user's approved Panel 01 storyboard/floorplan. The v02 home already has its roof, warm lighting, open doorway, dressed furniture, and readable play rug. **Those features were not rebuilt or replaced.**

The approved storyboard shows a blue ceramic vase with white flowers on the rear study table. That small family detail was missing from v02, so v03 adds an editable, grouped, low-poly turquoise ceramic vase with four ivory-and-gold flowers, stems and leaves at the rear table corner. The first placement hid it behind shelving; it was moved toward the front-right of the table to make the bouquet legible through `P01_STATIC_HERO_CAMERA` without obstructing the doorway, Nevali's anchor, or the toy blocks.

**New collection:** `V03_08_STUDY_TABLE_FLOWERS__SHOT_DETAIL` (visible by default, four mesh objects: vase, merged stems/leaves, merged flower petals, merged pollen centers). The existing main set, furniture, toys, materials, roof objects, cameras, scene lights, and marker positions are untouched.

The central dark timber post was checked by actual Blender ray casting and connected-mesh-island analysis. Its physical cross-section is approximately **18 × 22 cm**; the apparent width in the camera is a perspective effect. As a structurally plausible doorway post also found in the storyboard, it was retained rather than arbitrarily moved or shrunk.

## Evidence and independent reopen checks

All files below are in `ArtSource/LibraryProps/DefenseSets/Review_v03/`.

- `P01_v02_vs_v03_BeforeAfter.png`: matched v02/v03 32 mm hero camera renders and close-up of the study-table detail.
- `P01_v02_Before_Reference.png`: preserved copy of Codex's final v02 camera render.
- `P01_v03_Hero_RoofEnabled.png`: final 1280 × 720 A01/Panel 01 render with the complete roof **on**.
- `P01_v03_Roof_Exterior.png`: finished slate shingles, eaves, ridge and closed gable proof.
- `P01_v03_Roof_Underside.png`: solid wooden roof underside/rafters proof.
- `P01_v03_IndependentReopen_Smoke.png`: secondary 960 × 540 render from a fresh headless reopen of the saved v03 file.
- `P01_v03_validation.json`: independent quantitative geometry and scene QA.

Independent Blender v03 validation passed:

| Invariant | Result |
| --- | --- |
| Roof collection `V02_07_REMOVABLE_COMPLETE_ROOF` exists and is enabled by default | PASS |
| Roof coverage rays within the actual deck footprint | **63 / 63** hit solid deck/wooden underside |
| Van eye-to-Nevali body/head sightlines | **9 / 9** clear |
| Child staging cylinder near `(1.28, -0.45)` (structural furniture only, excluding intentional toys/floor) | No intersecting structural mesh islands |
| East window sunlight openings | **2 / 3** sample paths open; the third intentionally intersects a linen curtain |
| Golden sunlight | `Afternoon_sun_through_window`, SUN light energy 8, retained |
| Active static hero camera | `P01_STATIC_HERO_CAMERA`, 32 mm |
| Temporary/final actor geometry in environment | None |
| Roof exterior/underside cameras | Both retained |
| Visible geometry budget | **84 mesh objects / 58,157 evaluated triangles** (v02 was 80 / 56,377) |
| Fresh reopen and smoke render | PASS |

**Preserved source SHA-256:**

- v01: `650fbdab307f5886f4cfa71b96c5bf8ea209165555f71c01b91033c29a339e5d`
- v02: `2a81eec6307edf589f8c615114854f87e59d55289fcc760157ac574f89b937e6`

Only the versioned v03 `.blend` was edited/saved. The three existing Blender GUI sessions, including unrelated unsaved Young Van character work, were left untouched; all inspection and rendering used separate Blender background processes. The VALORANT safety gate was checked before development activity.

## Deliberately outstanding

- **This file contains the environment only.** The rough-cut occupancy image uses temporary actors. Exact Young Van facial likeness, crossed arms and gaze, Nevali's final seated body pose and hand contact with blocks are **not validated by this environment pass**.
- The middle doorway post remains noticeable in the hero composition, but is not an opaque wall between father and daughter and did not block sampled sightlines.
- Room is a warm static set, not a mansion or interactive simulation; hand-to-block interaction, audio, animated character performance, Unity export and mobile performance are outside this Blender-only change.
- Materials retain Blender-specific procedural shading. No claim of identical glTF/URP rendering or measured device FPS is made.

**Reproducibility:** inspection and versioned build scripts for this pass are in `Library/Panel01Home_v03_Audit/` (a temporary QA folder). The evidence and final `README_Home_v03.md` are saved beside the actual Blender deliverable for long-term review.
