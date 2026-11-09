# Original Sunbound Warden - rebuilt finger geometry

Current candidate: `SunboundWarden_RebuiltFingers_v005.blend`, 2026-10-05, Blender 5.0.1.

David found the preceding vertex-only refinement visually indistinguishable from the original. This pass replaces the distal finger geometry with connected cross-sectional/joint loops, fuller segments and short rounded caps. It is a materially different finger silhouette, not a relabeled copy or another whole-character model.

## Deliverables
- `SunboundWarden_RebuiltFingers_v005.blend`: current editable source, original body/armor plus remodeled fingers and local web joins, packed textures.
- `SunboundWarden_RebuiltFingers_v005.fbx`, `.glb`: separate exported copies. GLB roundtrip verified.
- `Hand_Before_After.png`: source and current candidate at identical camera scale/lighting.
- `HandTop.png`, `HandPalm.png`, `HandOblique.png`, `HandFront.png`, `Rework_Full.png`: inspected current geometry/material views.
- `geometry_qa.json`, `export_qa.json`: recorded checks.
- Earlier v001-v004 Blender files are iteration checkpoints. The v005 is the current review candidate.

## Actual changes
- Removed only distal source finger sections at local hand interfaces and bridged new, shared-vertex loops into the original mesh; all ten digits have new supported longitudinal sections and closed caps.
- Four-digit length hierarchy from the cut interface: middle 99 mm, index 94 mm, ring 92 mm, little 78 mm. These are authoring dimensions measured from the cut interface, not anatomical whole-finger measurements.
- Thumb rebuilt along an opposed diagonal with 65 mm of new section, supported joint rings, a fuller distal section and a rounded cap.
- Rounded rectangular cross-sections retain a restrained flat dorsal surface and rounded sides; the tips close over a short dome instead of an isolated source apex or a flat tube opening.
- Local thumb/little-finger web and attachment contours softened. Original cuff and central palm are retained. This is not a whole-palm topology rebuild.
- Original body, helmet, clothing and armor vertices away from the hands retain the source coordinates. Original body UVs/maps remain the material authority.

## Appearance
The original worn painted gauntlet style is retained. New finger UVs use a dedicated 1024 x 512 local atlas per texture, sampled per pixel from the corresponding source surfaces so source UV-island interpolation cannot introduce unrelated atlas stripes. Source diffuse/roughness/metallic/normal data is reused for appearance; no new plain purple glove or shiny standalone gold finger blocks were added. The new normal-map strength is reduced for the transferred finger material; final tangent-space appearance should be checked in the intended renderer.

Texture transfer can smooth/stretch details on the newly shaped digits and the thumb transition remains less uniform than a fully hand-painted texture pass. The source-painted palm texture itself is unchanged. Artistic acceptance remains David's decision; the before/after comparison is the evidence for visible shape change.

## Validation and limits
- Original source SHA-256 remains `ad612715af88d13e1b6b560033c228b39d1d75bc866d9ca47a2bb5de5ae03c1b`.
- Native current mesh: 10,764 vertices and 10,680 faces. The source had 8,608 vertices and 8,622 faces; this pass genuinely adds/rebuilds local topology.
- Zero open boundary edges in the checked hand region, confirming the remodeled digit loops are bridged and closed. This does not certify every inherited source-body topology issue.
- Coordinates outside the hands matched the original source check.
- Reviewed top, palm, oblique and front closeups plus full-character view. A read-only second visual reviewer confirmed the change is now obvious at equal scale, with complete rounded fingertips; remaining angular attachment/texture/style details are review items.
- GLB reimport: one mesh, two materials, **21,494 triangles**, original **1.693357 m** height, loaded original and local transferred textures. Explicit temporary triangulation makes export geometry deterministic.
- FBX was exported with embedded image data; a fresh FBX roundtrip was not performed for this pass.

Still **unrigged**. Finger bending, gripping, sword attachment, skin weights, animation deformation, Unity materials/controllers, PC gameplay and physical Android acceptance are unverified. No canonical Unity asset was replaced or edited. Do not claim this is animation-ready solely from closed loops, a static image or export success.

Original Desktop file and all rejected experiments remain intact. `OriginalHands_Rework_v001` is superseded by this candidate after the user's "they're the same" feedback. No scene, prefab, gameplay script, Animator Controller, Stage 1 or Stage 2 changes were made.
