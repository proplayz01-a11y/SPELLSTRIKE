# SPELLSTRIKE: Storyboard-to-3D Cinematic Production Plan

**Planning date:** 2026-10-21  
**Status:** Planned; environment assets, Timeline clips, animation, audio, full cinematic playback, and physical-device QA are **not complete**.  
**Scope:** Recreate the **existing approved storyboard's places and events** as voiced, animated 3D cinematics. No new narrative scenes or character redesigns without approval. This plan does not authorize changes to stage progression or the postponed academic-document revisions.

## 1. Source-of-truth and migration rules

1. Original 42 approved prologue source panels: `Assets/Story/Approved/Prologue_Panel_XX_SOURCE_APPROVED.png`, panel continuity in `docs/STORY_PANEL_ASSET_MANIFEST.md`.
2. Current import baseline: **28 production composite images** `Assets/Story/Pages/Prologue_Page_01.png` through `28.png`; `Assets/Data/Story/Prologue_Sequence.asset` serializes 28 pages. `docs/STORY_UNITY_PAGE_LAYOUT_PLAN.md` is an **older 21-page plan**, not the current runtime page count.
3. Library geometry must obey `docs/LIBRARY_ENVIRONMENT_BIBLE.md`, especially `Assets/Story/References/Library_TopDown_Layout_REFERENCE.png`; keep the front study, lower-right exterior entry, flanking shelves, central arch/red runner, and rear archive physically connected. Do **not** treat the three workbench staging roots as three separate library buildings.
4. Keep the existing comic `StoryScene.unity`, `Prologue_Sequence.asset`, persistence keys, `TutorialScene`, saved progress, and stage selection as untouched reference/fallback until the new 3D version passes the acceptance gates. A static-panel slideshow is **not** the accepted final delivery.
5. The 3D cinematics may condense adjacent storyboard panels into continuous **shots/sequences** while preserving chronology, dialogue meaning, approved characters, props, and major visual revelations. Use recorded dialogue as the final timing authority.

## 2. Source-panel-to-environment map (production starting point)

| Source panels | Narrative beat | Reusable 3D environment | Practical construction approach |
| --- | --- | --- | --- |
| 01 | Van/Nevali family memory | Home / family-memory set | Small camera-visible space with reusable period props |
| 02–03 | Broken Language, Great War, word magic | Battlefield / war set | Modular terrain, rubble, silhouettes, controlled combat VFX |
| 04 | Devastated family home | Damaged home variant | Reuse home layout but swap damage, debris and lighting |
| 05 | Young Van seeks refuge | **Canonical library exterior entry** | Lower-right doorway / stained glass / banners, shared with interior |
| 06–15 | Confession, grief, time jump, dream, Nevali contact | **Library front study / main table** | Same table, shelves, window, gateway; era/lighting/actor changes by shot |
| 16–18 | Pass through central arch, retrieve ancient volume | **Library red-carpet passage / rear archive** | Continuously linked to study via one arch |
| 19–33 | Chronicle, questions, Nevali writing, revelation, forbidden technique decision | **Library study / book close-up zone** | Reuse study and table; add hero ancient-book prop and magical overlays |
| 34–35 | Vander leaves/re-enters with staff | **Library gateway / passage** | Exact matching camera axis and physically correct return route |
| 36–37 | Forbidden casting, Van transported into book | **Library study / magic action zone** | Same room with staff-to-book effect and character transfer choreography |
| 38–42 | Van wakes, retrieves staff, approaches bridge | **Existing TutorialForest / bridge-inspired set** | Inspect and reuse production geometry/materials safely; match pre-game handoff |

**Potential distinct production sets:** home (clean/damaged variant), battlefield, **one canonical complete library**, and TutorialForest. Do not count every panel or camera angle as a new map. Confirm final list after shot-by-shot inspection of the 28 composite pages (not yet visually audited for this new plan).

## 3. Technical design and ownership

- **Unity**: Unity 6.3 LTS URP; `com.unity.timeline` 1.8.10 is already in the manifest. The project does **not** currently declare `com.unity.cinemachine`; simple direct camera Animation Tracks can work initially. Do not add Cinemachine or new packages without an explicit benefit/compatibility check.
- **Workbench**: `Assets/Scenes/Cinematics/Storyboard3D_CinematicWorkbench.unity` is the isolated prototype, deliberately excluded from Build Settings. Its 15 cube mesh renderers and layout are placeholders, **not** an approved version of the canonical library layout. Rebuild the blockout from the top-down reference before calling layout complete.
- **Environment**: Rodin primarily for modular architecture/props, Blender for spatial alignment, mesh cleanup, low-poly variants, pivots, UVs, material batching and asset export. Avoid baking the complete library into one huge GLB.
- **Characters**: reuse canonical Van Aert (young and older eras), Vander Poel, Nevali models/reference designs; make independent cinematic-only Animator instances. **Do not instantiate the persistent gameplay PlayerRoot as the Timeline actor.**
- **Shared Vander Poel visual identity (user confirmed 2026-10-21)**: build **one canonical Vander Poel old-wizard 3D character model and compatible humanoid visual rig** for both storyboard performance and the Final Stage boss. The user's `Character: Vander Poel - OLD` four-view turnaround is the visual authority: preserve the old scholar face, blue eyes, loose white-grey hair, broad beard, glasses, teal/navy layered hooded mantle and long robe, exact gold/celestial ornament layout, blue diamond chest gem, rear cloak emblem, belt/pouch and brown boots. **Same character art, separate prefab/controller ownership**: a clean cinematic-only actor/Animator for Timeline versus a boss actor with gameplay Animator, AI, combat/HP/hit logic. Do not attach boss scripts to the cinematic actor, or cinematic tracks to the boss controller. No final-stage gameplay edits are authorized by this asset decision.
- **Rodin model-first exception**: Rodin remains primarily for modular environment meshes but is explicitly authorized for Vander Poel's missing character model. Generate from the approved turnaround before broad library assets. Target an unobstructed full-body neutral A/T pose with sane anatomy, clear hands/face and body/clothing separation. The wizard staff is a **separate prop** based on its approved staff reference, not fused to hand/body; protect grip/animation compatibility. Blender cleanup, retopology/UV, Mixamo humanoid mapping, cloth/skirt deformation, and actual Unity rig import must be tested before claiming the mesh production-ready. Character generation is **not yet completed**.
- **Animation**: Mixamo baseline locomotion/idles/gestures; LLM-assisted mocap for story-specific performances; validate licenses, clip availability, humanoid mapping, retargeting, hand contacts, staff grip and root-motion strategy. Dialogue requires appropriate acting, not just characters wandering while talking.
- **Direction**: Timeline PlayableAssets with Animation Tracks, camera transforms/cuts, Activation Tracks, Audio Tracks, and optional control/VFX tracks; a dedicated PlayableDirector per isolated cinematic sequence. Do not bind combat controllers to cutscene tracks.
- **Voice/audio**: user records the dialogue; obtain temporary scratch tracks early to lock timings, then replace with mastered files. Plan speaker-specific clips, room tone, footsteps, music/SFX and optional subtitles without permanently baking dialogue into artwork.
- **Safety**: do not wire Main Menu or Stage Complete routing, overwrite comic scenes, add scene to Build Settings, reset PlayerPrefs, or change stage-clear/review flags during cinematic production. Integration is a separate gated task.

## 4. Production phases and gates

### Gate 0: Canonical Vander Poel character asset (immediate next step)

- [ ] Use the user-provided old Vander Poel four-view reference to produce a neutral-pose Rodin draft, matching **front, side and back** silhouette and distinctive chest/back gold motifs. Avoid submitting the full sheet including typography as a single geometry-driving image if the generator supports cleaner isolated views.
- [ ] Inspect Rodin mesh front/side/back, face, hair and robe; iterate any lost blue chest gem, spectacles, sleeve trim or large rear emblem against the approved turnaround. No new character costume or magical form without approval.
- [ ] Export the best draft separately from the staff and assess humanoid rigging/animation readiness in Blender + Mixamo. A textured mesh is not proof of a usable rig; verify hands, shoulders, pelvis, long skirt and cloak deformation with sample dialogue/attack motions.
- [ ] Establish one shared Vander visual/model asset with **two separate derived Unity actor prefabs** (Timeline-only performance and gameplay boss). Only wire Final Stage boss logic in its own authorized gameplay task; do not change the live final-stage scene during cinematic model preparation.

**PASS means:** approved identity retained in the 3D draft, recoverable exported geometry, humanoid animation test and a clear shared-art/separate-controller plan. Real cinematic and boss integration remain separately testable milestones.

### Gate A: Shot list and physical set layout

- [ ] Review all 42 approved panel beats against all 28 final pages; record **shot ID, original panels/pages, location, characters, action, spoken line, camera angle, estimated duration, required prop, FX**.
- [ ] For the library, translate `Library_TopDown_Layout_REFERENCE.png` into a measured modular blockout: entry → study table → central arch → red runner → rear archive. Verify the view axes for Panels 5, 6–8, 17–18, 34–35, 36–37.
- [ ] Produce quick Game View frames at the important camera markers and compare composition against source references; reject drifting architecture **before** generating Rodin assets.

**PASS means:** a signed-off shot-to-place matrix and a navigable/visually coherent library greybox, with no impossible table/door/archive relationships.

### Gate B: One complete animated proof-of-concept

- [ ] Select an isolated **15–30 second** library dialogue/reaction beat from approved Panels **6–8**, without rewriting speech or adding nonapproved dialogue.
- [ ] Place correct-era Young Van + Vander, bind two reusable idle/talk/reaction Mixamo or mocap animations, and animate at least two camera shots with visible camera movement or intentional cuts.
- [ ] Bind a scratch voice recording and time movement/eye line/gestures to it; support a silent reaction moment and fade in/out.
- [ ] Run in the isolated scene in **actual Unity Play Mode**. A still frame with a voice track alone does not count.

**PASS means:** real 3D body/face/gesture performance and camera sequence are visible, the voice timing is correct, props do not detach, no missing Timeline bindings/log errors. No Android speed claim yet.

### Gate C: Build the approved library set

- [ ] Start with environment asset list: floor/wall/arch module, doorway, window/banners, shelf modules, red runner/rug, main table and chairs, chandelier/practical lights, archive shelves, ancient volume and a few repeatable dressing props.
- [ ] Generate modules in Rodin with consistent scale and material direction; improve and split in Blender. Keep their pivots, collision choices and import scale documented.
- [ ] Replace greybox **section by section**, checking shot matching; preserve camera/actor timings from Gate B.
- [ ] Build actual LOD0/LOD1/LOD2 meshes and Unity LODGroups **only for assets that justify them**. For short indoor corridors, occlusion, visibility and reduced shadows may matter more than distance LOD.

**PASS means:** all library-required shots can be framed in one coherent optimized model set; no hidden duplicate room, missing backfaces, broken staff/book contact, or giant individual GLB.

### Gate D: Complete the remaining prologue sequences

- [ ] Work in coherent shot groups: library arrival → grief/time jump → archive/book → recognition/warning → staff retrieval → forbidden casting → transfer.
- [ ] Then create/home and battlefield flashbacks; reuse/inspect the existing TutorialForest set for the awakening/bridge transition.
- [ ] Lock the final voice line lengths, retarget Mixamo + mocap animation, align prop movement/contact and camera timing; capture a shot review clip after each group.
- [ ] Preserve early/older Van differences and canon staff/Nevali designs; do not invent additional scenes because a generated asset looks interesting.

**PASS means:** every narrative beat in the 42-panel map is represented by an approved 3D cinematic shot/transition, with no missing location or dialogue.

### Gate E: Performance and delivery validation

- [ ] Profile PC first, then the real Samsung Galaxy A05s at landscape 2400×1080; aim for **stable 30 FPS** rather than claim 60 without evidence.
- [ ] Audit CPU Animation/Animator cost, SkinnedMeshRenderer counts, draw calls, GPU frame time, textures/mips, shader variants, light/shadow count, VFX, Timeline binding memory and scene load/unload peaks.
- [ ] Use LODGroups where useful, reduce unnecessary visible environment, cap distant/hidden animation updates where safe, favor baked lights, and avoid loading all set locations at once.
- [ ] Check camera near/far clipping, subtitle readability, voice playback, synchronization, skip/fast-forward, interrupted playback, scene loading, and repeated play without leaks or duplicated actors.

**PASS means:** cinematic completes from start to finish on PC and physical A05s at measured acceptable performance, without black screens, stalled Timelines, silent/missing audio, or excessive RAM.

### Gate F: Gameplay and review-safe integration (LAST, separately approved)

- [ ] Decide the exact cinematic-request, resume, skip and durable-completion contracts; ensure existing Prologue completion is not accidentally reset.
- [ ] Route the accepted animated Prologue into Tutorial without duplicates, and handle post-stage/final cutscenes **only after** valid clears, real final-stage content and mandatory review rules are implemented.
- [ ] Test clean and existing saves, skipped/replayed sequences, full Stage Select chain, Android restart, and return to gameplay.

**PASS means:** cinematics neither unlock stages early nor bypass vocabulary review, and interrupted or skipped sequences cannot corrupt progression.

## 5. Practical task order / project priority

The user's immediate gameplay commitments remain Android build, Vander Poel progress, Stage 3 optimization and final-stage push. **Do not derail them by trying to author all 42 panels in one weekend.** The user has now prioritized a **single Vander Poel character model shared visually by the ending boss and the storyboard**; generating and validating that character is the first concrete cinematic asset task. Produce the actual 3D pilot after the character and scene prerequisites, without implying that the boss logic is complete.

First concrete handoff after planning: `GATE_0` Vander Poel Rodin draft, Blender/Mixamo humanoid verification and shared-art/separate-prefab contract. Then `GATE_A` panel/shot table, measured continuous library blockout and storyboard camera review. Then pilot Panels 6–8 with correct-era Young Van, this Vander, Mixamo/mocap, scratch voice and Timeline. Do not manufacture an actor or completed Timeline from an empty placeholder.

## 6. Explicitly unresolved, not assumed

- The actual user-approved panel-to-shot durations and final scene cuts are pending.
- The final 28-page composite edit has not been visually audited against all 42 original panels for this conversion.
- Some old docs still prescribe the rejected comic-only production direction; **this plan supersedes that presentation choice** but does not rewrite or delete historical evidence.
- Approved voices, music/FX, custom mocap provider/rig constraints and final scene-loading strategy await prototype evidence.
- The formal academic-document corrections are **separate and pending user instructions**.
