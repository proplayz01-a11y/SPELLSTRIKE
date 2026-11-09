# SPELLSTRIKE Panels 11–14 Unity Production, October 27, 2026

## Owner directive and narrative boundaries

- User asked to accelerate beyond Panels 07–10, use simple character animations and dialogue text without voice, and ultimately consolidate approved prologue into a continuous sequence.
- Authoritative story references: `docs/STORY_PANEL_ASSET_MANIFEST.md`, plus `Assets/Story/Approved/Prologue_Panel_11_SOURCE_APPROVED.png` through `Prologue_Panel_14_SOURCE_APPROVED.png`.
- Panel 11: Nevali's warm vision is treated as **part of Older Van's recalled dream**, NOT evidence of a physical resurrection/return. No new VFX graph, boss, dream-world mechanics or new story event was added.
- Approved Panel 12: solo contemplative reaction Vander, gateway/background continuity. Panel 13: tighter Vander continuation. Panel 14: two-wizard table conversation; older Van listens, Vander responds, restrained open-palm pose remains an art-direction review gap.
- Previous approved Panels 01–10 and their asset versions, Blender GUI work, prior MP4s and canonical staff were not overwritten.

## Panel 11: actual generated assets

- `Assets/Scenes/Cinematics/Prologue_P11_NevaliDream_v03.unity` **exists, saved, nonempty**.
- `Assets/Cinematics/Storyboard3D/Timelines/Panel11_v03/P11_NevaliDream_8s_v03.playable` **exists, 8.0-second Timeline with 2 AnimationTracks**.
- `Assets/Cinematics/Storyboard3D/Timelines/Panel11_v03/P11_Nevali_v13_SubtleHeadBob_v03.anim` **exists**: 570 curves, rest-pose for non-head transforms and 4 subtly moving head-quaternion curves.
- Scene reuses P10 canonical library, seated Older Van's previously verified 8s head clip, Nevali v13 rigged import (`Assets/Cinematics/Storyboard3D/Characters/Panel01_v03/Nevali_v13_MediaPipe_BlockPlay_v04.fbx`) with adjusted import scale, four existing textured Nevali materials, static camera, scene-local warm fill light, and a Unity UI text-only provisional line `Van Aert: Nevali...?` (no synthesized voice).
- Successful Unity serialization evidence `Library/Panel07_10MotionAudit/Panel11_BuilderV03.txt` (active Stage3Scene unchanged). Prior incomplete v01/v02 scene-only copies remain historical, never promote them as playable.
- Model/scale preflight `Library/Panel07_10MotionAudit/Panel11_PreflightV01.txt`. Untinted/unposed rough Unity scout `Library/Panel07_10MotionAudit/Panel11PoseScout_v01/P11_Nevali_s016_pose0.png`; this **is not** the final staged scene or visual approval. Textured v02 scout and isolated v03 Unity playback screenshot attempt did not finish during severe RAM pressure. Do not report real P11 image/playback PASS until rerun with enough memory and screenshots.
- P11 art acceptance gates: Nevali front-facing with correct braid/dress/blue ribbons and non-white materials; Older Van facing/reaching toward her; restrained dream warmth; no undisclosed plot. Provisional subtitle requires user approval.

## Panels 12–14: actual generated assets

| Panel | New versioned Unity scene | New Timeline | Checked in Unity |
|---|---|---|---|
| 12 | `Assets/Scenes/Cinematics/Prologue_P12_VanderReflects_v01.unity` | `Assets/Cinematics/Storyboard3D/Timelines/Panels12_14_v01/P12_VanderReflects_8s_v01.playable` | Saved, director duration 8s, solo Vander animator bound |
| 13 | `Assets/Scenes/Cinematics/Prologue_P13_VanderContemplatesClose_v01.unity` | `Assets/Cinematics/Storyboard3D/Timelines/Panels12_14_v01/P13_VanderContemplatesClose_8s_v01.playable` | Saved, director duration 8s, solo Vander animator bound |
| 14 | `Assets/Scenes/Cinematics/Prologue_P14_VanderReplies_v01.unity` | `Assets/Cinematics/Storyboard3D/Timelines/Panels12_14_v01/P14_VanderReplies_8s_v01.playable` | Saved, director duration 8s, Older Van and Vander animator bindings |

- All reuse existing canonical library, existing P10 rig meshes and v06 head-bob animation clips. P12/P13 intentionally remove Older Van from a **versioned scene copy only**, preserving the P10 base.
- P14 includes provisional Unity text subtitle: `Vander Poel: We must understand what the dream means.` No voice recording, no lip sync, no promise of final script approval.
- Unity build report: `Library/Panel07_10MotionAudit/Panels12_14_BuilderV01.txt`; verifies `SCENE_LIST_UNCHANGED=True` and `Stage3Scene.unity dirty=False` before/after.
- Isolated Unity frame/realtime verification for P12–14 attempted by `Assets/Editor/Cinematics/Panels12_14TimelineReviewV01.cs`; did not finish while Unity Editor reloaded domains / machine under memory pressure. Do not mark Game View or preview playback PASS from source alone.
- P12/13 camera angles and DOF, and P14's **approved open-palm gesture**, still need visual approval / material-specific correction. Their saved scene/Timeline assets are build outputs, not user-approved finals.

## Consolidation status, next steps

- Machine-readable authoritative ordered scene list is `docs/PROLOGUE_SCENE_SEQUENCE_INDEX_v01.json`. **This is only an index, not a master playable Timeline.** It includes existing P01–P04 scene paths, v07 P07–P10, v03 P11, v01 P12–14, and missing gates.
- `Assets/Scenes/Cinematics` and all `Assets/Scenes/**/*.unity` searched: **no separately produced P05 or P06 Unity cinematic scenes found**, despite approved storyboard sources. Do not skip beats or falsely claim a complete 01–14 playable story.
- Panel 15 has approved transition direction (Vander rises from chair, turns toward same gateway), and an archival P15–18 blocking Timeline asset, but **no verified production scene or rising animation**. P15 cannot be finished through head-bobbing alone; author the actual stand/rise motion before approval.
- Runtime/Editor continuous sequence, story subtitles for all panels, P11–14 isolated visual/playback checks, P05/P06 scene recovery/build, and Panel15 stand-up remain open. Do not change Build Settings as a shortcut.

## Computer safeguards and constraints

- VALORANT not running during preflights. Existing Unity 6.3 GUI on Stage3Scene and Blender GUI on Panel04 preserved. No Unity MCP or second Unity GUI was launched.
- `Stage3Scene.unity` clean and active after all executed additive builds. Existing Edge previews and earlier renders untouched.
- Unity Editor later displayed `Reloading Domain (busy for 44s)` and Windows free physical memory dipped below 500MB during high-resolution isolated Nevali/Timeline previews. Heavy auto-run QA classes had their `[InitializeOnLoad]` attributes disabled after these attempts. Avoid further 1280x720 multi-frame previews until available RAM and Editor responsiveness are sufficient. Manual-only menu methods remain for targeted retest.
- Unity AI service TLS mismatch still deferred separately to `docs/SPELLSTRIKE_DEFERRED_ISSUES.md`; never click Yes to bypass certificate checks.

## Manual isolated acceptance procedure

1. Verify VALORANT exited, Stage3Scene saved/clean, and RAM healthy. Do not close unrelated Blender work.
2. Open versioned Panel11 scene **after safely preserving current scene**, inspect P11 actor materials, pose, and Game View at start/middle/end of 8s, subtitle visibility; record time-labelled screenshots.
3. Inspect P12 and P13 Vander-only gateway framing, and P14 2-person dialogue framing/open palm without altering prior versions. Fix demonstrated issues as new versions only.
4. Recover/build Panels05–06 and Panel15 physical standing motion before composing a true 01–15 master sequence. Use the JSON index to assemble clips without skipping story beats.

## Approved storyboard comparison sheet
- Created and opened via CMD (Windows default image viewer): `ArtSource/Cinematics/Panels11_14_Review_20261027/Panels11_14_ApprovedStoryboard_ContactSheet_v01.png`. This 2x2 contact sheet displays **approved source still images**, NOT screenshots of the newly generated Unity cinematics. Preserves all four original source PNGs unchanged.
