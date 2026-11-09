# SPELLSTRIKE Panels 01–06 Van Staff Continuity Audit

Date: October 27, 2026

## Definitive model

The selected base_basic_shaded MeshFilter and MeshRenderer in the real
Assets/Prefabs/VanAertPlayer.prefab use GUID 77b1ad2bf6b513048a3956905a1c24d8
from Assets/VAN AERT/STAFF/base_basic_shaded.glb. It has 18,557 vertices,
34,112 polygons, an embedded 2048x2048 shaded texture and vertical mesh bounds
0–1.8856 m. Its cyan diamond and gold details match the screenshot.

The base_basic_pbr.glb has the same geometry but different PBR maps, not the
selected screenshot material. Assets/Character/STAFF.glb, GUID
85e409f88beb1c94abee8973c66cf04c, is a distinct 3,623-vertex model
and was incorrectly called canonical for these cinematics.

## Panel-by-panel results

| Panel | Previous | Corrective work |
| --- | --- | --- |
| P01 | No staff object in latest Blender staging; no old GUID in P01 Unity scene references | No change justified |
| P02 | Latest Unity v05 references old GUID85 for visible staff | Versioned P02 Unity v06 scene candidate with exact shaded GUID77 mesh+material, old renderer disabled, glow child preserved |
| P03 | Latest Unity v03 references old GUID85 for visible staff | Versioned P03 Unity v04 scene candidate with same correction |
| P04 | Blender v15 used wrong old model and left three handmade placeholders | Blender v16 correct actual staff laid on floor, three obsolete staff placeholder pieces hidden in derived scene only; Unity v08 imported and observed in Play Mode |
| P05 | Approved static Blender v09 used old model | Blender v10 correct actual staff in equivalent upright location |
| P06 | Approved Blender v04 used old model | Blender v05 correct actual staff beside Young Van; same actors, cameras, timeline and motions |

New source files:

- ArtSource/LibraryProps/DefenseSets/Prologue_Home_04_Ruined_v16_SceneUsedVanStaff.blend
- ArtSource/LibraryProps/LibraryShell/LibraryStudyArchive_P05_StaticArrival_v10_SceneUsedVanStaff.blend
- ArtSource/LibraryProps/LibraryShell/LibraryStudyArchive_P06_Confession_v05_SceneUsedVanStaff.blend
- Assets/Scenes/Cinematics/Prologue_P02_A02_GreatWar_v06_SceneUsedVanStaff.unity
- Assets/Scenes/Cinematics/Prologue_P03_A03_UltimateRelease_v04_SceneUsedVanStaff.unity
- Assets/Scenes/Cinematics/Prologue_P04_A04_GriefStatic_v07_SceneUsedVanStaff.unity
- Assets/Scenes/Cinematics/Prologue_P04_A04_GriefStatic_v08_UnityValidatedSceneStaff.unity

New P04 static GLB export:

Assets/Cinematics/Storyboard3D/Characters/Panel04/Panel04_GriefScene_StaticBlockout_v03_SceneUsedVanStaff.glb

The new full P04 GLB preserves precisely the same 753 node names and node
order as approved v02, but includes corrected shaded staff mesh/material
geometry, with no animations. New GLB has its own .meta GUID.

## Actual Unity Editor validation (October 27, 2026)

Unity 6.3 LTS, 6000.3.10f1, Android platform selected in the Editor,
rendering through Direct3D12 on the development PC. The only main Unity Editor
was operated using COS Desktop/Windows controls, **not Unity MCP**. Before
automation, the VALORANT process check returned `VALORANT_CLEAR`.

**P02 — versioned v06 scene:**

- Successfully imported live. The visible
  `P02_VanStaff_SceneUsed_BaseBasicShaded` GameObject resolves its MeshFilter and
  `model` material. Correct cyan crystal is visible beside Van in the war zone.
- The original `A02_BrokenLanguageWar_BackTurnThunder_v05.playable` reference,
  playable bindings, narration and camera remain serialized and unchanged.
  Source Timeline duration is 8.5 seconds, with 0.50 m lateral camera reveal
  plus three subtle thunder/sky pulses.
- With Stage3 unloaded, the standalone Game View displayed the war-torn city,
  violet pylons/sky, and Van. Captured 42 seconds of real Unity Play Mode.
- **Animation qualifier:** P02's captured Game View remains visually static at
  sampled times; the .5 m camera movement and thunder pulse timing were NOT
  independently confirmed from that footage. This is a remaining motion QA
  item, not evidence that the staff import failed or that animation is broken.

**P03 — versioned v04 scene:**

- Successfully imported live. The
  `P03_VanStaff_SceneUsed_BaseBasicShaded` MeshFilter resolves the exact
  shaded staff and `model` material. The blue crystal is visible at Van's
  pointed staff tip in the low rear framing.
- Original 7.6-second `A03_UltimateRelease_ThunderFireTiles_v03.playable`
  asset and serialized bindings remain intact. It contains a 0.32 m low-rear
  camera push, word-tile spin, fires, and sky/thunder tracks.
- Clean standalone Game View, actual Unity Play Mode MP4, and contact sheet
  confirm Van, shadow threat, word tiles, violet pylons, and changing flame/
  ember particles. No obvious staff clipping or material loss is visible.
- **Animation qualifier:** Ambient particle activity is visible. Exact timeline
  camera displacement and keyframe timing have not been separately measured
  against time-coded Unity PlayableDirector playback.

**P04 — v07 prefab checked, v08 Unity-validated scene:**

- Unity imported the versioned GLB v03; the 753-node named hierarchy resolved.
  Actual shaded staff and blue crystal are visible on the debris-covered floor.
  Non-graphic grief staging and the family composition remain intact.
- The original 10-second
  `P04_A04_Grief_ReturnHome_v06.playable` Timeline and `Panel04A04ShotLockV05`
  are preserved. The locked cut timings are 0, 2.667, 6.042 and 8.458 seconds.
- **Verified in real standalone Unity Play Mode**, with Stage3 unloaded:
  establishing ruins; Van beside his wife and blue staff; closer grief beat;
  final symbolic shot of Nevali's blue bow. The four-shot sequence and existing
  narration were visibly captured in the 10-second MP4 excerpt and inspected
  using contact sheets. No new graphic content.
- `Prologue_P04_A04_GriefStatic_v08_UnityValidatedSceneStaff.unity` is a
  *versioned* Unity Editor save of v07. Diff shows only Unity's newly serialized
  URP AdditionalCameraData and two URP AdditionalLightData components; no
  camera transforms, Timeline source asset, narration, actor staging, or prefab
  reference was changed in this save.

### Unity Game View proof and preview output

All paths below are relative to
`ArtSource/Cinematics/StaffContinuityReview_20261027/UnityEditorValidation/`.

- `P02_v06_ImportedStaffColor_Position.png` and
  `P02_v06_StaffInspector_SceneView.png`: exact staff imported in Inspector.
- `P02_v06_UnityGamePlay_raw42s.mp4`,
  `P02_v06_UnityGamePlay_contactSheet.png`, and
  `P02_v06_UnityPlayMode_WarWide_10s.mp4`: real standalone Unity wide-war
  preview. Motion timing **not** certified.
- `P03_v04_ImportedStaffBlueCrystal_SceneView.png`,
  `P03_v04_UnityGamePlay_raw43s.mp4`,
  `P03_v04_UnityGamePlay_contactSheet.png`, and
  `P03_v04_UnityPlayMode_UltimateRelease_10s.mp4`: actual Game View and
  dynamic ambient embers/fire, correct staff alignment.
- `P04_v07_ActualStaff_GriefSafe_SceneView.png`,
  `P04_v07_CameraShotLock_Inspector.png`,
  `P04_v07_TimelinePlayableAsset_Inspector.png`: live component evidence.
- `P04_v08_UnityGamePlay_raw55s.mp4`,
  `P04_v08_UnityCameraCuts_contactSheet.png`, and
  `P04_v08_UnityPlayMode_FourShot_excerpt.mp4`: **verified real four-camera
  Unity Play Mode sequence**.
- `P04_v08_01_27.00s.png` through `P04_v08_04_34.50s.png`: four extracted
  distinct Unity cuts. Recording is time-coded relative to the recording
  start, not necessarily the Timeline start because Unity entered Play Mode
  while the recording was running.
- The earlier `P03_v04_GameView_SharedStage3Overlay.png` and
  `P04_v07_GameView_SharedStage3HUD_NotClean.png` are **diagnostic only**.
  They were superseded by clean standalone Game View captures.

## Rendered QA

Full replacement render evidence under:

ArtSource/Cinematics/StaffContinuityReview_20261027/

P04/P04_Wide_SceneStaff.png, P04/P04_Van_Grief_SceneStaff.png,
P05/P05_StaticArrival_SceneStaff.png, and
P06/P06_Wide_SceneStaff.png, P06/P06_Van_SceneStaff.png,
P06/P06_Vander_SceneStaff.png, P06/P06_EmotionalTwoShot_SceneStaff.png.

Before/after evidence:

P04/P04_Staff_BeforeAfter_v01.png,
P05/P05_Staff_BeforeAfter_v01.png,
P06/P06_Staff_BeforeAfter_v01.png.

Animated P06 preview (verified real Blender frames):

P06/P06_Confession_SceneUsedVanStaff_10s_v05.mp4

The revised P06 video is 120 unique rendered frames encoded at 12 FPS for
10 seconds from the original editable 24 FPS and 240-frame Blender timeline.

The blue crystal is visible on the P04 floor, near Younger Van in P05, and
beside his left chair in P06. Composition, acting, architecture, non-graphic
grief staging, four P06 cuts, and existing subtle movement remain unchanged.

## Preservation, restored Editor state and outstanding gates

- Original approved Panels 01–06, previous Blender versions, prior GLB files
  and Unity scenes have **not been overwritten**.
- Blender v16/v10/v05 corrections and the approved Panel 06 10-second preview
  were already visually verified in previous work; **not rebuilt or modified**
  during this Unity validation.
- P02 and P03 remain narrow Unity v06/v04 staff-mesh scene variants. The
  original legacy renderer is disabled but preserved, with the actual
  shaded-model renderer parented to the same staff object. Legacy crystal-glow
  child and original Timelines remain unchanged. P04 v08 is a versioned Unity
  serialized scene with the validated v03 static GLB.
- No unrelated Unity scene, gameplay, collider, mobile/URP profile, original
  cinematic or animation source was saved over or modified. The Unity Editor
  was ultimately returned to **Stage3Scene as the active and only loaded
  scene**; P02–P04 are present only as *unloaded* scene entries. No Stage3
  scene save or discard was performed. The Editor Game View returned to Stage3.
- No Android device build or Android device-performance validation is claimed.
  These were Unity Editor runs while the Android build target was selected.
- **Remaining limited checks:** exact timed P02 lateral/thunder and exact P03
  camera/word animation timing are not established by the recorded frames.
  A dedicated monitored Timeline-playhead capture is appropriate before
  treating those animations as fully accepted. The imported staff, appearance,
  main Game View composition, P03 ambient particle visuals, and P04 four-shot
  Timeline playback have separate positive evidence above.
- COS scripts and audit JSON/source checks reside in `Library/Panel06Audit/`.

## Final P02/P03 motion follow-up (October 27, 2026)

This update SUPERSEDES the earlier limited-check paragraph. All previously
approved Blender assets, staff replacements, Unity scene variants, Panel 04
four-shot sequence, and original Timeline source assets are retained.

### P02 v06: camera reveal and thunder

- Original 8.5-second camera clip travels 0.500 m from
  (-5.400, 5.900, 22.600) to (-5.8923917, 5.900, 22.513107).
- Three original thunder-light animation peaks: 2.05s at intensity 0.78,
  5.12s at 0.62, and 7.20s at 0.34. Three storm-sky RGB tracks remain.
- NEW LIVE PASS: real Unity Game View footage following Play Mode startup
  visibly demonstrates the lateral camera reveal and shifting city geometry
  while Young Van and the correct shaded staff remain in context.
- LIMIT: Thunder peak timestamps are verified in the original animation
  keys and Timeline references, but exact on-screen intensity peaks have
  not been independently measured against a calibrated Unity playhead.

### P03 v04: camera, rotating tiles and flames

- Original 7.6-second camera clip advances 0.320 m from
  (-2.600, 1.550, 13.600) to (-2.5666027, 1.5878929, 13.284012).
- Seven distinct P03_TILE_SPIN_A through P03_TILE_SPIN_G quaternion
  rotation tracks and 54 scale-animation curves for 18 fire sites remain.
- NEW LIVE PASS: real Unity Game View footage shows the low-rear camera
  moving, the blue word tiles changing rotation, and fire/embers moving
  throughout the action. Van's Ultimate Release pose, correct blue-crystal
  staff and shadow threat remain visually intact.
- LIMIT: Exact real-time thunder/sky peak timestamps and the complete
  per-keyframe Timeline sampling were not measured using an instrumented
  PlayableDirector time source.

### New targeted evidence

All paths below are relative to:
ArtSource/Cinematics/StaffContinuityReview_20261027/UnityEditorValidation/

- P02 live 10-second excerpt:
  P02_v06_TimelinePlayback_MovingCamera_10s_v03.mp4
- P02 live-motion contact sheet:
  P02_v06_LiveMotion_ContactSheet_v03.png
- P03 live 10-second excerpt:
  P03_v04_TimelinePlayback_TilesFireCamera_10s_v03.mp4
- P03 live-motion contact sheet:
  P03_v04_LiveMotion_ContactSheet_v03.png
- Original, popup-free real Unity window capture sources:
  P02_v06_WindowOnly_PlayFromStart_v03_raw46s.mp4 and
  P03_v04_WindowOnly_PlayFromStart_v03_raw42s.mp4

The camera/word-tile changes were verified across sampled frames of
actual Unity Editor Play Mode recordings, not reconstructed still frames.
The capture uses a non-mutating Win32 PrintWindow method sampling at
approximately 8 encoded FPS, and does NOT provide synchronized frame
times against Unity's internal Timeline. Contact-sheet time labels are
relative to the recording, not to the Timeline start time.

An unrelated recurring McAfee expired-subscription notification
obstructed two previous desktop captures. Those v02 recordings are
diagnostic failures, NOT cinematic evidence. No antivirus settings were
changed and neither Renew nor Uninstall was selected. Window-only
capture excluded the unrelated popup.

### Final status and preservation

- Staff-continuity PASS from earlier P01-P06 validation, unchanged.
- P02 lateral camera reveal: visible live Play Mode PASS.
- P03 camera movement, rotating word tiles, fire/embers: visible live
  Play Mode PASS.
- P04 four cinematic camera cuts: previously confirmed live PASS,
  unchanged.
- Thunder/sky animation source curves: PASS; exact measured real-time
  on-screen intensity peaks: untested.
- No demonstrated scene defects requiring new P02/P03 versions, shader
  or VFX modifications. Original Timeline references and source assets
  were left unchanged.
- Final QA v02: Library/Panel06Audit/Unity_StaffContinuity_FinalQA_20261027_v02.json
  Original v01 QA remains intact. Separate source and prior-video
  audit: Library/Panel06Audit/P02_P03_TimelineMotion_SourceAndPriorRecordingQA_v02_20261027.json.
- Unity was returned to Stage3Scene as the ACTIVE and ONLY loaded
  scene; the desert map Game View was visually confirmed. The Stage3
  scene file retains its original October 21 date. No Stage3 save or
  discard was performed. Approved Blender, gameplay, colliders, mobile
  configuration, graphics settings, and unrelated assets stayed intact.
- No Android device testing or benchmarking was requested or performed.

### Final v03 follow-up: visible synchronized thunder flashes

The **v03 QA result below supersedes the v02 statement that visible thunder
peaks were entirely untested**. A separate non-destructive analysis of the
already-recorded, popup-free v03 live Unity Play Mode sources identified **all
three visually distinct scene-wide flashes in BOTH panels**, rather than
inferring their occurrence solely from source animation keyframes. Each event
brightened fixed image regions in the sky, ruined buildings, and ground
simultaneously. The capture-time observations are intentionally **not**
presented as PlayableDirector keyframe timestamps.

| Panel | Three observed flashes (recording seconds, not Timeline seconds) | Measured mean-image brightness increase at each flash: sky / ruins / ground |
| --- | --- | --- |
| P02 v06 | 17.375, 22.500, 26.500 | +11.13/+20.99/+22.73; +10.43/+24.75/+18.17; +5.64/+14.36/+8.62 |
| P03 v04 | 14.000, 18.250, 22.125 | +26.24/+13.34/+21.69; +31.53/+21.23/+22.53; +15.23/+11.53/+10.67 |

Each comparison uses a frame from 0.50 **recording** seconds before the
flash as its baseline. Quantitative pixel values are supporting evidence,
not a calibrated light intensity probe. Visual comparisons of the actual
Unity frames are preserved in:

- `ArtSource/Cinematics/StaffContinuityReview_20261027/UnityEditorValidation/P02_v06_TripleThunderSkyGround_SyncedLiveFrames_v04.png`
- `ArtSource/Cinematics/StaffContinuityReview_20261027/UnityEditorValidation/P03_v04_TripleThunderSkyGround_SyncedLiveFrames_v04.png`

These contact sheets are crops of **actual Unity window recordings**,
not synthetic, reconstructed, or Blender-rendered frames. They can be
reproduced from the original `P02_v06_WindowOnly_PlayFromStart_v03_raw46s.mp4`
and `P03_v04_WindowOnly_PlayFromStart_v03_raw42s.mp4` with
`Library/Panel06Audit/finalize_p02_p03_visible_flash_qa_v03.py`.
They supplement, not replace, the live camera-motion and word-magic
proof in the existing v03 10-second excerpts.

**Definitive result:** P02's 0.50 m *authored* lateral reveal is visibly
played; the authored three thunder/sky pulse tracks produce three
visually synchronized scene-wide flashes. P03's 0.32 m *authored*
push, seven rotating word tiles, animated fire/embers and three
thunder/sky flashes are visibly played in the approved Ultimate
Release composition. The retained 7.6-second P03 Timeline describes
the Van Aert release as a **static held pose**, not an independent
character attack-animation or projectile-release track. There is
no basis for adding such animation outside the locked art direction.
Character/staff silhouette, context, and scene continuity remain
consistent with the previously approved visuals.

**Remaining timing limitation:** Source keyframe positions are verified
(P02 thunder 2.05 / 5.12 / 7.20 s; P03 inherited storm pulse tracks),
but the unobstructed game captures lack an instrumented, simultaneous
Unity PlayableDirector time readout. Therefore the exact visual peak
frame-to-playhead agreement, individual attack Animator/release event,
and frame-accurate stage synchronization are **not certified**.
These caveats do not erase the verified three visible flashes in each
live sequence.

**Final QA v03 (latest):**
`Library/Panel06Audit/Unity_StaffContinuity_FinalQA_20261027_v03.json`.
QA v01/v02 and all existing videos remain intact. No P02/P03 Unity
scene, Timeline, Blender, staff asset, or other production asset was
changed to obtain the additional evidence. Final active Editor scene
was again observed as `Stage3Scene`; its production file at
`Assets/Scenes/Stage3Scene/Stage3Scene.unity` retained the original
October 21 timestamp, with no save/discard performed here. P04 v08,
P05 v10, P06 v05 and its approved ten-second preview remain approved
and unchanged. Android-device playback and performance remain outside
scope.

## Final Playhead-Calibrated Unity Timeline Evidence (v04)

**This is the most recent timing verification. It supersedes the earlier
v01-v03 uncertainty about whether three visible storm flashes coincide
with the authored Timeline playhead positions.** It does not supersede
the approved staff, Blender, GLB, composition, and non-graphic grief
checks. Windows COS Desktop was used for actual Unity Editor operations
and the read-only COS Core PrintWindow capture; Unity MCP was never
used. VALORANT was checked absent.

### Panel 02 v06: live Game View and moving Timeline ruler together

Source remains the original `A02_BrokenLanguageWar_BackTurnThunder_v05.playable`
at 8.5 seconds. The 0.50 m lateral camera reveal, narration activation,
thunder light and matching storm sky tracks all remain intact. During
actual Unity Play Mode, all four Timeline tracks and the changing
Game View were visible in the **same video frames**, including three
light/sky pulses. The vertical white playhead was measured against the
visible one-second Timeline ruler (about 105 pixels per second).

| Authored thunder/sky peak | Observed live playhead | Difference | Game View sky brightness increase | Ground increase |
| --- | --- | --- | --- | --- |
| 2.05 s | 2.048 s | -0.002 s | +13.51 | +16.73 |
| 5.12 s | 5.152 s | +0.032 s | +7.50 | +9.88 |
| 7.20 s | 7.210 s | +0.010 s | +5.00 | +7.01 |

These values are derived from 10-FPS Unity window footage and ruler
coordinates, not from guessed media start offsets. Their conservative
on-screen sampling tolerance is **plus/minus 0.11 seconds**, rather
than millisecond-accurate engine logs. All three match the original
authored peaks within this tolerance. Changing ruined-city geometry,
Van's scale and the correct cyan-crystal staff remained coherent during
the 8.5-second camera move. No camera or timing correction is required.

New primary footage:
`ArtSource/Cinematics/StaffContinuityReview_20261027/UnityEditorValidation/P02_v06_TimelineVisible_PlayMode_v04_raw38s.mp4`.

Real Unity Game View and ruler before/at each of the three peaks:
`ArtSource/Cinematics/StaffContinuityReview_20261027/UnityEditorValidation/P02_v06_CalibratedPlayhead_ThunderSequence_v05.png`.

### Panel 03 v04: exact PlayableDirector time and Unity Step evaluation

Source remains the original `A03_UltimateRelease_ThunderFireTiles_v03.playable`,
duration 7.6 seconds. The five original tracks remain: 0.32 m low-rear
camera push, thunder light, storm sky, seven A-G word-magic rotating
tiles and 18 fire sites (54 animated fire-scale curves).

Real Unity Play Mode was **paused** and the original Timeline frame
field set to the precise source key times, then the Editor's
**Step** button was used to evaluate the rendered Game View. All
five Timeline tracks, the keyed playhead position and actual scene
were captured together.

| Unity evaluated baseline | Exact stepped thunder peak | Full Game View brightness increase | Left ruins increase | Ground increase |
| --- | --- | --- | --- | --- |
| 1.70 s (51 frames) | 2.05 s (61.5 frames) | +18.69 | +30.91 | +18.95 |
| 4.80 s (144 frames) | 5.12 s (153.6 frames) | +15.80 | +25.94 | +15.38 |
| 6.90 s (207 frames) | 7.20 s (216 frames) | +9.76 | +16.24 | +9.11 |

These are **actual evaluated Unity frames at identifiable playhead
positions**, not calculated render simulations. Each storm/light
pulse brightens sky, ruined buildings and the ground together, in the
original first-strongest, third-softest order. Additional already
verified continuous Play Mode recording confirms the slow camera
push, tile rotations, independent flames and drifting embers.

Original source approval in
`docs/STORY_PANEL_ASSET_MANIFEST.md` identifies Panel 03 as an
illustrated still battle/word-magic composition. Its Timeline explicitly
labels its camera track as **Static Ultimate Release Frame**. Van's
held casting pose and canonical staff **match that source approval**.
The track list does not contain a separate humanoid attack-release
animation, a projectile launch, impact event or gameplay spell action,
nor was any such new requirement authorized. **No extra release
animation was added or assumed.**

New primary live playback footage:
`ArtSource/Cinematics/StaffContinuityReview_20261027/UnityEditorValidation/P03_v04_TimelineVisible_PlayFromZero_v05_raw28s.mp4`.

Direct before/stepped-peak composite (six source screenshots preserved
individually in the same folder):
`ArtSource/Cinematics/StaffContinuityReview_20261027/UnityEditorValidation/P03_v04_CalibratedPlayhead_ThunderSequence_v05.png`.

### v04 conclusions, preservation and limitations

- P02: **LIVE TIMELINE/GAME PLAYBACK PASS** at all three sampled thunder
  peaks (ruler/video tolerance +/-0.11 s), continuous camera reveal
  verified; original 8.5-second sequence remains unchanged.
- P03: **EXACT PLAYHEAD-SAMPLED GAME EVALUATION PASS** for all three
  authored thunder/sky peaks and original held release composition.
  Camera/tiles/fire/embers continuous visual playback was already
  confirmed and retained; original 7.6-second sequence unchanged.
- P04 v08 four-cut non-graphic grief cinematic: previously accepted
  and **not reworked**; Panels 01/05/06, including P05 v10, P06 v05
  and its approved 10-second Blender preview: **preserved**.
- Canonical Van Aert staff remains
  `Assets/VAN AERT/STAFF/base_basic_shaded.glb`, as traced to the
  scene-used VanAertPlayer.prefab. All replacement assets and source
  textures were preserved; **no scene or Timeline corrective edits**.
- **Limitations:** P02 moving-playhead measurements are sampled video
  frames, not millisecond-accurate logs. P03 exact keyframe samples
  use paused Editor Step, not a 60-FPS continuous hardware-clock
  record; previously recorded continuous playback supplies the motion
  evidence. Neither an unapproved projectile-release event, external
  audio waveform synchronization nor Android device performance
  has been claimed or tested.
- Restored main Unity Editor `Stage3Scene` visually with its original
  gameplay desert view. The on-disk source at
  `Assets/Scenes/Stage3Scene/Stage3Scene.unity` retained its original
  October 21 last-write timestamp. No Stage3 work was saved over,
  discarded or modified. No original P02/P03/P04 cinematic scene,
  asset, gameplay, collider, mobile setting or renderer setting was
  edited during this pass.

**Newest QA report (v04):**
`Library/Panel06Audit/Unity_StaffContinuity_FinalQA_20261027_v04.json`.
The earlier v01-v03 reports, approved Blender/Unity sources, older
videos and full comparison captures remain intact. Read-only
reproducible verification script:
`Library/Panel06Audit/finalize_calibrated_timeline_qa_v04.py`.
