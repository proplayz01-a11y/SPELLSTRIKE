# Sunbound Warden - new mesh v001

2026-10-05. Fresh character modeling candidate in Blender 5.0.1, following David's explicit direction to abandon the rejected hand rework and build a new character mesh. Visual reference: `C:/Users/David/OneDrive/Desktop/STAGE 3 CHARACTER NO. 1.blend` and the supplied screenshots. The new asset does not reuse source mesh vertices, faces, UVs, or images.

## Delivery
- `SunboundWarden_NewMesh_v001.blend`: native editable character, procedural PBR materials, packed texture atlases, studio camera/lights, named layers and hidden baked export copy.
- `SunboundWarden_NewMesh_v001.glb`: one consolidated character mesh, one material, embedded 2K base color, normal and roughness/metallic maps.
- `SunboundWarden_NewMesh_v001.fbx`: one consolidated mesh with embedded base color and normal textures; use the supplied roughness/metallic atlas when rebuilding the material in another application.
- `Warden_BaseColor.png`, `Warden_Normal.png`, `Warden_OcclusionRoughnessMetallic.png`: new 2048 x 2048 atlas textures. Packed-map channels are R=1 (no AO bake), G=roughness, B=metallic. Do not describe R as measured ambient occlusion.
- `NewMesh_ThreeQuarter.png`, `NewMesh_Front.png`, `NewMesh_Back.png`, `NewMesh_Hand.png`: native-model visual review renders.
- `NewMesh_Export_Verified.png`: actual reimported GLB render.
- `verification.json`, `export_roundtrip.json`, `fbx_roundtrip.json`: fresh numeric evidence.

## Design and geometry
New anatomical/garment core in a T-pose, branched five-finger glove meshes with an opposed thumb and supported finger joint rings, complete new horned helmet and mask, crescent horns, layered cuirass and abdominal plates, pauldron shells/lames, forearm and finger armor, waist belt/buckle, hip tassets, knee sun bosses, greaves and sabatons. Purple shoulder banners and front/back tabards are independent cloth meshes. New solar seals, engraving, rivets and embroidery maintain the reference's gold/purple knight theme. This is a new interpretation, not an exact surface-level replica of the source's painted sculpt detail.

Native layers:
- `LAYER_1_Anatomical_Core`
- `LAYER_2_Armor`
- `LAYER_2_Cloth`
- `LAYER_2_Armor_Detail`
- `REVIEW_Studio` (never included in export)
- `EXPORT_Baked_Character` (stored hidden; contains the single textured export mesh)

The native meshes retain independent parts and modifiers for editing. The core is made of independently closed garment/body sections, not a welded skin across all joints; final retopology/rigging decisions and deformation testing remain necessary. Armor intersections and fitted overlap are intentional in the static pose. No claim of production animation topology is made.

## Verified results
- Height including horns: **1.900 m**; feet at Z=0.
- Export: **66,496 triangles**, **34,426 mesh vertices**, one mesh/material.
- All checked core base meshes, including both branched hands, have zero non-manifold edges. This is a base-mesh check, not a whole-character Boolean union or animation test.
- New coordinates are finite. Original source file SHA-256 remains `ad612715af88d13e1b6b560033c228b39d1d75bc866d9ca47a2bb5de5ae03c1b`.
- GLB reimport: one mesh/material, 66,496 triangles, 1.900 m height; all three 2048px embedded textures loaded. Actual reimported asset rendered and reviewed.
- FBX reimport: one mesh, 66,496 triangles, 1.900 m height; embedded base color and normal pixels loaded with external image searching disabled.
- Front, back, three-quarter and hand renders inspected during modeling/refinement. Renders provide static shape/material evidence; they do not demonstrate animation deformation, gripping or mobile performance.

## Remaining acceptance
This version is an **unrigged visual/modeling candidate**. David's artistic acceptance remains pending. Rigging, weights, finger closure, sword grip, shoulder/knee deformation, Unity material/root orientation integration, LOD/triangle budget and PC/Galaxy A05s acceptance remain open. The 66,496-triangle export is not presented as an Android optimization result. Do not replace the canonical Sunbound Warden automatically with this mesh.

The original Desktop file, rejected `TargetedRework_v001` outputs, canonical rigs/animations/controllers, Unity scenes/prefabs/scripts, Stage 1 and Stage 2 were left intact. No Unity integration or gameplay edits were performed.

## Scripts
`build_new_mesh.py` constructs/audits the base; `build_armor.py`, `refine_and_render.py` and `finish_art.py` author/refine the independent armor and cloth; `bake_and_export.py` unwraps and bakes the export atlas; `verify_export.py` and `export_fbx.py` verify exported results. Intermediate Blender checkpoints are retained in this folder, but the delivery source is `SunboundWarden_NewMesh_v001.blend`.

## Status update — 2026-10-05

David rejected this whole-character new-mesh candidate and chose to return to reworking the original. Keep this folder as a separate experiment; do not integrate it into the canonical character. The current original-hand review candidate is `../OriginalHands_Rework_v001/SunboundWarden_OriginalHands_Reworked_v002.blend`, with its own README/evidence.
