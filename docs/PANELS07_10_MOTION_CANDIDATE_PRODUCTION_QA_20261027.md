# SPELLSTRIKE Panels 07-10: Motion Candidate Production QA (2026-10-27)

## Status
Four new independent 8-second motion-preview candidates produced. These are NOT approved by the user and NOT Unity Timeline-integrated cinematics. Original P07-P10 still-frame reviews and their previously opened contact sheet remain unchanged. Approved Panels 01-06 and canonical gameplay Van staff (`Assets/VAN AERT/STAFF/base_basic_shaded.glb`) remain untouched.

## Preflight / safety
- VALORANT-Win64-Shipping.exe absent at preflight and again before batch motion rendering.
- Read AGENTS.md, routing policy, story manifest and scripts. Classification: established-design incremental cinematic production (Terra-type task), no systemic redesign.
- COS command, Blender 5.0.1 headless, and Desktop window enumeration available.
- Existing Unity editor is currently open with Stage3Scene. Do not open/save/replace it, invoke Unity MCP or launch a second Unity instance.
- Blender GUI has unrelated Panel04 source open. Leave unchanged.
- Older approved source and review files were not overwritten.

## New versioned `.blend` candidates
Each source scene's existing character actions are preserved, with a small camera-forward drift (4.8 cm over 192 Blender frames); existing one-shot composition retained. New `MotionCandidate_v01` Blender scene files live beside their predecessor:

- P07: `ArtSource/LibraryProps/LibraryShell/LibraryStudyArchive_P07_SilentGrief_v02_MotionCandidate_v01.blend`
- P08: `ArtSource/LibraryProps/LibraryShell/LibraryStudyArchive_P08_VanderContemplates_v02_MotionCandidate_v01.blend`
- P09: `ArtSource/LibraryProps/LibraryShell/LibraryStudyArchive_P09_DecadesLater_v03_MotionCandidate_v01.blend`
- P10: `ArtSource/LibraryProps/LibraryShell/LibraryStudyArchive_P10_OlderVanDreamConfession_v02_MotionCandidate_v01.blend`

Each corresponding `ArtSource/Cinematics/PanelXX_v01/Reviews/MotionCandidate_20261027_v01/` contains `candidate_meta.json`, actual Blender-rendered beginning/middle/end PNGs, 96 frame-by-frame PNGs in `PreviewFrames_12fps/`, and `PXX_MotionCandidate_8s_12fps_v01.mp4`.

## Encoded preview QA
- For EVERY panel: 96 real Blender-rendered frames, 640 x 360 PNG sequence, encoded 12 fps MP4, duration 8 seconds, successfully decoded frames 0 / 48 / 95. Rendered still review triplet: Blender scene frames 1 / 96 / 192, 960 x 540.
- Measured maximum difference from first sampled frame in mean absolute grayscale values: P07 9.710, P08 14.633, P09 10.320, P10 10.596. Nonzero animation/camera change confirmed.
- Numeric verification is in `Library/Panel07_10MotionAudit/Panels07_10_MotionClips_QA_20261027.json`.
- Separate NEW twelve-view sheet: `ArtSource/Cinematics/Panels07_10_Review_20261027/Panels07_10_NEW_MotionCandidate_BME_ContactSheet_v01.png` (distinct from original contact sheet).
- No audio generated; no speculative dialogue. Motion candidates are silent.

## Specific narrative/asset state
- P07: Younger Van bowed, alone, hand/chest IK inherited from v02. Arm/sleeve silhouette still needs user's close-up decision; Vander deliberately hidden.
- P08: Vander alone, hand near chin, seated thoughtful rig with baked character action inherited from v02.
- P09: older white-haired and bearded Van seated foreground, Vander standing at original Gothic gateway; existing foreground camera correction inherited from v03. Gameplay older Van texture reference exists.
- P10: older Van speaks with restrained arm movement; Vander listens at same study table; canonical staff hidden from render; no Nevali projection, new dream effect or dialogue. Exact pair-of-chairs composition from predecessor still requires approval.
- Canonical staff asset was not modified, duplicated, or silently substituted. Existing P09 staff is inherited in-scene.

## Technical limitations / outstanding gates
1. User has NOT approved the compositions/poses. Specifically review P07 concealed right hand, P08 chin touch readability, P09 older Van's framing relative to gateway Vander and staff presence, P10 relative seating and conversational gesture/empty main table.
2. This is a 640x360, 12fps motion review, NOT a full-quality 1920x1080 24fps master render or polished final film.
3. Blender media textures: some inherited paths point to old Downloads/Mixamo temp locations. Blender renders succeeded but material texture portability should be fixed ONLY after assessing which are packed/referenced and validating restored output.
4. No GLB/FBX export yet, no Unity Timeline or Unity Game View playback. Animation retarget/export and on-screen Editor review are separate gates. DO NOT report Unity test success.
5. The new preview scenes preserve existing static compositions and add restrained camera motion; a shot-cut design was NOT introduced because final composition and exact transitions remain unapproved.
6. Blender addon logs `127.0.0.1:60600` port binding warnings in headless sessions, but actual Blender renders and saved scenes passed. Avoid interfering with GUI addon or altering user Blender configuration without cause.

## Source and tools
- `Library/Panel07_10MotionAudit/audit_scenes.py`
- `Library/Panel07_10MotionAudit/create_motion_candidates.py`
- `Library/Panel07_10MotionAudit/render_three_frames.py`
- `Library/Panel07_10MotionAudit/render_motion_frames.py`
- `Library/Panel07_10MotionAudit/create_new_contact_sheet.py`
- `Library/Panel07_10MotionAudit/encode_and_verify_clips.py`

## Unity handoff, after visual signoff and safe Editor state
1. Inspect the live Unity Editor for unsaved Stage3 work, and do NOT switch or save until independently safe or the user frees this session.
2. Export actual animated actor models and camera/material-compatible content from each approved versioned Blender scene, using new output folders under `Assets/Cinematics/Storyboard3D/Panels07_10/` without importing through Unity MCP.
3. In permitted visible Unity Editor GUI, create new distinct versioned cinematic scenes and individual 8-second Timeline assets. Reuse existing canonical actors/library, bind animations and cameras with exact frame/time alignment. Add appropriate shot cuts only where visually authorized.
4. Capture initial, middle and final Game View plus Timeline position; run full playback recording. Audit sky/materials/character identity, clips, clipping and duration. Compare against approved storyboard and revised Blender reference.
5. Do not modify player, Stage3Scene, Stage4, gameplay, surveys or other panels.

## 2026-10-27 follow-up: independent Unity export preparation

Safety: VALORANT absent; AGENTS.md reread. Desktop list again showed Unity 6.3 Stage3Scene active and separate Blender GUI Panel04 open. These remain untouched; no Unity MCP used, no second Editor launched. Prior MP4s/contact sheets untouched. No user approval inferred.

### NEW tangible files (not Unity-integrated)
The root `ArtSource/Cinematics/Panels07_10_UnityExport_20261027/PanelXX/` now contains:
- `P07_YoungVan_Animated_v01.glb` (P07 only)
- `P08_VanderPoel_Animated_v01.glb` (P08 only)
- `P09_OlderVan_Animated_v01.glb` and `P09_VanderPoel_Animated_v01.glb`
- `P10_OlderVan_Animated_v01.glb` and `P10_VanderPoel_Animated_v01.glb`
- for EACH panel: `PXX_CanonicalLibrary_Env_v01.glb` and `PXX_UnityCameraTiming_v01.json` plus `export_manifest_v01.json`.

Actual Blender glTF export returned six nonempty animated models, each structurally parsed with one or more animations, skinned meshes and embedded images. Model rig scales remain as authored; avoid blindly normalizing scale in Unity. Environment GLBs parsed as valid GLB 2.0, with 217/230/217/217 exported meshes and 18 image assets each. These are packages for Unity import, not verified Unity rendering. Camera timing metadata is in Blender native space, requiring conversion/reconciliation in Unity.

Scripts and evidence:
- `Library/Panel07_10MotionAudit/export_inventory.py`: audited rig/object visibility and packed images.
- `Library/Panel07_10MotionAudit/export_animated_actors_v01.py`: exports visible rigged actors by panel.
- `Library/Panel07_10MotionAudit/check_export_glbs.py`: verified all six GLBs have glTF animations, skins, meshes, image contents.
- `Library/Panel07_10MotionAudit/export_library_environment_v01.py`: exports panel-specific canonical library mesh objects, with no hidden actor rigs.
- `Library/Panel07_10MotionAudit/check_export_bundle.py`: parses all environment GLBs and manifest/camera bundles.
- `Library/Panel07_10MotionAudit/approved_vs_motion_20261027.png`: new side-by-side storyboard vs motion midpoint sheet, without overwriting either earlier review sheet.

### Important current limitations
- New 1280x720/24fps MP4 masters not produced in this follow-up; last known proven videos are four 640x360/12fps v01 motion candidates.
- The P07 chest hand and other subjective visual composition questions are still unresolved; no unjustified pose claim made.
- No corrected motion-v02 Blender revisions were saved beyond the existing MotionCandidate_v01.
- GLB content structurally validated but actual imported scale, rig animation playback, shader fidelity, face/hand positions and Unity camera matching still untested.
- Active Stage3Scene editor blocked safely creating/binding versioned Unity scenes/Timelines or capturing Game View. Scene/Timeline paths remain NOT CREATED. Must use visible Editor only after Stage3 work is safely saved/cleared by user; never invoke Unity MCP.
- Canonical staff `Assets/VAN AERT/STAFF/base_basic_shaded.glb` unchanged; staff in existing P09 scene needs explicit visual fidelity review after import. No speculative narration, dream imagery or changes to Panels01-06.
## Same-day production continuation: 07-10 refinements and guarded Unity builder

Safety rechecked: VALORANT not running. Active Unity Stage3Scene and unrelated Blender Panel04 remain open/untouched. No Unity MCP, no Editor scene switch, no second Editor, and no changes to gameplay or Panels01-06.

### Visual evidence and corrections
- Compared actual approved storyboards against old beginning/middle/end rendered motion frames in `Library/Panel07_10MotionAudit/Panels07_10_Storyboard_BME_Audit_v02.png`. P07 was not showing clear hand-to-chest; P08 frame angle differs substantially from approved central frontal portrait; P09 and P10 significantly wider/visually different from approved compositions.
- Created new `CinematicRefinement_v02.blend` from prior `MotionCandidate_v01.blend` for EACH P07/P08/P09/P10 under `ArtSource/LibraryProps/LibraryShell/`; no overwrite. Actual 960x540 beginning/middle/end v02 frames at `ArtSource/Cinematics/PanelXX_v01/Reviews/CinematicRefinement_v02/`; new sheet `Library/Panel07_10MotionAudit/CinematicRefinement_v02_Review_BME.png`.
- P07 v02 hand IK moved too high near shoulder; visually rejected. Corrective new `LibraryStudyArchive_P07_SilentGrief_v02_CinematicRefinement_v03.blend`, rendered first/mid/end frames under `ArtSource/Cinematics/Panel07_v01/Reviews/CinematicRefinement_v03/`. Midframe confirms forearm crosses chest, but the oversized sleeve still conceals the hand. NOT approved and NOT a solved pose.
- P08 v02 tighter shot, chin pose preserved. P09 v02 narrower foreground Older Van + gateway Vander. P10 v02 closer framing of Older Van + Vander, yet still wide relative to storyboard. All are review candidates, no inferred approval.

### Unity assets and tooling prepared, NOT yet played
- Existing glTF/GLB v01 actors/environment exports copied, without modifying originals, into `Assets/Cinematics/Storyboard3D/Panels07_10_v01/PanelXX/`. 18 total files: four environment GLBs, six actor animation GLBs, camera metadata, and export manifests. Source paths remain `ArtSource/Cinematics/Panels07_10_UnityExport_20261027/PanelXX/`.
- Prepared `Assets/Editor/Cinematics/Panels07_10TimelineAssemblerCandidateV01.cs`: visible Editor menu `SPELLSTRIKE/Cinematics/Panels 07-10/Build Isolated Unity Candidates v01`. Guarded against unsaved active scenes, Stage3Scene active, missing imported source assets, or versioned output overwrites. Intended scene paths: `Assets/Scenes/Cinematics/Prologue_PXX_TimelineCandidate_v01.unity`; Timeline paths: `Assets/Cinematics/Storyboard3D/Timelines/Panels07_10_v01/PXX_Cinematic_v01.playable`. THESE OUTPUT SCENES/TIMELINES DO NOT YET EXIST, builder has NOT been compiled/tested, and its provisional camera needs visible calibration. Do not equate this scaffold with integrated cinematics.
- Existing imported GLBs v01 are not automatically synchronized with new Blender refinement v02/v03 files; re-export refined candidates after pose/framing acceptance and inspect actual clips/import fidelity.
- Existing MP4s remain unchanged at 640x360 12fps. New 1280x720 24fps masters and Unity Game View playback not yet produced.
- Visible Editor is on Stage3Scene; preserve unsaved work. No Unity live validation attempted.

Remaining: actually solve P07 hand silhouette; review P08-P10 match; render new high quality complete motion, export corrected assets and verify animation; compile, execute and calibrate builder only in safe Editor, then inspect actual Unity Game View/Timeline and capture proof. Keep subjective composition approval open.
## Latest continuation: true 1280x720 / 24fps delivery-motion render batch

VALORANT preflight: clear. Unity editor remained Stage3Scene; Blender GUI held unrelated P04. No Unity MCP or second editor used. Confirmed previous candidate Blender files, exports, source scripts, approved manifest, and v01 builder. Reused versioned P07 CinematicRefinement_v03 and P08-P10 CinematicRefinement_v02 without overwriting them.

### Production results, NEW
- Four independent 192-frame Blender Eevee PNG sequences rendered at 1280 x 720, 24 fps under `ArtSource/Cinematics/PanelXX_v01/Reviews/CinematicDelivery_v03/PXX_001.png` through `PXX_192.png`.
- Encoded and decoded four versioned 8.00-second MP4 previews at `ArtSource/Cinematics/PanelXX_v01/Reviews/CinematicDelivery_v03/PXX_CinematicDelivery_1280x720_24fps_v03.mp4`. Decoder checks at frames 0, 95, 191 succeeded. Metadata path `Library/Panel07_10MotionAudit/delivery_v03_mp4_validation.json`.
- New 12-image begin/middle/end contact sheet `ArtSource/Cinematics/Panels07_10_Review_20261027/Panels07_10_NEW_Delivery_v03_BME_ContactSheet.png`, visually inspected. Opened new sheet via Windows Invoke-Item. Original contact sheets/older MP4s remain untouched.
- Source scripts `Library/Panel07_10MotionAudit/render_delivery_v03.py`, `encode_delivery_v03.py`, `make_delivery_sheet_v03.py`.
- Visually inspected current sheet: all four render images contain intended character counts and distinct outfits/age, but P07's grieving hand is still concealed in oversized sleeve and P10 has a prominent blank tabletop; treat as review candidate only.
- The existing `Assets/Editor/Cinematics/Panels07_10TimelineAssemblerCandidateV01.cs` remains a GUARD-ONLY, UNCOMPILED, UNEXECUTED generator; source patch removed scene-directory creation. Unity scene and Timeline assets do NOT exist. It still has provisional camera coordinates, so it cannot be accepted without actual playhead/Game View inspection.
- New corrected Blender refinements have NOT been re-exported to character GLBs. `Assets/Cinematics/Storyboard3D/Panels07_10_v01/` still carries previous v01 GLB exports, not the latest shots.

### Critical remaining work
1. Solve P07 hand visibility without unnatural sleeve/IK distortion; photograph at start/middle/end. User approval required for exact composition.
2. Re-export corrected/approved v02/v03 actors and camera-compatible sets to new GLB paths, ensuring correct action and transform, test import motion and materials in Unity.
3. In a safely available visible Editor (Stage3 no unsaved work), compile/execute/create four cinematic scenes and four Timelines with matched calibrated camera, character animation tracks, actual lighting and cuts. Capture Game View and Timeline playback. No Unity playback PASS claimed.
4. Final film polish and user visual approval pending all four shots.
## Follow-up: latest refined actor exports staged for Unity, 2026-10-27

Safety preflight: VALORANT clear. Live Unity continues Stage3Scene; unrelated Blender P04 remains open. No Unity MCP used. All original candidates, approved Panels01-06, staff, prior preview MP4s/contact sheets untouched.

**New actual refined animation exports** from P07 `LibraryStudyArchive_P07_SilentGrief_v02_CinematicRefinement_v03.blend` and P08-P10 corresponding `CinematicRefinement_v02.blend` scenes, into `ArtSource/Cinematics/Panels07_10_UnityExport_20261027/PanelXX/RefinementExports_v02/`:
- P07 `P07_YoungVan_CinematicRefinement_Animated_v02.glb`
- P08 `P08_VanderPoel_CinematicRefinement_Animated_v02.glb`
- P09 `P09_OlderVan_CinematicRefinement_Animated_v02.glb`, `P09_VanderPoel_CinematicRefinement_Animated_v02.glb`
- P10 `P10_OlderVan_CinematicRefinement_Animated_v02.glb`, `P10_VanderPoel_CinematicRefinement_Animated_v02.glb`
- Each has `PXX_CameraRefinement_v02.json`. Blender export calls succeeded for all six; actual imported Unity playback remains unverified.

**New staged assets** copied without overwriting v01:
`Assets/Cinematics/Storyboard3D/Panels07_10_v02/PanelXX/` holds corresponding refined actor GLBs, camera metadata, and existing canonical-library environment GLB. Earlier `Panels07_10_v01` was preserved.

**Updated independent Editor generator candidate**:
`Assets/Editor/Cinematics/Panels07_10TimelineAssemblerCandidateV02.cs` references v02 actor GLBs and prepares new versioned output paths:
- `Assets/Scenes/Cinematics/Prologue_P07_TimelineCandidate_v02.unity` through P10
- `Assets/Cinematics/Storyboard3D/Timelines/Panels07_10_v02/PXX_Cinematic_v02.playable`.
This tool is NOT compiled/executed or validated in Unity. Existing active Stage3Scene safety guard still refuses to run there; source includes a provisional camera requiring visible calibration. **Scenes and Timeline assets remain NOT CREATED.**

**Known unfinished** P07 sleeve still covers hand, P08/P09/P10 composition subject to user review, P07-P10 v03 MP4s are valid Blender previews, not real Unity Game View footage. Do not claim finished Unity cinematics or actual Unity playback pass. On safe Editor session: compile, inspect glTFast imported clip motions, correct poses/cameras/materials, execute builder and validate Game View & Timeline playhead across all four. Do not disturb unsaved Stage3 work.
## Current execution attempt: safe additive builder preparation

- Preflight `VALORANT-Win64-Shipping.exe`: not running. Windows desktop list continues to show Unity Stage3Scene and a separate unrelated Blender Panel04 GUI window. No Unity MCP called and no extra Editor launched. The four v02 cinematic scene and Timeline output paths still do not exist; no Unity playback has been performed.
- Reviewed approved Panel07-10 storyboards/asset manifest, `PXX_CameraRefinement_v02.json`, v02 staged GLBs, assembler source and prior QA. Previously rendered and decoded 1280x720/24fps `CinematicDelivery_v03` clips remain untouched.
- Changed only `Assets/Editor/Cinematics/Panels07_10TimelineAssemblerCandidateV02.cs` to use `NewSceneMode.Additive`, explicitly assign newly created root/director/camera GameObjects to the new scene, save and close only the new scene. **The dirty-Stage3 scene safety gate remains active.** These are source-level changes, NOT compiled, executed or proven safe by actual Unity validation.
- Existing limitations: v02 builder has a provisional uncalibrated camera, picks longest available animation clip rather than the specifically approved action, and cannot yet be considered a fully working cinematic generator. Do not run with dirty Stage3Scene or claim PASS until corrected and visibly tested.
- No new Unity scene or Timeline files generated. Technical and visual approval gates remain the same, including P07 concealed hand.
## October 27, latest protected Unity integration attempt

- Initial `VALORANT-Win64-Shipping.exe` safety check CLEAR. Live Unity remains `SPELLSTRIKE - Stage3Scene - Android - Unity 6.3 LTS`; unrelated Blender GUI still opens P04 ruined home. No Unity MCP, no new Editor or scene switch/save/discard of Stage3Scene. Approved Panels 01-06, old 1280x720 videos, staff GLB and contact sheets unchanged.
- Inspected v01/v02 assembler sources, approved storyboard manifest and images, newest staged `Assets/Cinematics/Storyboard3D/Panels07_10_v02/PanelXX` exports and camera metadata. Four v02 Unity cinematic scene/Timeline outputs remain ABSENT.
- New versioned source-only candidate `Assets/Editor/Cinematics/Panels07_10TimelineAssemblerCandidateV03.cs`, based on existing v02 guarded additive approach. References existing v02 animated actor GLBs and authentic environment, targets separate `Assets/Scenes/Cinematics/Prologue_PXX_TimelineCandidate_v03.unity` and `Assets/Cinematics/Storyboard3D/Timelines/Panels07_10_v03/PXX_Cinematic_v03.playable`. Added distinct per-panel approximate camera pose/field of view from the Blender refinement camera JSON instead of generic zero-rotation camera.
- Source guard scan `Library/Panel07_10MotionAudit/audit_v03_builder_source.py` passed balanced braces, additive mode, active dirty scene protection, expected source paths. This is NOT an actual C# compiler result or Unity test; script remains UNCOMPILED/UNEXECUTED. Four Unity scene files are NOT CREATED, and PlayableDirector/Timeline playback has NOT been observed.
- The v03 builder still selects clips by duration, not specific acting action; approximate camera basis conversion is NOT verified with imported Game View; all remain explicit open issues. Never claim integration or playback PASS from this source-level work.
- Important visual issue P07 right hand remains concealed by sleeve, despite prior v03 forearm refinement; end user has not approved compositions.
## 2026-10-27 late safety/execution verification

- Checked `VALORANT-Win64-Shipping.exe`: absent. AGENTS.md read. COS Core and desktop enumeration available. Active Unity Editor remains Stage3Scene, another Blender GUI instance is on unrelated Panel04.
- Attempt to passively inspect existing Unity window via COS Desktop `get_window_state` timed out, so the actual unsaved/dirty state could NOT be established safely. Did not invoke Unity MCP, switch/save active Unity scene, launch a conflicting Editor, or touch approved work.
- Verified the existing Editor assembly at `Library/ScriptAssemblies/Assembly-CSharp-Editor.dll` was last written October 26, before latest v03 assembler source on October 27. Latest v03 **not shown compiled**, let alone executed.
- Verified FOUR intended output scenes `Assets/Scenes/Cinematics/Prologue_PXX_TimelineCandidate_v03.unity` and FOUR Timeline assets `Assets/Cinematics/Storyboard3D/Timelines/Panels07_10_v03/PXX_Cinematic_v03.playable` are still ABSENT. No Unity playback captured.
- Previously verified 4 Blender 1280x720/24fps eight-second videos, 6 refined animation GLBs and library staged assets remain preserved. P07 sleeve-hand visibility and visual approval are unresolved.

Safety outcome: did not force scene switching or claim an unverified Unity result. Independent builders exist, but no new finished Unity scenes were produced in this turn.

## Deferred issue (user request, 2026-10-27 evening): Unity certificate warning, Stage 3

**TO BE FIXED LATER — not part of current cinematic scope.** User screenshot on Stage3Scene shows dialog `Hostname mismatch in secure connection`: certificate hostname `SN-131703000545` does not match requested `generators.ai.unity.com`. Do **not** select `Yes` to bypass certificate hostname validation. Recommend `No`/dismiss and later investigate system clock, trusted certificate chain, Unity AI service/endpoint, proxy interception and full Editor.log. No security override or Stage3 modification performed. User specifically requests continuing Panels 07–10 first.

## Panels 07–10 current Unity evidence (supersedes earlier source-only status)

- Existing versioned scenes: `Assets/Scenes/Cinematics/Prologue_P07_TimelineCandidate_v03.unity` through `P10_TimelineCandidate_v03.unity`, 4 Timelines `Assets/Cinematics/Storyboard3D/Timelines/Panels07_10_v03/PXX_Cinematic_v03.playable`. Unity compiled/executed the one-shot assembler on clean cinematic workbench and created all 8 files.
- Corrected **v04** scenes `Assets/Scenes/Cinematics/Prologue_PXX_TimelineReview_v04.unity` and Timelines `Assets/Cinematics/Storyboard3D/Timelines/Panels07_10_v04/PXX_Cinematic_v04.playable` (PXX = 07,08,09,10), with specific 8-second PXX acting animation clip selection. Supporting Unity logs/metadata: `Library/Panel07_10MotionAudit/UnityReview_v04/UnityReviewV04.txt`.
- 3 Unity render sample points (0.0,4.0,7.9 s) per panel captured in `Library/Panel07_10MotionAudit/UnityReview_v04/PXX_Unity_v04_{0_0,4_0,7_9}s.png`. **Visual QA failed**: P07/P08/P10 images appear fully blocked/blank; P09 frames show Old Van seated but a broad brown wall/divider obstructs environment and hides Vander. Do NOT mark Unity playback/cinematic completion PASS.
- Subsequent non-destructive Unity diagnostic captures `Library/Panel07_10MotionAudit/UnityReview_v05_Diagnostics/PXX_ACTORS_ONLY_4s.png`, `PXX_ACTOR_DIRECT_4s.png` and `results.txt`. Report shows imported actor skinned meshes and camera frustum contain their bounds, yet camera images remain occluded. Most likely contaminating geometry from additively-open base cinematic workbench (its placeholders render along with cinematic scene), not proven until isolated per-camera layer test. Panel 09 actor moves from T-pose when intended clip bound. Original MP4s, all Blender sources, staff, and approved panels preserved.
- User has since switched active Unity back to **Stage3Scene**. Do not run cinematic additive review/rebuild against Stage3 or touch unsaved gameplay changes. Fix isolated cinematic visuals via versioned files only; inspect/capture after safe clean workbench session.

## User-directed cinematic restraint (2026-10-27 evening, supersedes experimental camera motion)

- User specifies **ONLY subtle head bobbing and dialogue**, not elaborate acting or added gestures. **No camera push/cuts, cinematic VFX, dream projections, new dialogue, or new story events.** Keep character identities and story positions, with still cameras, original library, and previously approved scenes protected. This is a correction to prior camera-motion experimentation.
- Dialogue **lines/audio not supplied in this turn**. Do not invent them or assert dialogue is synchronized. For animation, preserve deliberately subtle character motion; verify it is visible and timeline-driven before claiming completion.
- The `Assets/Editor/Cinematics/Panels07_10IsolatedRevisionV05.cs` automatic runner and menu action were disabled to prevent accidental further camera-push changes. Its incomplete v05 scene/Timeline assets were **not promoted** as review-ready. Previous v04 scenes/Timelines remain the last assembled actor-binding revision, and v03 Blender MP4s remain preserved.
- Last v05 attempted additive build failed with `Actor missing CAMERA` for all four scenes. The associated four `v05` files and experimental camera animations exist but are **incomplete experimental assets**; do not use them for final playback or include them as completed cinematics.
- Isolated v04 Unity image captures: `Library/Panel07_10MotionAudit/UnityReview_v05_Isolated/PXX_ISOLATED_{0_0,4_0,7_9}s.png`, PXX=07–10. They show scenes rendered correctly in isolation, but frames are identical at all three sample times. `Library/Panel07_10MotionAudit/CurveAuditV07.txt` and `UnityRigBindingAuditV08.txt` independently show genuine 8-sec imported head/arm curves and rig-connected bone motion; robust Timeline playback evidence remains an open technical gate.
- Active Unity scene at last verified check is Stage3Scene; its gameplay is untouched. User separately deferred Unity certificate mismatch to `docs/SPELLSTRIKE_DEFERRED_ISSUES.md`.

## October 27 final evening milestone: static-camera head-bob-only Unity v07 (supersedes earlier candidate-only entries)

**User art direction was simplified to head bobbing and dialogue only.** No action gestures, camera motion/cuts, dream projections, additional dialogue, magical effects, or new plot events. The dialogue script / voice recording has not been supplied or attached in this Unity milestone; **do not claim voiced/lip-synced dialogue is done**.

### Actual built Unity assets (not just scripts)

| Panel | Authored Blender scene | Preserved/refined GLBs staged under `Assets/Cinematics/Storyboard3D/Panels07_10_v02/PanelXX/` | New Unity v07 scene | PlayableDirector Timeline v07 | Approved 8s Blender MP4 retained |
|---|---|---|---|---|---|
| P07 | `ArtSource/LibraryProps/LibraryShell/LibraryStudyArchive_P07_SilentGrief_v02_CinematicRefinement_v03.blend` | `P07_YoungVan_CinematicRefinement_Animated_v02.glb` and `P07_CanonicalLibrary_Env_v01.glb` | `Assets/Scenes/Cinematics/Prologue_P07_HeadBobDialogue_v07.unity` | `Assets/Cinematics/Storyboard3D/Timelines/Panels07_10_v07/P07_DialogueHeadBob_v07.playable` | `ArtSource/Cinematics/Panel07_v01/Reviews/CinematicDelivery_v03/P07_CinematicDelivery_1280x720_24fps_v03.mp4` |
| P08 | `ArtSource/LibraryProps/LibraryShell/LibraryStudyArchive_P08_VanderContemplates_v02_CinematicRefinement_v02.blend` | `P08_VanderPoel_CinematicRefinement_Animated_v02.glb` and `P08_CanonicalLibrary_Env_v01.glb` | `Assets/Scenes/Cinematics/Prologue_P08_HeadBobDialogue_v07.unity` | `Assets/Cinematics/Storyboard3D/Timelines/Panels07_10_v07/P08_DialogueHeadBob_v07.playable` | `ArtSource/Cinematics/Panel08_v01/Reviews/CinematicDelivery_v03/P08_CinematicDelivery_1280x720_24fps_v03.mp4` |
| P09 | `ArtSource/LibraryProps/LibraryShell/LibraryStudyArchive_P09_DecadesLater_v03_CinematicRefinement_v02.blend` | `P09_OlderVan_CinematicRefinement_Animated_v02.glb`, `P09_VanderPoel_CinematicRefinement_Animated_v02.glb`, `P09_CanonicalLibrary_Env_v01.glb` | `Assets/Scenes/Cinematics/Prologue_P09_HeadBobDialogue_v07.unity` | `Assets/Cinematics/Storyboard3D/Timelines/Panels07_10_v07/P09_DialogueHeadBob_v07.playable` | `ArtSource/Cinematics/Panel09_v01/Reviews/CinematicDelivery_v03/P09_CinematicDelivery_1280x720_24fps_v03.mp4` |
| P10 | `ArtSource/LibraryProps/LibraryShell/LibraryStudyArchive_P10_OlderVanDreamConfession_v02_CinematicRefinement_v02.blend` | `P10_OlderVan_CinematicRefinement_Animated_v02.glb`, `P10_VanderPoel_CinematicRefinement_Animated_v02.glb`, `P10_CanonicalLibrary_Env_v01.glb` | `Assets/Scenes/Cinematics/Prologue_P10_HeadBobDialogue_v07.unity` | `Assets/Cinematics/Storyboard3D/Timelines/Panels07_10_v07/P10_DialogueHeadBob_v07.playable` | `ArtSource/Cinematics/Panel10_v01/Reviews/CinematicDelivery_v03/P10_CinematicDelivery_1280x720_24fps_v03.mp4` |

- Animated head-bob-only 8s clip files (six total): `Assets/Cinematics/Storyboard3D/Timelines/Panels07_10_v06/HeadBobClips/P07_YoungVan_SubtleHeadBob_v06.anim`, `P08_VanderPoel_SubtleHeadBob_v06.anim`, `P09_OlderVan_SubtleHeadBob_v06.anim`, `P09_VanderPoel_SubtleHeadBob_v06.anim`, `P10_OlderVan_SubtleHeadBob_v06.anim`, `P10_VanderPoel_SubtleHeadBob_v06.anim`. These are directly bound to the six corresponding Timeline animation tracks. All non-head animation bindings were held at their initial static pose; the only animated bone is the head quaternion. Nods are deliberately minimal (about 0.45°–1.25° source amplitude, less than 0.5° difference at t=4s). No camera AnimationTrack is present, only an eight-second activation of the stationary camera.
- Generation evidence: `Library/Panel07_10MotionAudit/UnityHeadBobV06_BuildReport.txt` and `Library/Panel07_10MotionAudit/UnityV07_StaticDialogueBuildReport.txt`. Unity compiled and executed both builders, creating four nonempty standalone scene YAML files and four Timeline/PlayableAsset files. The earlier v03, v04 and incomplete v05 iterations remain preserved without promotion.

### Real Unity PlayableDirector/rig validation

- **Actual Unity isolated-Editor PlayableDirector playback verified** using `director.Play()`, manual playhead evaluation at 0.0 / 4.0 / 7.9 sec, and render captures on distinct Editor update frames. Evidence report: `Library/Panel07_10MotionAudit/UnityReview_v07_RealTimeline/V07_Unity_RuntimeReview.txt`.
- Twelve time-identifiable Unity screenshots: `Library/Panel07_10MotionAudit/UnityReview_v07_RealTimeline/PXX_HeadBob_Unity_0_0s.png`, `_4_0s.png`, `_7_9s.png` for each PXX = 07, 08, 09, 10. **All four groups have three distinct screenshot hashes**, corresponding with head bone rotation readings. Actual head-bone displacement from t=0 to 4.0 sec: P07 YoungVan 0.18131°; P08 Vander 0.35827°; P09 OlderVan 0.25334° + Vander 0.25334°; P10 OlderVan 0.49889° + Vander 0.23739°. All camera transform invariants PASS at 0/4/7.9 sec.
- Visual contact sheet of four actual Unity frame captures at 4 sec: `ArtSource/Cinematics/Panels07_10_Review_20261027/Panels07_10_Unity_HeadBob_v07_Time4s_ContactSheet.png`. Additional static-camera comparison images: `Library/Panel07_10MotionAudit/UnityReview_v06_StaticShotScout/`. P09 v07 camera at (1.10,1.85,10.60), 38° FOV removes former foreground post occlusion; P10 v07 camera at (0,1.50,8.70), 38° FOV improves two-person table framing.
- **Safety PASS**: original active `Assets/Scenes/Stage3Scene/Stage3Scene.unity` was clean before and after; list of open scenes unchanged. Unity MCP unused. Blender Panel04 GUI session, prior rendered/decoded v03 MP4s, Canonical Van Aert staff, earlier cinematic variants, approved Panels01–06, gameplay, colliders, Android/URP settings, and Edge review windows left unchanged. `VALORANT-Win64-Shipping.exe` absent at preflight.
- Disabled automatic startup execution of 13 one-shot QA/revision scripts after successful tests to prevent re-running resource-intensive captures on future Unity openings. Menu-invoked utilities remain available.

### Honest remaining gates

1. **Audio/dialogue:** No supplied approved voice files or approved spoken lines were found/attached here. Timeline head-bobbing and scene rendering are verified, but no voice/dialogue synchronization is claimed. Do not invent dialogue.
2. **Subjective framing/art:** P07 Young Van's hand remains partially concealed by his robe sleeve and is not clearly against his chest; a static-pose versioned repair is still needed if the original storyboard requirement is enforced. P08 gesture is subtle but exact chin-contact needs user inspection. P09 Vander near the gateway is visible but posture/composition still differs from the approved illustration; needs user approval. P10 revised framing shows both characters, but table remains foreground and needs user aesthetic approval.
3. **Runtime mode:** Real Unity isolated preview-scene PlayableDirector and frame captures PASS. Separate visible **Game View in Play Mode**, audio playback and Android cinematic build have **not** been independently verified. Do not label these tests PASS.
4. **TLS warning deferred:** Unity AI-service certificate mismatch from user screenshot is logged separately in `docs/SPELLSTRIKE_DEFERRED_ISSUES.md`. Do not click Yes to bypass certificate verification.

**Milestone status:** FOUR functional, saved and verified head-bob animated Unity cinematic scene/Timeline pairs exist at v07, with screenshots and reports. Final spoken-dialogue integration and visual approval remain open, so do not describe these as fully voiced/user-approved cinematics.
