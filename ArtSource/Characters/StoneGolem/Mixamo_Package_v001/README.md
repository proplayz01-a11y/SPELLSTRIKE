# Stone Golem - Mixamo upload copy

Prepared 2026-10-06 in Blender 5.0.1.

## Use
Upload **StoneGolem_Mixamo.fbx** to Mixamo. The FBX embeds the original 4096 x 4096 base-color texture. The ZIP is a delivery bundle; extract it and choose the FBX for upload.

Adobe's upload/rigging guide: https://helpx.adobe.com/creative-cloud/help/mixamo-rigging-animation.html

This is a prepared upload copy, not a claim that Mixamo auto-rigging has passed. Align the chin, wrists, elbows, knees and groin markers with the actual body joints. The source has closed stone fists, so individual finger rigging/deformation is unverified; review whichever skeleton options Mixamo presents. Bulky shoulders/armor also require a test animation after rigging.

## Exact source
`C:/Users/David/Downloads/stone golem 3d model (1).glb`

Its node `tripo_node_79325208-e312-4f26-a11d-91acbd0df204` matches the selected object ID in David's screenshot. This establishes the stone golem identity despite the screenshot's open Blender file being named DesertArcher. The source GLB and the user's unsaved Blender session were not modified.

Source SHA-256: `f8b12f7a4318c3cc4238f693dbde194749888bf4ea766280d9299a705204ea2a`.

## Preparation
- Retained source mesh shape, topology, UVs, texture detail and closed-fist T-pose.
- Rotated the copy -90 degrees about Blender Z so the front faces Blender -Y.
- Uniformly scaled by 2.432304038 to 2.00 m height; centered horizontally and placed feet at Z=0. This is an authoring scale choice.
- Applied rotation/scale into mesh coordinates, leaving clean object transforms.
- Exported one static character mesh using FBX Binary, -Z forward/Y up, without animation, armatures, cameras, lights, hidden references or other characters.

## Delivery files
- `StoneGolem_Mixamo.fbx` — upload this file.
- `StoneGolem_Mixamo_Copy.blend` — editable normalized copy with packed texture and review camera.
- `Textures/StoneGolem_BaseColor.jpg` — original image bytes for manual material use.
- `StoneGolem_Mixamo_Front.png` — reviewed front pose.
- `Export_Verification.json` — fresh reimport evidence.

## Verified
The FBX was copied to an isolated verification folder and imported with external image searching disabled. It contained one mesh, 13,109 triangles, 2.000 m height and feet at ground within floating-point tolerance. The 4K texture loaded from embedded media extracted inside that isolated folder. Native UV coordinates remain unchanged, and the source file hash still matches.

No actual Mixamo upload, auto-rig, skin-weight/animation test, Unity integration or device validation was performed. No Unity scenes, prefabs, Animator Controllers or gameplay scripts were changed. Original source and current Blender documents remain intact.
