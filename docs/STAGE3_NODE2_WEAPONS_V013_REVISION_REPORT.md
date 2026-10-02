# Stage 3 Node 2 weapons — v013 revision report

**2026-10-02 — Blender asset revisions implemented; Unity integration and final texturing remain deferred.**

## Deliverables

- `ArtSource/Stage3/Node2/S3N2_DesertWeapons_v013.blend`
- Reproducible, scoped revision script: `ArtSource/Stage3/Node2/Revision_v013/revise_weapons.py`
- Inspection/render script: `ArtSource/Stage3/Node2/Revision_v013/qa_render.py`
- Actual object changes/archive list: `Revision_v013/changes.json`
- Geometry, collision-sample, normals, and native-timeline results: `Revision_v013/qa.json`
- Material views, five draw poses, nock/arrow close-ups, and three release views: `Revision_v013/*.png`

The input v012 file was preserved. Its SHA-256 remains `C1A93D0922CE87684E976BC534D389F9F798677207631CF0A469AA6AFD202277`. Older/replaced component objects are retained in the hidden `ARCHIVE_v012_Replaced` collection of v013; they are outside the production source collections. Blender-generated `.blend1` files are intermediate backups, not the delivery target.

## Implemented changes

- **A — passage/nock:** upper riser now forms an offset bridge around the established shaft line. Rest and window proxy align to Z=.120. The arrow nock is a connected fork with a rounded seat at its local origin and overlap onto the shaft. Feather roll places the single cock feather upward. Shaft bands were reduced to 6.6 mm outer radius after the release check found contact with the rest.
- **B — string:** true 3 mm four-sided cord with closed ends; ring sections follow the segment directions. Brace/full-draw ring centers are unchanged. Tip saddles now sit at the actual string endpoints.
- **C — limbs/trim:** existing ten limb objects retain the general centerline with broader tapered, chamfered sections. Sixteen posts became four continuous curved border rails. No global subdivision was introduced.
- **D — guards/caps:** shoulder and guard plates sweep in the bow plane. Hooked terminal caps replace sideways block silhouettes. One-segment 0.75 mm bevels provide edge highlights on major metal plates.
- **E — grip/cloth/runes:** oval leather grip, continuous overlapping helical wrap, two folded opaque cloth strips, thin curved rune lanes on both sides, and a restrained center accent. The upper cloth was moved outward after the first draw test found head contact. The pendant silhouette was retained and its attachment brought directly beneath the existing ring.
- **F — arrow:** sharpened 110 mm-wide barbed head with real sloped ridge faces, eight seated rune diamonds, three closed feather solids, and a small leather tie. Arrow nock-to-tip length and 12 mm shaft were retained. The head's welded edge topology was checked after correcting a precision mismatch in the construction script.
- **G — review fixture:** separate linked Blender review scene, one floating-point Draw control, seated held-arrow instance, and native draw/release timeline. The source projectile remains independent in the original source scene. No runtime animation or projectile code was added.

The rune-lane mask is packed **preview artwork**, not a final shared atlas or texture bake. Fine weathering/filigree are intentionally unfinished.

## Fresh verification

| Check | Result |
|---|---|
| Bow evaluated triangles | **2,758 / 4,000**; v012 was 3,672 |
| Arrow evaluated triangles | **460 / 800**; v012 was 265 |
| Missing UV layers | **0** in active source meshes |
| Non-unit object scales | **0** in active source meshes |
| Unintended open/zero-area structural geometry | None reported; rune overlays and retained pendant detail are intentionally surface geometry |
| Outward-facing bow/arrow rune surfaces | Pass on both sides |
| Arrow axis at 0/25/50/75/100% draw | No bow hits |
| Full arrow/bow triangle-surface intersections at those poses | None detected; only the intentional string/nock pair excluded |
| Release surface-overlap sweep | **121 discrete samples**, no detected arrow/bow intersections; includes shaft/feather versus string, excluding only the intentional string/nock pair |
| Native timeline frame 1 | Draw 0; arrow nock Y=.280 |
| Native timeline frame 16 | Draw .5; arrow nock Y=.480 |
| Native timeline frame 31 | Draw 1; arrow nock Y=.680 |
| Native timeline frames 39/43/48/60 | Arrow separates and advances while string returns to brace |
| Original v012 integrity | SHA-256 unchanged |

These are offline Blender geometry/preview checks. Discrete surface-overlap sampling is not a proof of continuous collision safety, a measured universal 2 mm clearance, an actual enemy hand fit, or a Unity/Android performance test. Rendered poses are reviewed separately from numeric results.

The render pipeline exposed review-fixture issues: rendering reevaluated the active animation frame rather than retaining a manually set pose, integer-valued custom controls snapped intermediate animation values, and initial projectile travel lagged behind string return. The final file uses floating-point properties, linear keys, and explicit frame/subframe rendering. Arrow and string nock travel match during the return to brace; the arrow then continues forward. Native timeline values above were rechecked in the saved file.

## Open and inspect

1. Open **S3N2_DesertWeapons_v013.blend** and use scene **S3N2_Review_v013**.
2. Frames **1 / 16 / 31** are brace / half draw / full draw. Release/return begins after frame **38**; the arrow leaves the string at brace around frame **43** and frame **60** completes the preview flight. Play frames **1–60** to inspect the Blender-only sequence.
3. `S3N2_Bow_ROOT` has the **Draw** custom property. It is keyframed in the preview; scrub the timeline for repeatable poses. All keyed bow parts follow that one property.
4. Scene **Scene** retains the original independent arrow source collection. `PREVIEW_HeldArrow` contains linked review copies, not extra production geometry. Exclude the preview and archive collections from future export.
5. Inspect `Bow_Grip_Window.png`, `Arrow_Nock.png`, opposite-side view, the five `Bow_Draw_*.png` captures, and `Release_Frame_39/43/48.png`.

## Remaining work and limits

- Final shared UV atlas, BaseColor/Normal/Mask/Emission baking, texture weathering and final material approval.
- Enemy right-hand/socket fit, left-hand pose/IK, gameplay release event, and real projectile lifecycle/collision behavior.
- Unity import, renderer/material consolidation, LOD policy, deformation bounds, shader appearance, PC gameplay testing, and physical Galaxy A05s profiling.
- No Unity scripts, Inspector assignments, scenes, or prefabs were changed. No Unity Play Mode or device test was run.

When Unity integration is separately authorized: fit the grip to the real right hand, confirm wrapper +Z forward, check idle/aim/full draw/release, compare held and spawned arrow world poses, then test collisions and performance at the actual enemy/projectile counts on PC and A05s. That integration is outside this revision.
