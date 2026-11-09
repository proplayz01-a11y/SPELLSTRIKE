# Sunbound Warden - body reflections matched to hand response

Current source: `SunboundWarden_ReflectiveBody_MaterialOnly_v002.blend`.

David explicitly prefers the clean, uniform, light-responsive finish of the hands in `TargetedRework_v001/SunboundWarden_TargetedRework_FINAL.blend` and requested the same light-dependent response on the body while keeping the character unchanged. This artifact is a material-only copy of that exact file, not a different hand/mesh candidate.

## Exact preserved data
- Character mesh/curve topology, vertex coordinates, transforms and UV signatures: identical to input, rechecked on the saved v002 file.
- Packed source texture bytes: identical. Body Base Color link and source normal image are retained.
- Original hands, their materials and their geometry are unchanged.
- Body armor/clothing shapes, color atlas, painted cracks/shading, purple fabric, and sun motifs remain intact.
- Input file SHA-256 remains `cb2eb9b631be0ef4ab410f522cedc86c4f819f011c6e894280077a7cce7720e9`.

## What changed
Only the active body material was copied and changed to `Warden_Body_HandMatched_Reflectivity`. The original `model` material is retained for comparison. Original metallic texture produces a smooth region mask (0.30-0.75 remap): metal areas receive the polished response, cloth stays dielectric with soft sheen.

- Armor metallic target: **0.80**.
- Armor roughness: **0.28-0.40**, retaining modest variation from original roughness texture.
- Cloth roughness: **0.55-0.68**; original cloth normal-map strength retained.
- Armor normal-map strength: **0.35**. This softens shader relief/highlight breakup without modifying the normal image or geometry. Do not claim unchanged shader relief.
- Base color, UVs and actual mesh normals remain intact. Painted cracks/shadows baked into the base-color texture are therefore still visible. This does not claim to remove painted wear or turn the entire body into an untextured uniform surface.

The only additional scene objects/state are the isolated reflection-review lights/camera settings. Frames **1, 31 and 61** move the key light left/front/right; the character does not animate.

## Use
Open the v002 Blender file. Material Preview has scene lights/world enabled. Scrub frames 1, 31 and 61 to inspect the moving highlights, or orbit in Material Preview. Shader controls are in the named `Hand matched light response` node frame. Reassign the retained `model` material to the body if comparing the original shader.

`ReflectiveBody_LightAngles.gif` is a three-angle preview, not a rendered gameplay animation. `Material_Before_After.png` compares the old and revised body materials under identical camera, light, exposure and world settings. The Before/After_v002 PNGs contain full-resolution captures. `Metal_Region_Mask_Check.png` verifies material regions: orange metal, blue cloth. `verification_v002.json` records source and saved-file checks.

## Validation / limits
Fixed-camera, fixed-exposure and fixed-fill renders were inspected with the rectangular key at left/front/right. Highlights shift across helmet/shoulders, chest, bracers, belt, knees, shin ridges and boot toes. Purple cloth remains fabric-like; emblems, painted details and silhouette remain visible. A separate read-only visual review reached the same Blender-specific conclusion. No obvious blanket metallic cloth or featureless clipped regions were observed at the delivered render size.

Geometry/transform/UV and texture-byte equality were asserted after creation and again after loading the saved v002 file. The input artifact remains unchanged. `v001` is an earlier, subtler shader checkpoint; use v002 for the current result.

This validation is **Blender only**. No Unity material, scene, prefab, Animator Controller, character rig, gameplay script, Stage 1 or Stage 2 asset was edited. Unity's target shader/material texture packing and visual/device validation remain separate work. Export baking is not performed in this pass, since the deliverable is the editable Blender material and its controlled preview.
