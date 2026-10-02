# Stage 3 Node 2 desert archer — empty hollow quiver

**2026-10-02 — separate character asset authored from the exact source shown in the screenshot.**

## Files

All outputs are under `ArtSource/Stage3/Node2/Character/`:

- `S3N2_DesertArcher_EmptyQuiver_v001.blend` — editable character with packed texture; original reference retained in a hidden collection.
- `S3N2_DesertArcher_EmptyQuiver_v001.glb` — exported active character only, with embedded texture.
- `Character_Front.png`, `Character_ThreeQuarter.png`, `Character_Back.png`.
- `Quiver_Open_Closeup.png`, `Quiver_Interior_Top.png`.
- `empty_quiver_qa.json`, `export_verification.json` — fresh mesh and export checks.

## Source and scope

The screenshot's Tripo object ID `f922fb36-c740-49f9-8224-8ac24dda4d3d` matches
`C:/Users/David/Downloads/fantasy hooded character 3d model.glb`. That source was
used to preserve the supplied character's silhouette, hood, armor, cloth, UVs,
and texture detail. This is a faithful adaptation of the existing character,
not a claim that its original sculpt or texture was newly handmade.

The original source remains unchanged, SHA-256:
`92944cbf5cf4769de144769601f54c9f74ad97346dff41b9f4ab604ebce9a685`.
The weapon v013 asset and the user's open Blender sessions were not modified.

## Changes

- Removed **seven disconnected arrow components**, totaling **353 triangles**.
- Separated the existing quiver exterior into `DesertArcher_Quiver_Hollow`, preserving its exterior detail and UV appearance.
- Removed the **36-triangle solid top surface** that had painted arrow/shaft detail on it, plus two small residual cap wedges at the opening boundary.
- Added actual inward wall thickness, an annular rim, and an interior bottom with an editable Solidify modifier. Interior and rim use restrained leather materials.
- Preserved body UV corners exactly: measured maximum UV difference **0** on retained body faces.
- Normalized the new copy to **1.90 m tall**, feet at Z=0 and front toward Blender -Y. This is an authoring scale choice, not a measured fit to an existing enemy rig.
- Preserved the source's 4096² base-color texture and packed it into the Blender file; the GLB embeds it as well.

## Verified results

| Check | Fresh result |
|---|---|
| Body triangles | 12,813 |
| Hollow quiver evaluated triangles | 596 |
| Total | **13,409 triangles** |
| Wall thickness | Approximately **6.7 mm** |
| Centerline usable cavity depth | Approximately **31.2 cm** |
| Five rays into opening | All pass through the opening and first hit the interior about 31.2 cm below it |
| Evaluated quiver boundary edges | 0 — the wall/rim/bottom form a closed shell around an open cavity |
| Evaluated quiver non-manifold edges | 0 |
| Export reimport | Exactly two active meshes; no hidden reference or review objects exported |
| Export triangles | 13,409 |
| Export cavity depth | 0.312155 m |
| Export character height | 1.900 m |
| Packed/embedded texture | Present and loaded in both Blender and reimported GLB |

Front, three-quarter, rear, opening, and top-down cavity renders were inspected.
The opening is real geometry, not a black disk covering the original cap.

## Use and remaining work

Open the `.blend` and use the `S3N2_DesertArcher` collection. The body and hollow
quiver are separate objects under `S3N2_DesertArcher_ROOT`. `Quiver_Opening` marks
the cavity position/orientation for future attachment work. The hidden
`REFERENCE_Original_Unedited` collection is for comparison only; exclude it from
future exports. The supplied GLB already excludes it.

This character is **unrigged**. No character animation, bow attachment, Unity
prefab/scene integration, Inspector assignment, gameplay script, or Android
performance validation was added. The original body/clothing topology was
retained; rigging/weights and actual deformation still need validation. The 4K
source texture also needs a platform-specific memory/compression decision before
Android production use.

When integration is separately authorized: fit/rig the character, assign the
quiver to the appropriate hip motion, verify the empty mouth through idle and
attack animations, attach the existing bow/arrow assets, and then validate PC and
Galaxy A05s behavior. None of those future checks is claimed complete here.

## Reproduction scripts

### Mixamo material/texture package — 2026-10-02

`Character/Mixamo_Package/` contains `S3N2_DesertArcher_Mixamo.fbx` with embedded
texture, `Textures/DesertArcher_BaseColor.jpg` (4096², original image bytes),
`DesertArcher_Materials.blend` (three reusable Blender materials with packed
image), `Material_Settings.json`, usage instructions, and export verification.
`Character/S3N2_DesertArcher_Mixamo_Package.zip` bundles these delivery files.
Upload the FBX itself to Mixamo after extracting the package. The library is a
Blender material library, not a Unity `.mat` asset.

The FBX was copied to a separate verification folder and imported with external
image searching disabled. Its embedded texture loaded from that isolated folder;
mesh count, 13,409 triangles, 1.90 m height, and 0.312 m quiver cavity were retained.
The material library was also independently loaded and its packed image verified.
The editable character source was unchanged. No Mixamo upload or rigging was run.
The source has one actual color atlas; interior/rim use plain material colors.
No separate normal/roughness/metallic texture maps were invented.

`prepare_mixamo_package.py` creates and verifies this package. Supported upload
formats/embedded-media guidance:
[Adobe Mixamo guide](https://helpx.adobe.com/creative-cloud/help/mixamo-rigging-animation.html).

`inspect_character.py` imports the matching source and creates the inspection
scene. `analyze_cap.py` and `analyze_components.py` locate the exact source regions.
`build_empty_quiver.py` performs the scoped edit, restores/packs the exact embedded
source image, validates the shell, and exports. `verify_export.py` reimports the
delivery GLB and checks its mesh count, image data, scale, and cavity. These scripts
require the matching source GLB at the recorded path; they do not drive or replace
the user's interactive Blender session.
