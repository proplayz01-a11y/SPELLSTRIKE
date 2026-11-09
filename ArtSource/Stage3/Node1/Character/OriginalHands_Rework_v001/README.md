# Original Sunbound Warden - faithful hand rework

2026-10-05. Current modeling direction: rework the supplied original character. The replacement gauntlet rework and whole new mesh were rejected by David; both remain preserved as separate experiments.

## Deliverables
- `SunboundWarden_OriginalHands_Reworked_v002.blend` — current native character copy, original material maps packed, original mesh topology/UV data retained, editable localized fingertip bevel and digit selection groups.
- `SunboundWarden_OriginalHands_Reworked_v002.fbx` and `.glb` — separate mesh exports. Nothing was imported into or assigned to the canonical Unity character.
- `Hand_Before_After.png` — original and refined hand rendered with identical framing, lighting and material display.
- `Final_HandTop.png`, `Final_HandPalm.png`, `Final_HandFront.png`, `Final_HandOblique.png`, `Final_Full_Front.png` — reviewed static evidence.
- `geometry_qa.json`, `geometry_qa_final.json`, `export_qa.json`, `glb_roundtrip.json` — numeric checks.

## Scope
Source: `C:/Users/David/OneDrive/Desktop/STAGE 3 CHARACTER NO. 1.blend`, SHA-256 `ad612715af88d13e1b6b560033c228b39d1d75bc866d9ca47a2bb5de5ae03c1b`.

Edited the original digit vertices, retaining its painted, worn gold/purple gauntlet style. The initial extension pass adds about 15 mm to little/index, 2 mm to ring, 1 mm to middle and up to 16 mm along the thumb's own diagonal. The last pass retracts/distally rounds the sharp apex vertices by approximately 2 mm, so final extreme lengths are smaller than those initial extension values. The little finger is slightly straighter and the thumb remains opposed, rather than parallel to the other fingers. Finger corner softening is a tunable local Bevel modifier limited by `Rework_Fingertip_Softening`.

No replacement glove, new plates, enlarged cuff, new material palette, or whole-body redesign. Both existing hands receive matched edits. Palm, wrist cuffs, body/helmet/armor silhouette, clothing, original transform and source height are retained. Native base remains 8,608 vertices and 8,622 faces; original UV coordinates are exactly unchanged. Bevel-generated UVs are interpolated for export. Source material diffuse, normal, metallic and roughness maps retain their original bytes, with output-local copies provided for portability.

## Validation
- Reviewed original/after top views and refined palm, front, oblique and full-character views.
- A second read-only visual critique found the result appropriate as an original-hand proportion refinement/static review candidate; artistic acceptance belongs to David.
- Original UV array: exact equality before/after vertex edits.
- Coordinates away from the hands: exact equality.
- Mirrored matching error: about 0.0000011 m.
- Export: 18,070 triangles; height approximately 1.693357 m, matching the original source.
- Explicit temporary triangulation was used for export to make Blender/glTF triangulation deterministic; it is not a saved destructive native mesh operation.
- GLB reimport: one mesh, 18,070 triangles, same height, original 2048px diffuse/normal and combined roughness-metallic data loaded.
- Original Desktop source hash remains unchanged. Prior experiments and canonical Unity assets were not replaced.

## Use / remaining limits
Open the v002 Blender source and inspect the original textured model. Adjust the named digit groups or localized bevel if desired. The v001 file is a retained checkpoint, not the latest result. The export files contain the refined mesh and textures, without the review camera.

The supplied original is unrigged. These are static geometry/visual checks, not proof of weights, finger bending, gripping, sword attachment, animation deformation, Unity material compatibility or PC/Android performance. Those tests are still needed before replacing the canonical character.

No gameplay scripts, Unity scenes/prefabs/Animator Controllers, Stage 1 or Stage 2 files were edited. Documentation update is limited to a changelog entry and artifact-status notes.

## Superseded after visual feedback — 2026-10-05

David judged the vertex-only original-hand refinement as visually too similar to the source. The current candidate has rebuilt finger topology and a new equal-scale comparison under `../FingerGeometry_Rework_v001/`; use `SunboundWarden_RebuiltFingers_v005.blend` there. This folder remains an earlier checkpoint, not the current accepted result.
