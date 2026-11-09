# Stage 3 Node 2 weapons — v012 final art-direction/modeling review

**2026-10-02 · Review only · Verdict: revisions required before visual/functional acceptance.**

Retain this asset and its draw system. It does not require a whole-weapon rebuild. The blockers are local: the intended shaft line intersects the upper riser, the string is a flattened surface at brace with its modeled attachment fittings elsewhere, and the bow's narrow segmented limbs/repeated rectangular posts do not yet carry the broad ornate desert silhouette. Final texturing cannot repair these shape problems.

## Evidence and limits

Inspected the saved `ArtSource/Stage3/Node2/S3N2_DesertWeapons_v012.blend`, all five supplied PNGs, both original concept images, and `docs/STAGE3_NODE2_WEAPON_MODELING_HANDOFF.md`. Read the actual Blender mesh/material/shape-key data in a separate background process with auto-execution disabled. Generated five additional isolated material views for review, using temporary cameras/lights only; no asset revisions or `.blend` save operation were performed. Source SHA-256 before/after is identical: `C1A93D0922CE87684E976BC534D389F9F798677207631CF0A469AA6AFD202277`.

- Fresh counts agree: **bow 3,672 evaluated triangles; arrow 265**. Bow has **67 mesh objects, 48 with Draw**. Consolidation remains deferred.
- Fresh geometry checks sampled Draw **0, .25, .5, .75, 1**. The intended centerline `(X=0, Z=.120, shooting -Y)` hits `Bow_RiserUpper` at every sample; first hit is world **Y=.0375**.
- String ring centers match the stated Basis/Draw anchor coordinates. Tested rigid decorative parts preserve vertex-pair distances at the endpoint within approximately **0.00000001 m**. Preserve that deformation approach; this does not validate every intermediate contact after revisions.
- At brace the string anchors are **88.6 mm from the nearest surface of `Bow_Nock_U/L`**. Full-draw distances are approximately **77.5/81.0 mm**. The cord meets the limb region, but not the fittings labeled as its attachment nocks.
- All brace string vertices have **Y=.280**. It has no depth in Y and is edge-on/invisible in the exact X-axis profile view. It is not currently a volumetric 3 mm cord.
- `Bow_WindowShelf` reaches **Z=.240**, while the intended shaft center is **Z=.120**. `CHECK_ArrowWindow` is centered at **Z=.245**. The visible window proxy and the actual firing contract disagree.
- The arrow nock collar occupies Y=-.008 to +.012, shaft ends at Y=-.012: a **4 mm separation**. The modeled throat is behind the root, while the root lies inside the closed collar.
- Supplied renders use gray Workbench shading and show a stationary, unseated arrow during draw. They demonstrate bow pose changes, not nock following or shaft clearance. This is a review-fixture limitation, not a demand to implement runtime arrow logic.
- The additional views expose actual preview materials. Their broad palette is usable; no final atlas or weathering quality is being judged. Neutral lighting was added only in memory and is not a proposed asset revision.

Additional review images and machine-readable checks are in `ArtSource/Stage3/Node2/Review_v012/ArtDirectionAudit/`: `Bow_Profile_Material.png`, `Bow_Front_Material.png`, `Bow_Center_Material.png`, `Arrow_Broadside_Material.png`, `Arrow_Nock_Closeup.png`, `inspection.json`, `functional_checks.json`, and `deformation_checks.json`.

## Rules applying to all revision batches

- Work in a new version such as v013; preserve v012. Follow the project VALORANT gate before operating Blender.
- Keep roots, authoring -Y forward, nock-to-tip length, and established Draw marker coordinates. No Unity integration during these batches.
- For meshes with Basis/Draw, make topology-preserving edits in both keys. If a local component needs different topology, finish that component and recreate/transfer only its Draw key against the unchanged anchors. Do not apply topology-changing modifiers to all keyed objects or erase/recreate the whole draw setup.
- Budgets below are incremental estimates, not measured final totals. Recount evaluated triangles after each batch. **Bow <=4,000; arrow <=800**. Bow has only **328 triangles** free until Batch C recovers space. Do not spend spare arrow triangles simply to reach 800.
- The two-strip cloth choice, modular construction, 110 mm arrowhead width, and missing final texture bake are not automatic failures against an old instruction.

## Revision Batch A — P0: make one coherent arrow path and nock

**Objects:** `Bow_RiserUpper`, `Bow_WindowBack`, `Bow_WindowShelf`, `CHECK_ArrowWindow`, `Arrow_NockCollar`, `Arrow_NockTine_L/R`, and a temporary held-arrow preview instance.

**Wrong:** the actual firing axis intersects a solid riser. The shelf/proxy are approximately 120–125 mm too high for that axis. Arrow nock geometry does not seat the string at its declared root, and its collar is detached from the shaft.

**Execute:**

1. Keep the shaft center at **X=0, Z=.120**. Reshape the upper riser locally into an offset bridge: around the opening, center the side web near **X=+.045 to +.050 m**, web thickness **18–22 mm**. Connect to the existing grip below the opening and upper limb above it; do not translate the whole upper bow or its markers.
2. Move/reshape the rest so its top is approximately **Z=.113–.114** under the 12 mm shaft. Relocate the proxy to Z=.120. Preserve a **14 mm-radius shaft corridor**, with the intentional rest support below the shaft; no other solid material may cross it. Shape the actual opening around the fletching sweep as well, rather than certifying clearance from the centerline alone.
3. Extend the nock collar forward along -Y to overlap the shaft by **6–8 mm**. Keep the current approximately **4 mm slot width**; that fits the 3 mm string. Recess a rounded seat of approximately **1.6–1.8 mm radius**, centered on the arrow root, open toward +Y. Shorten the square prongs to approximately **8–10 mm behind the seat** and taper their outer ends. The string center must actually seat at the origin.
4. In the Blender review fixture only, place the arrow root at `(0, .280 + .400*d, .120)` for Draw `d`. This is temporary pose evidence; retain the separately authored projectile asset. Verify feather passage: the current downward-pointing vane may need a **180-degree roll about -Y** so the single cock feather points upward. This preserves a vertical nock slot. Keep that roll only if the actual sweep confirms clearance.

**Type / cost:** local geometry, transforms, review-fixture poses; bow approximately **0 to +100 triangles**, arrow approximately **+16 to +48**. No runtime scripts.

**Verify:** transparent/X-ray and solid orthographic views at 0/25/50/75/100%; shaft clears the riser, string seats in the nock, head is fully ahead of the riser at full draw. Slide the full arrow forward through release in Blender and inspect all three vanes. Permit only designed shaft/rest contact; require approximately **2 mm** clearance from rigid obstacles for feathers. Show a nock close-up and no shaft/collar gap.

## Revision Batch B — P0: make the cord volumetric and attach it visibly

**Objects:** `Bow_String_Brace`, `Bow_Nock_U`, `Bow_Nock_L`; preserve all existing String/HeldArrow markers.

**Wrong:** the brace string is flat, and the nock fittings are roughly 8–9 cm beyond its endpoints.

**Execute:**

1. Reorient the existing four vertices around each string center into a real **3 mm-diameter cross-section perpendicular to the local string tangent**. At brace, this lies in XY, not XZ. In Draw, orient upper/lower rings to their segment tangents and use a bisector at the central bend. Add only one short ring either side of the nock if needed to avoid a pinched join.
2. Keep all three ring-center Basis/Draw positions unchanged. Cap the ends where they would otherwise be exposed.
3. Move/refit `Bow_Nock_U/L` so the cord ends seat in visible grooves at the actual string markers: brace `(0,.280,+/-.700)`; full draw top `(0,.400,.628)`, bottom `(0,.400,-.651)`. Update fitting positions in both keys; do not move the string to the old decorative-cap positions. A narrow bronze saddle or wrapped binding is sufficient.

**Type / cost:** geometry plus Basis/Draw correction; approximately **+4 to +24 bow triangles** for the string; retain or simplify existing fittings. Keep total string/bindings under 100.

**Verify:** exact views along X and along -Y show a cord at brace; a close orbit reveals no coincident flat sheets. At all five draw samples, endpoints visibly meet their fittings with no floating gap or sliding through the limb. Treat the draw as an artistic deformation, not a constant-length physical simulation.

## Revision Batch C — P0: recover the broad curved bow silhouette

**Objects:** `Bow_Limb_U_01..05`, `Bow_Limb_L_01..05`, all sixteen `Bow_Border_*` objects.

**Wrong:** the bow's continuous sweep is obscured by narrow rectangular limb blocks and upright posts. The sixteen border objects alone cost **704 triangles** but read as spaced pegs, not edge trim. These are visible geometry problems even before texturing.

**Execute:**

1. Retain the existing limb centerline, modular sections, height, and endpoint motion. Reshape the sections' cross-sections so the broad face lies in **YZ** and thickness is along **X**. Measure width perpendicular to the local limb tangent, not by the entire rotated object's world bounding box.
2. Starting targets at |Z| approximately .28/.42/.56/.66/.75: profile widths **90/105/80/50/30 mm**; X thickness **34/30/26/22/18 mm**. Taper neighboring ends so their boundaries meet smoothly rather than forming five obvious square-ended sticks. Allow restrained laminate seams; avoid daylight cracks during draw. No global Subdivision.
3. Rework the sixteen border posts into **four continuous-looking curved edge rails**, two per limb, about **4–6 mm wide and 2–3 mm proud**, with **8–10 longitudinal sections**. Four rails together must stay at or below **320 triangles**, recovering at least **384 triangles** from the existing 704. The executor may repurpose the current parts or locally replace only the rails; no whole-bow reconstruction.
4. Keep the recurved terminal direction. Use a one-segment edge bevel around **0.8–1.5 mm**, not a broad rounded-box bevel that dominates the limb section. Let the larger wood/laminate face carry the later rune lane.

**Type / cost:** local geometry and corresponding shape-key updates. Limb refinement allowance approximately **0 to +160 triangles**; rail reduction **at least -384**. Complete this recovery before ornament additions.

**Verify:** side silhouette at 150–200 px tall reads as two broad curved limbs; no post/fence rhythm. Inspect both broad faces and a shallow three-quarter view. Draw 0/.5/1 shows smooth section transitions and seated rails. Do not optimize by hiding defects under larger armor.

## Revision Batch D — P1: sweep the guards and sharpen the terminal caps

**Objects:** `Bow_Guard_L/R`, `Bow_Shoulder_U/L_A`, `Bow_Shoulder_U/L_Wing`, `Bow_Shoulder_U/L_Wing2`, `Bow_TipCap_U/L`, `Bow_TipCap_U/L_Spike`.

**Wrong:** current guards form three broad horizontal crossbars; the middle pair spans approximately **319 mm across X**. Shoulder wings spread across X and read as mechanical brackets. Tips read as short rectangular blocks with sideways tabs. The concept's emphasis is swept blade forms following the YZ bow profile.

**Execute:**

1. Re-profile/reorient existing guard and shoulder polygons into elongated overlapping plates with their visual sweep in **YZ**. Use a principal guard approximately **150–180 mm along the limb**, **60–85 mm profile width**, and **10–14 mm thickness along X**. Add one smaller secondary point, roughly **60–70%** of the main point's length. Keep each assembly's total X span around **70–90 mm**, except the necessary offset riser bridge. Clear the grip and arrow path.
2. Taper tips down to **1–2 mm edge thickness** and bevel about **0.8 mm**, one segment. Put the strongest plates above/below the hand rather than one broad blade crossing the palm center. Retain the existing objects and bronze assignments when practical.
3. Reshape cap/spike pairs into hooks **80–100 mm long**, **35–45 mm profile width**, **18–22 mm X thickness**, ending at approximately the current +/-0.80 m height. Sweep points with the limb instead of leaving +X flags. Keep Batch B's string seating underneath the ornamental terminal.

**Type / cost:** geometry, transforms, corresponding Draw changes; **0 to +100 bow triangles** after simplifying duplicate slab faces.

**Verify:** inspect silhouette along X and -Y; the side view carries blade character and the front view no longer resembles a three-rung crossbar. Verify no plate becomes rubbery, clips its neighbor, or intrudes into the arrow corridor at intermediate draw.

## Revision Batch E — P1: make the grip, cloth, and rune hierarchy coherent

**Objects:** `Bow_Grip`, `Bow_Wrap_01..07`, `Bow_Wrap_Tail_A/B`, `Bow_Cloth_Long/Short`, `Bow_RunePlate_U/L/Center`, `Bow_Pendant_Frame/Rune`.

**Wrong:** the grip is approximately **50 x 60 mm** with front-mounted flat strips, which read as steps rather than wrapping leather. Two cloth strips are enough, but currently read as angular flat ribbons. Three glowing rectangular blocks concentrate attention at the riser/hand while the long limb rune lanes are missing. The pendant is visually acceptable.

**Execute:**

1. Keep grip length **180 mm** and its present center at approximately `(0,-.030,0)`; taper to an approximately **38–42 x 44–48 mm** oval/chamfered section. Preserve roots/markers. Final fit to the actual hand stays deferred.
2. Conform the existing seven wrap strips around the grip sides, **20–25 mm strip width**, **1–1.5 mm apparent thickness**, overlapping slightly. Their ends must tuck into another strip/collar. Reuse topology where possible; do not build individual cords or stitches.
3. Retain **two cloth strips**. Give them visible **20–30 mm ribbon width**, 2–3 shallow out-of-plane folds, a tapered end, and 2–3 large notches. Add **0.6–0.8 mm thickness** or explicit backs. One can remain longer (approximately **180–220 mm**), the other **100–140 mm**. Anchor under a wrap, not apparently floating; keep clear of the hand, shaft, and cord.
4. Reuse the rune material, but reshape/relocate the upper/lower rune plates into thin curved lane inserts on the newly broad limb faces, from approximately **|Z|=.32 to .63**, width **15–22 mm**, at most **1 mm proud**. Upper and lower lanes are the primary amber accents. Reduce the central plate to a **10–14 mm** accent or a non-emissive ornament; keep the pendant secondary. Lay out 5–7 separated glyph/mask positions per lane now; final glyph designs/emission baking belong to final texturing. Do not model each letter or leave an unbroken bright neon rail as the final treatment.

**Type / cost:** geometry, local transforms, shape-key following for lane inserts, preview material assignment. Allow **+140 to +220 bow triangles net** across wraps/cloth/lanes after repurposing the three existing plates; enforce the total cap after Batch C savings.

**Verify:** bare grip and wrapped grip close-ups; cloth front/back orbit; material view without bloom at 150–200 px bow height. Amber draws attention to the curved limbs, while the pendant remains a small accent. No thick glowing blocks or hovering inserts. Keep palette judgments provisional until the atlas exists.

## Revision Batch F — P1: sharpen the arrowhead and make feathers readable from both sides

**Objects:** `Arrow_Head`, `Arrow_Ridge_A/B`, `Arrow_Rune_A_1..4`, `Arrow_Rune_B_1..4`, `Arrow_Feather_1..3`, `Arrow_TieStub`.

**Wrong:** the head has a stepped, roughly zigzag/stacked-diamond silhouette and constant **18 mm thickness**, including the point. Its two ridge objects are flat cards, not a shaped ridge. The feathers are each a **single five-vertex face (3 evaluated triangles)** with no thickness/back. The short tied collar looks like a rectangular side peg.

**Execute:**

1. Keep head length **220 mm**, the current **110 mm maximum width at the widest barb pair**, shaft diameter/length, and all eight rune diamonds. The 110 mm deviation is accepted for readability; do not shrink it merely to satisfy the old 90 mm number.
2. Reshape the upper **110–130 mm** into a continuous taper toward the point, with blade width approximately **50–60 mm** near its base. Concentrate the widest **110 mm** at one rear-swept barb pair in the lower half; keep the second barb pair around **60–70 mm**. Sweep barb tips toward the shaft instead of making three equally prominent sideways diamond lobes.
3. Use the existing head as the base, adding real center-ridge faces. Keep ridge thickness **14–18 mm**, taper perimeter to **1–2 mm** and the apex below **1 mm**. Integrate or replace only the two superficial ridge cards. Seat rune diamonds on the sloping faces **0.5–1 mm proud**. No floating planar ridges or displaced diamonds.
4. Keep three feathers, **145 mm length**, approximately **26 mm radial height**. Add 3–5 length sections and at most three broad trailing-edge notches per vane, plus **0.6 mm thickness/explicit backs**. Preserve three-vane spacing. Use Batch A's validated roll. Small feather barbs are texture work.
5. Taper/bend `Arrow_TieStub` into a short leather knot/tail, maximum **20 mm**, and assign leather rather than wood. Keep it clear of the shaft's travel envelope.

**Type / cost:** local geometry/material assignment; approximately **+120 to +220 arrow triangles**, plus Batch A's nock. Expected total approximately **400–535**, comfortably below 800. Do not add a high-poly shaft or tiny engravings to fill the budget.

**Verify:** isolated arrow broadside, edge-on, rear, and three-quarter views. Head reads as a sharp spear with barbs rather than a thick cutout. Four amber diamonds read on each face. Feathers remain visible with backface culling enabled and clear the revised bow during a release sweep.

## Revision Batch G — P0 acceptance evidence, no additional asset scope

**Objects/area:** revised asset review setup and report, not Unity.

**Wrong:** existing gray views hide material distribution and the stationary arrow cannot establish nock/shaft clearance. A single favorable three-quarter angle hides the flat string and head thickness.

**Execute:** render the same camera/scale at Draw 0/50/100 with the preview arrow seated; add X-axis bow profile, -Y bow front, opposite three-quarter, isolated arrow broadside/edge-on, nock close-up, and grip/window close-up. Provide both neutral clay and actual preview-material views with bloom off. Capture a short Blender-only draw/release sweep or five explicit poses plus the forward feather passage. Include counts and each gate's actual result.

**Type / cost:** review setup/evidence only; **zero asset triangles**. Authoring object/material count is recorded, not consolidated in this review.

**Verify:** bow <=4,000, arrow <=800; roots/axes unchanged; no negative or non-unit scale; all changed keyed parts have matching vertex counts and valid Basis/Draw; no string vanish, attachment gaps, shaft collision, missing feather backs, or unexpected ornament stretch. Mark anything untested explicitly. Stop and return for review; do not begin Unity integration.

## P2 — optional polish only

- Slight asymmetric cloth wear, different upper/lower trim damage, 1–2 broad silhouette chips, and subtle patina variation. Prefer texture/normal work. Any silhouette chip allowance is **<=20 extra bow triangles total**, only within the cap and after all P0/P1 work.
- Do not add a third cloth strip, individual rope strands, more jewels, extra hanging props, or geometry filigree merely to increase detail. These do not address the present weaknesses.

## KEEP AS-IS

- Current approximately **1.60 m** overall bow scale and the centerline's general recurved sweep. Local profile/terminal refinements above are sufficient.
- **Basis + Draw design**, correct endpoint/nock coordinates, and the existing rigid-decoration following method. Preserve fixed grip/window behavior while correcting the window geometry.
- Separate authoring parts, names, root hierarchy, unit scale, and **-Y authoring / +Z intended Unity wrapper** convention.
- Arrow's **950 mm nock-to-tip length**, **12 mm shaft**, three-feather layout, four rune diamonds per face, and accepted **110 mm maximum barb width**.
- Two cloth strips, single pendant silhouette, and restrained material-slot palette of bronze, sand/wood, leather, cloth, and amber. Do not redo texture polish before silhouette approval.
- The intentionally deferred LOD/export work. Existing UVs remain a usable authoring baseline until topology is approved.

## DEFER UNTIL UNITY

- Actual enemy right-hand fit and `BowSocket_R` offsets; left-hand IK/pose; final weapon scale against the real enemy.
- Runtime held-arrow following, release event, projectile pooling/spawn/collision/damage, aim alignment, and gameplay timing.
- Shader Graph/URP import appearance, bloom decisions, final renderer/material consolidation, LOD policy, runtime blend-shape bounds, shadows, and physical Galaxy A05s profiling. Enemy-specific use does not establish those performance results, but none is a prerequisite to complete this Blender review.
- Scene/prefab integration and any modification to existing gameplay scenes.

## DEFER UNTIL FINAL TEXTURING

- Deliberate shared-atlas packing and final BaseColor/Normal/Mask/Emission PNGs.
- Heat cracks, fine metal filigree, engraved glyph recesses, wood grain, stitching, feather barbs, cloth weave/fraying, edge wear, sand accumulation, and oxidation.
- Exact roughness/metallic breakup and final rune intensity/color tuning. Keep the current preview palette as a starting point, not a final material approval.

## Executor order

**A — clear arrow path and seat the nock → B — correct cord/attachment geometry → C — broaden limbs and replace border posts → D — sweep guards and tip caps → E — conform wraps/cloth and establish limb rune hierarchy → F — refine arrowhead and feather backs → G — return verified review views/counts.**

P2 is optional after the above. No asset revision, export, or Unity integration was performed by this reviewer.
