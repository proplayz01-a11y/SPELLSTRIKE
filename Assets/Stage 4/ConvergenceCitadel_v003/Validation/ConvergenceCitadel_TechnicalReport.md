# Convergence Citadel v003 — technical report

2026-10-20 · revision `v003_live_mcp_monumental_scale`

**Result:** native source checks pass **14/14**, the four bridges pass **1,212 support samples with zero failures**, and final GLB inspection/fresh-reimport checks pass **33/33**. These are offline source/export results; Unity integration and gameplay acceptance remain untested.

v003 changes the physical hierarchy to the user's latest explicit choice: **Node 3 / Titan is the largest island**, while the finale becomes a **white/gold/royal-blue throne courtyard** with an actual chair, four roofed pavilions, and a six-step stair approach. This supersedes the original largest-finale instruction. Nodes 1 and 2 retain their established sizes and landmark identities. The route remains Start → Hollow Knight → Drowned Captain → Titan → Vander Poel.

The environment remains a stylized interpretation of the painted references. Cliff forms, weathering, surface detail and lighting still differ from the dense painted artwork; a pixel-level appearance match is not claimed. Stronger scale cues and authored geometry do not establish visual acceptance. v001/v002 outputs are preserved separately. No Unity project files were edited, and no Unity import, controller traversal, NavMesh, combat/IK, or physical-device test is claimed.

## Which process authored the result

- **Live authoring:** the user-visible native **Blender 5.0.1** session was controlled through **Blender MCP**. The v002 collection was appended into that session; the initial unsaved default scene was backed up as `work/v003/InitialLiveScene_Backup.blend` before replacement. Layout, scale cues, monumental details, courtyard, bridges, and source corrections were authored in the live session. The saved primary source is `ConvergenceCitadel_Final.blend`, and the native Editor remains on that v003 file.
- **Native source checks:** Blender 5.0.1 opened/inspected the saved source. `ConvergenceCitadel_SourceVerification.json` records the final **14/14 source checks**, while `ConvergenceCitadel_LiveSourceVerification.json` preserves the native-check record. `ConvergenceCitadel_LiveBridgeSupport.json` separately records the native all-bridge support pass. These are not Unity tests.
- **Derivative export/render verification:** portable **Blender 5.1.2** is used offline to create GLBs, review renders, and fresh-GLB verification artifacts from the saved source. This derivative process must **not save over the live-authored `.blend`**. Its Blender version describes the exporter/renderer, not the source-authoring session.

The workflow is therefore live MCP authoring plus an offline derivative export/review pass; neither an all-CLI nor an all-MCP description would be accurate.

## Final asset inventory

| Artifact | Status / inventory |
|---|---|
| `ConvergenceCitadel_Final.blend` | Native 5.0.1 editable source; **16,206 objects, 16,156 mesh objects, 191,746 vertices, 297,322 triangles**, including collision and preview assets. Texture images are packed and use relative `Textures/` paths. |
| `ConvergenceCitadel_Final.glb` | **281,090 triangles, 110 meshes, 159 nodes, 53 materials, two embedded images / two texture descriptors / one sampler**. Excludes collision, preview clouds, cameras and lights; includes gameplay markers. |
| `ConvergenceCitadel_LOD1.glb` | **199,754 triangles, 110 meshes, 159 nodes, 53 materials**, same two images/descriptors and one sampler. Measured triangle reduction **28.94%**; manual Unity LODGroups required. |
| `ConvergenceCitadel_Collision.glb` | **3,816 triangles, 19 meshes/nodes, zero materials/images**; batched from **250 separately editable source collision pieces**. |
| `ConvergenceCitadel_MapManifest.json` | Final layout, bridge profiles, dimensions, source/export counts, materials, **36 marker transforms** and support results. |
| `ConvergenceCitadel_SourceVerification.json`, `ConvergenceCitadel_LiveSourceVerification.json` | Native Blender **5.0.1**, **14/14 source checks**; check scope is explained below. |
| `ConvergenceCitadel_LiveBridgeSupport.json` | **1,212 native source support samples, zero failures**; maximum recorded surface error **0.000108719 m**. |
| `ConvergenceCitadel_GLBVerification.json` | Portable Blender **5.1.2**, **33/33 binary/fresh-import checks**, exact export hashes, geometry/material checks and all-marker comparisons. |
| `ConvergenceCitadel_TextureReuse.json` | Descriptor deduplication to two texture entries / one sampler in each visual export, with binary geometry buffers unchanged and no unsupported preview normal map. |
| `ConvergenceCitadel_TopDown.png`, `ConvergenceCitadel_Perspective.png`, `Island_00_Review.png` through `Island_04_Review.png` | Source review renders. Overview, Titan and throne-courtyard renders were inspected for the intended hierarchy, complete landmarks and courtyard seal. |
| `ConvergenceCitadel_ImportedGLBReview.png`, `ConvergenceCitadel_LODReview.png` | Actual exported main/LOD reimport renders for material and LOD review; distinct from source renders. |
| `Textures/`, `Source/` | Two 2048² atlas PNGs, reproducible scripts, source/scale evidence, and `Source/README.md` describing the live-authoring and derivative-export process. |

The fresh visual GLB reimport contains **612,686 vertices** because normals, face UVs and material boundaries split vertices. The source vertex count is not the runtime vertex budget. The main visual export has **81,980 more triangles than v002's 199,110**; added geometry is not a performance or visual-quality certification.

| Source collection | Mesh objects | Triangles |
|---|---:|---:|
| 00_START_ISLAND | 109 | 7,157 |
| 01_HOLLOW_KNIGHT | 3,977 | 63,079 |
| 02_DROWNED_CAPTAIN | 3,254 | 66,672 |
| 03_TITAN_COLOSSUS | 4,020 | 69,041 |
| 04_VANDER_POEL — throne courtyard | 2,971 | 55,049 |
| BRIDGES | 720 | 13,012 |
| BACKGROUND_ISLANDS | 669 | 8,518 |
| WATERFALLS | 75 | 2,166 |
| COLLISION | 250 | 3,816 |
| CLOUDS_AND_ATMOSPHERE — preview only | 111 | 8,812 |

GAMEPLAY_MARKERS contains **36 empties**, including the new `Boss_ThroneSeat`; LIGHTING_AND_PREVIEW contains nine non-mesh objects. Five island parent empties account for the remaining non-mesh source objects. Source mesh counts are not batched draw-call counts. All 36 marker positions match the final manifest within **1 mm** after fresh GLB reimport; maximum recorded error is approximately **0.00000763 m**. Markers contain no spawning, progression or encounter logic.

## Physical hierarchy and scale cues

The authored nominal island layout is:

| Island | Blender centre (X, Y, Z) | Radius | Nominal diameter |
|---|---|---:|---:|
| Start | (0, 0, 0) | 78 | 156 |
| Node 1 — Hollow Knight | (276, 132, 6) | 138 | 276 |
| Node 2 — Drowned Captain | (624, 306, 12) | 150 | 300 |
| **Node 3 — Titan** | **(1280, 620, 18)** | **420** | **840** |
| Finale — throne courtyard | (2020, 980, 30) | 240 | 480 |

Titan's nominal diameter is **3.04348× Node 1, 2.8× Node 2, and 1.75× the finale**. Corresponding nominal circular-footprint area ratios are **9.26276×, 7.84×, and 3.0625×**. These are design-radius calculations, not navigable-area measurements. Titan's radius/diameter increased **2.25806×** from v002.

The native source pass additionally checks **measured collision footprint bounds**, rather than relying only on a perspective image or the nominal radii. Its XY bounding-box area indices for Start/Node1/Node2/Node3/finale are approximately **25,844.75 / 76,929.94 / 102,802.28 / 774,343.17 / 265,363.59 square units**: Node 3 is largest. A bounding-box product is not the mesh's true area or walkable surface area. Source edits require a fresh check.

Titan retains its colossal heads, detached stone anatomy, and gateway, with **six additional giant rear-row columns**, local stairs, and **four much smaller shrine facades** providing explicit scale contrast. The complete fallen right head contains **128 mesh parts**, restoring seven face/nose pieces removed by v002's conservative approach cleanup. It was checked in the new Titan render. Do not infer metre dimensions from distant houses in painted references: perspective, camera distance, and artistic compression alter screen size.

The construction state specifies shrine clear openings **15.8508 units wide × 24.2046 units high**, with facade height **28.7028**. Against the derived saved-Stage3 capsule (**10.710001 diameter × 21.42 height**), this leaves about **5.14 units total width margin** and **2.7846 units headroom**. These are authored opening parameters, not a capsule traversal test or proof of physical shrine access. Ordinary shrine scale is intentionally separate from the island's 2.25806× enlargement and the colossal-column scale.

Seven coherent **background ruin precincts** provide distant silhouettes. Their recorded apparent scale factors are **0.30, 0.45, 0.48, 0.50, 0.40, 0.36, 0.40**; they have **zero dedicated colliders** and are not extra destinations or playable islands.

## Floors, bridges, and source corrections

Titan's source main floor diameter is **680.4 units**, with declared clear-disc diameter **588 units** (radius **294**). The final support record has a flat top-vertex elevation range of **0.0**. The throne courtyard's declared central clear radius is **158.4**. These are source/design measurements; actual Unity import and actor calibration remain required.

Bridge presentation follows the journey: **royal stone → bound timber dock bridge → sandstone with purple banners → white stone with royal-blue banners**. New island spacing required rebuilding the bridge profiles and updating placement markers, not simply stretching the old visual meshes.

A native look exposed inherited parent/group offsets that misaligned floor/bridge presentation. Those transforms were corrected, and the source check now includes an independent downward probe at **all five visual floor centres**, in addition to collision-bound checks. Recorded floor-centre hit elevations match their design elevations within approximately **0.000031 units**.

Bridge profiles account for the asymmetric receiving islands with end allowances `t0 = 0.43 × fromRadius / horizontalLength` and `t1 = 1 − 0.43 × toRadius / horizontalLength`. Final slopes are approximately **5.945°, 5.912°, 4.775°, and 10.605°**; bridge clear width remains **30.72 units** inside a **33.60-unit total width**. Native and final manifest support records pass **303 samples per bridge / 1,212 total**, with **zero failures** and maximum surface error **0.000108719 m**. This is sampled support proof, not capsule traversal.

World matrices were baked only after a Blender view-layer update, and **102 bridge ornament/support pieces** were rebuilt on the intended routes. The final fresh GLB check adds **four bridge geometry bounds tests**, all passing, to catch misplaced support/piers near the origin instead of relying on triangle totals alone.

The native **14/14** source record covers four declared clear-disc **vertex** tests, nominal and measured Node-3 dominance, five visual-floor centre probes, finite mesh/UV coordinates, and absence of remaining zero-area faces. Final GLB verification passes **33/33**, including binary/index/finite-coordinate/material checks, fresh main/collision import, expected groups/markers, measured LOD reduction, the four bridge-route bounds tests, and **five fresh visual floor probes**. The fresh Start probe uses `PlayerSpawn_Start` at the legitimate raised pedestal **(-6, -12, 1.44)**; it does not incorrectly require that raised surface to be at zero elevation. Fresh surface probes may hit intentional paving/inlays above the base floor, unlike native checks targeting the continuous floor mesh alone.

Vertex clearance is not a triangle-intersection test, capsule sweep, camera-clearance test, or proof that an animated Titan can stand and attack. Five floor probes do not validate every point of a floor. No source/export check certifies those runtime paths.

## Surfacing, effects, and export safeguards

Shared atlas surfacing is retained, with white/gold/blue materials for the rebuilt courtyard. Source inventory contains **56 materials**; the main/LOD exports use **53**. Both visual exports reference **two embedded 2048² images, two texture descriptors and one sampler**. `ConvergenceCitadel_TextureReuse.json` records descriptor consolidation with unchanged binary geometry buffers. Keep the established safeguard: **disconnect Blender preview Bump during GLB export and reject unsupported `normalTexture` metadata**. v002's actual reimport render had caught albedo being exported as a normal map; successful geometry/index checks alone did not catch that material defect. Both final v003 visual exports pass explicit checks for absence of an unbaked preview normal map; actual reimport PNGs support the separate material/visual review.

There is no baked normal map, lightmap UV1, or ambient-occlusion bake. The retained ORM red/occlusion channel is neutral **1.0**; do not describe it as baked AO. Per-face atlas UV0 uses a **3-pixel margin** and requires mip/filtering review. The two shared 2048² atlases use approximately **32 MiB if each becomes RGBA32 without mipmaps**, or **42.7 MiB with full mip chains**, assuming one copy per image. This is arithmetic, not measured device residency; platform compression/format/import duplication may change it. Preview bump and Blender lighting/atmosphere do not automatically transfer to Unity.

Water, cascades, foam/mist, portal, and glow remain geometry/material sources for a future **URP Shader Graph/VFX** pass. Animated flow/refraction/depth foam, gameplay portal behaviour, and device overdraw are unverified. Transparent water/mist layers require real camera/device profiling.

Export metre/axis convention is **Blender (X, Y, Z) → glTF (X, Z, −Y)**, with an intended one-unit-to-one-metre mapping. GLB export is Y-up, selected-object export, applied transforms/extras, with camera/light export off. The selected Unity importer must calibrate scale, handedness, facing, marker transforms, and material channels. Exact export hashes and final binary counts are in `ConvergenceCitadel_GLBVerification.json`.

## Unity acceptance still required

The collision GLB is mesh data, not automatic Unity physics. Import its **19 batched meshes** separately, disable/remove their MeshRenderers, and deliberately configure **static non-convex MeshColliders** and layers. The source retains **250 editable collision pieces** with the same **3,816 triangles**. Decorative props/background precincts are not comprehensively covered by collision or projectile cover. Collision must remain independent of visual LOD; retain only one gameplay-marker hierarchy when setting up manual LODGroups. The LOD's measured **28.94%** triangle reduction does not reduce its 53 materials or install culling. Profile independently bounded island/bridge chunks, distant precinct visibility, shadows, split vertices, atlas instances and alpha overdraw in the real combat camera.

Future visible COS/Unity staging must:

1. Measure actual imported bounds/axes and the real canonical player/Titan. The saved Stage3 player dimensions above were rechecked from serialized source; they are derived values, not live Unity measurements. Do not resize actors to conceal an environment mismatch.
2. Walk all bridges/gates and inspect landing seams, boundaries, shrine opening/headroom, stairs, camera clearance, safe entry, and death/reset behaviour. Sampled floor rays and centre probes do not replace capsule traversal.
3. Bake/test the actual NavMesh and boss stance. Preserve Titan's **94% anatomical reach cap**, **0.35 m initial/support ground-contact tolerance**, and **0.30 m preflight left/right floor-height cap**. Recheck original frame-28 support and four genuine post-rig deformed right-sole contacts over repeated complete clips, fixed committed targets, colliders/hurtboxes, and the real 98 m wave chain.
4. Wire and test the approved encounter/vocabulary/progression/reward/reset systems. Empty markers carry no logic. Review the new art with the user, then measure PC and physical Galaxy A05s frame time, memory, draw calls, shadows, alpha overdraw and sustained behaviour. No 30 FPS claim follows from offline geometry counts.

The live-authored source and verified exports are ready for isolated Unity import calibration and further visual review. This document does not certify gameplay, NavMesh, controller, IK, a painted-reference appearance match, or device performance. `Source/README.md` preserves the live-MCP/native-source and offline derivative script workflow; changing the source or exports requires fresh verification.
