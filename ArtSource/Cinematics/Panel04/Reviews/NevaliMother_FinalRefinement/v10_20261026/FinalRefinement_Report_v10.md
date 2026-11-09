# Nevali's Mother — Final Character Refinement v10

**Visual-ready: PASS for medium/full-body defense cinematic presentation.** Both hands are visibly improved and the GLB retains the result after reimport. Hand anatomy remains stylized at macro range; a hero hand close-up finish is not claimed.

**Rig-ready: NO.** The source remains an unweighted T-pose character with independent hand meshes. Automatic-rigging acceptance, skin weights and finger/wrist deformation have not been tested.

## Saved files

- Blender: `C:\Users\David\PROTOTYPE\SPELLSTRIKE\ArtSource\Characters\NevaliMother\Working\NevaliMother_FinalCharacterRefinement_v10.blend`
- GLB: `C:\Users\David\PROTOTYPE\SPELLSTRIKE\ArtSource\Characters\NevaliMother\Working\NevaliMother_FinalCharacterRefinement_v10.glb`
- Versioned evidence: `C:\Users\David\PROTOTYPE\SPELLSTRIKE\ArtSource\Cinematics\Panel04\Reviews\NevaliMother_FinalRefinement\v10_20261026`
- Identical deliverable copies are beside this report.

## Hands and wrist repair

- Replaced both dense v08 voxel hands with explicit connected topology: an editable palm, four shared-vertex finger sockets and an opposed thumb side socket. Each hand cage has 843 vertices / 884 faces, with one connected component and zero open, nonmanifold, degenerate or loose elements in the hand checks.
- Differentiated finger lengths and taper; added rounded tips, padded silhouettes, restrained joint fullness, gentle depth curvature and narrower natural spacing. Reduced the elevated thumb silhouette and moved its opposition toward the palmar side.
- Added modest thenar/hypothenar fullness, a central palm cup and subtle crease/nail texture detail.
- Removed 62 retained **skin-only** wrist fragment faces from the derived body, using location and original texture color to distinguish them from the original sleeve. The new wrist extends inside the original linen cuff; there are no added cuff cylinders, disks or floating hand gaps at the inspected cinematic distances.
- Preserved the original linen rim. A tiny inherited triangulated notch is visible in extreme cuff inspection, including when the new hands are hidden. It is not a new skin seam.
- Removed one unused hand-cage point per hand and five unused inherited body wire edges. Experimental hand/cuff objects from earlier versions are absent from the final character scene/export.

## Materials and identity

- Added packed 1024 x 1024 base-color and metallic/roughness atlases for the new UV-mapped hands. Color is sampled from the original wrist skin; the maps include restrained source-derived color variation, soft creases and unpolished nail regions.
- Moderate skin roughness, zero metallic response, no shader noise and no strong subsurface effect. The actual image maps round-trip through glTF; the material does not depend on an unexported procedural bump network.
- All three original 2048 x 2048 Rodin PBR images match the original GLB byte-for-byte. All 10,214 retained body position/UV entries in the exported body match original source float bytes. A separate polygon hash verifies 15,213 original polygons outside the local wrist volume retain geometry and UVs.
- Established adult facial identity, closed eyes, subdued expression, brown hair/floral braids, jewelry, blue-and-ivory gown and gold embroidery remain. Existing weathering/desaturation is retained; no destructive dust overlay was added.

## Geometry budget

| Object | Evaluated vertices | Triangles |
| --- | ---: | ---: |
| Preserved body | 10,212 | 15,359 |
| Right hand | 13,802 | 27,600 |
| Left hand | 13,802 | 27,600 |
| **Total source** | **37,816** | **70,559** |

The GLB has **42,852 position entries** including UV/normal seam splits. v08 had 139,029 triangles; v10 reduces that by **49.25%**. The editable hand cages retain level-2 subdivision for close inspection.

## Export and reimport verification

- Actual GLB import into a separate Blender scene: PASS. Full body, palm, back and wrist renders compared visually with the source.
- Exactly three character meshes, two materials and five embedded PNG PBR images. No external image/buffer URIs, studio objects, experimental geometry, cameras, lights, skin rigs or animations in the GLB.
- Buffers, accessor lengths, indices, finite positions/normals/UVs and texture bindings: PASS. Height remains 1.830143 m; Y-up glTF orientation and mirrored hand placement retained.
- Source/reimport bounds agree within 0.000036 m; no unintended scale/orientation change was observed.
- Protected v08 Blend/GLB, rejected v09 Blend and original Rodin GLB hashes remain unchanged.

## Actual visual evidence

The QA folder includes OpenGL viewport captures of full-body front/back/side, both hands palm/back/oblique/edge-on, and both cuff transitions. Neutral and cinematic light renders and separate GLB reimport renders are included.

**Matched comparison — v08 left, v10 right, identical camera and neutral lighting:**

![Hands before and after](C:/Users/David/Documents/Codex/2026-10-25/task-improve-the-spellstrike-panel-01/outputs/NevaliMother_FinalRefinement/QA/Hands_Before_v08_After_v10.png)

![Final full body](C:/Users/David/Documents/Codex/2026-10-25/task-improve-the-spellstrike-panel-01/outputs/NevaliMother_FinalRefinement/QA/Final_v10_FullBody_Front.png)

## Remaining topology and rigging limits

- The retained Rodin body still has 151 disconnected mesh islands and 4,764 boundary edges, chiefly inherited source surface structure. This task did not remesh the gown, face or hair to force a closed character volume. New hand meshes are closed/manifold.
- Hands remain separate from the body, tucked under the cuffs. They need compatible armature binding, appropriate weights, and fist/spread/wrist-bend deformation checks. No rig-ready or Mixamo acceptance claim is made.
- No Panel 04 death pose, scene integration, animation, Unity 6.3 import/URP render, PC playback or Android device/performance test was performed. The GLB is structurally valid PBR glTF 2.0; Unity presentation still requires actual importer/material validation.
- The QA studio is retained for source inspection and excluded from GLB export. No gameplay asset, approved cinematic scene, shader or project configuration changed.

The next measured gate is a separate rigging pass, followed by wrist/finger deformation checks and the non-graphic Panel 04 pose using the approved environment. Final art acceptance remains with David.
