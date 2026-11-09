# Nevali Blender Cosmetic Repair

## Recommended actual-mesh reconstruction: v11 (2026-10-23)

Use `Nevali_TrueMeshFace_Rebuild_v11.blend` (editable Blender) and
`Nevali_TrueMeshFace_Rebuild_v11.glb` (export). This is **not an image decal**.
Based on the user's approved Nevali front and side reference, v11 physically
deletes **723 original defective Rodin face polygons** and creates a new
rounded closed 3D child head, two separate 3D eyes (sclera, brown irises,
pupils, catchlights), real modeled eyelids/lashes, eyebrows, a tapered
low-poly nose and small smiling lips. The original body, outfit and long
hair remain intact; the prior v06 hair-island reposition is retained.

Front, three-quarter, side and full-body rendered proof plus build/roundtrip
reports are in `Library/NevaliModelRepair_20261023/Geometry_v11/`.
Reimport QA verified **31 physical geometry meshes, original Rodin source
byte-unchanged, no facial-image projection and 723 old polygons removed**.
The script `reconstruct_nevali_face_geometry_v11.py` rebuilds the result.

**Visual limitations:** This is a defense-prototype character, not the
approved illustration rebuilt with production sculpting fidelity. Nose,
eye contour, and jaw may need manual polish, and the old Rodin crown still
has protruding strips. The new head parts are not weighted to bones; rigging,
facial deformation, expressions, and animation testing are pending. This is
**real 3D geometry**, but NOT yet final rig-ready topology.

## Superseded: flat photo-projection v08

## Latest: v08 reference-based facial appearance, 2026-10-23

Current editable Blender file: Nevali_FaceRebuilt_Reference_v08.blend. Export: Nevali_FaceRebuilt_Reference_v08.glb. The four approved front/side/back/top reference images have been copied into ../References and packed inside the Blender source. The v08 model projects a feathered image taken from the approved front character reference onto a curved, separate mesh above the original face. This makes the brown eyes, nose and slight smile more visually faithful and masks the black oversized pupils and bad mouth texture from Rodin. It does NOT rebuild anatomical 3D eyelid, nose or mouth topology.

Original Rodin body, blue ribbon, dress and hairstyle remain. The v06 eyebrow parts remain editable but hidden for render because the reference face includes eyebrows. The v06 correction of sixteen protruding hair islands remains in place. The upper hair is still rough; this facial projection is best for frontal/medium defense-cinematic shots and is NOT rigged, not facial animation-ready and not final close-up quality. Blender round-trip GLB QA PASS, including alpha material, with the original source verified unmodified. Evidence and front/three-quarter/full-body/top renders: Library/NevaliModelRepair_20261023/After_v08/.

Reproduce with build_nevali_face_texture_v08.py and rebuild_nevali_face_v08.py. New files were created without overwriting any previous v06 work or user Downloads. Blender scene is opened in a separate window for immediate inspection.

## Earlier v06 eyebrow/hair geometry repair

**Current prototype:** `Nevali_FaceHair_Repair_v06.blend` and `Nevali_FaceHair_Repair_v06.glb`.

The user's original Rodin files remain unchanged in `../Original/`. The source archive stays in Downloads. Version v06 adds two editable dark-chestnut eyebrow mesh strips with surface-aware placement and recesses 16 separate, protruding hair islands (453 vertices) into the main hairstyle without deleting any of the original mesh's 29,056 vertices. The hairstyle, blue ribbons, dress, and original face texture are otherwise preserved.

The earlier experiments v01-v05 are not recommended for use. In particular, the attempted oversized crown cap was visually rejected. Compare the render evidence in `Library/NevaliModelRepair_20261023/Before/` and `After_v06/`.

**Remaining issues:** The Rodin model is composed of thousands of disconnected mesh islands. Hair at the top still has flattened protruding geometry; this cleanup is partial, especially in a close-up. The face texture contains dark oversized eyes and a rectangular texture projection boundary. Those defects were not fixed by the geometry repair. The eyebrows have no skin weights or bones. No character rigging, deformation test, animation, or Unity integration was completed. This is a readable medium-shot **prototype**, not a final cinematic-close-up character.

For a future close-up-ready version, prioritize rebuilding localized crown hair mesh and manually correcting the facial UV/texture before rigging. Do not spend additional Rodin credits until comparing that workload with a new generation.

Validation: `validate_nevali_repair_v06.py` reimports the GLB and checks editable brows, body, materials, and byte-for-byte preservation of the original file against the Rodin ZIP. Its report is saved as `Library/NevaliModelRepair_20261023/After_v06/ExportRoundtripQA.json`.
