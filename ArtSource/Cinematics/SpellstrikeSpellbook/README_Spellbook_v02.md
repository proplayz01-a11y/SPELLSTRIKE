# SPELLSTRIKE Physical Spellbook | v02
Date: 2026-10-30

## Authoritative asset
- Editable Blender: `Working/Spellstrike_PhysicalSpellbook_Model_v02.blend`
- Visual design reference: `References/Spellstrike_Fantasy_Spellbook_Asset_Board.png`
- Reference is also packed into the .blend.
- Full textured GLB: `Exports/Spellstrike_PhysicalSpellbook_Closed_Textured_v02.glb` (43,780 triangles; 291 mesh objects).
- Mobile LOD1 GLB: `Exports/Spellstrike_PhysicalSpellbook_MobileLOD1_v02.glb` (30,348 triangles; 41 mesh objects).
- Latest closed hero preview: `Working/QA/Spellstrike_PhysicalSpellbook_v02_hero_final_textured.png`
- Rig/page-turn QA render: `Working/QA/Spellstrike_PhysicalSpellbook_v02_independent_page_turn.png`

## Separate geometry and controls
- `HINGE | Front cover - rotate Y to open`: set rotation Y toward -180 degrees for opening.
- `HINGE | Back cover - rotatable around Y`: separate back cover pivot.
- `PAGE HINGE | 01 - Rotate Y` through `PAGE HINGE | 24 - Rotate Y`: independently pose/animate individual sheets.
- `PAGE SHEET | NN independent UV art`: physically separate double-sided, thickened pages, each with `Page_Art_UV` covering a full 0..1 image space; no floating image planes.
- Each page's image can later be changed separately to the approved lore illustrations, Nevali letter, etc. Keep the same page mesh and UVs.

## Materials and performance
- Four 1024px color textures are embedded in the textured GLBs: midnight leather, inset charcoal leather, brown spine leather, aged parchment.
- Blender source keeps the procedural fine leather bump; additional high-detail maps may still be needed to match the concept art exactly in Unity.
- LOD1 preserves turnable page hinges and both covers, but omits tiny gold flecks, scratch curves, and minor runes to reduce draw calls.
- LOD1 is **not yet device-tested** on the Galaxy A05s. Triangle counts and draw calls must be validated in Unity and on Android before production integration.

## Scope and protection
- This is a versioned **new** spellbook model. Source v01 reference workbench is preserved.
- **Not yet integrated** into `Prologue_Master_Sequence_P01_42_v07_SpellbookWalkPolish.unity`.
- Old P20/P21/P25/P26/P29/P30 page artwork remains in the Unity cinematic; it was not overwritten.
- Protected v06 and TutorialScene untouched.
- The open-page test was temporary and reverted. The saved Blender model is in the **closed** default pose.
- Remaining work: animation timing, UV assignment of approved illustrated pages, hero close-up texture fidelity, correct scene/world scale, and runtime/device validation.
