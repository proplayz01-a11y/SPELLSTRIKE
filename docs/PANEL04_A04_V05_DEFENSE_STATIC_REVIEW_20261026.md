# SPELLSTRIKE Panel 04: The Destroyed Home

Date: October 26, 2026
Recommended isolated Unity scene: Assets/Scenes/Cinematics/Prologue_P04_A04_GriefStatic_v05.unity
Status: Defense-presentable **static four-shot 3D cinematic prototype**. Not a fully animated character sequence.

## Finished

- Nevali's mother has a 71-bone editable Blender death-pose source at ArtSource/Characters/NevaliMother/Working/NevaliMother_Panel04_DeathPose_v15.blend.
- Younger Van Aert has a 71-bone editable Blender kneeling/reaching pose source at ArtSource/Characters/YoungVanAert/Working/YoungVanAert_Panel04_KneelSupport_v07.blend.
- Both are staged in the destroyed family home with debris, toy-block clues, Nevali's blue hair bow, and a staff on the floor.
- Final Blender working scene: ArtSource/LibraryProps/DefenseSets/Prologue_Home_04_Ruined_v14_BowVisibleReview.blend. Confirmed clean/saved on October 26.
- Four Blender import-back validated static GLBs are located in Assets/Cinematics/Storyboard3D/Characters/Panel04/:
  - Panel04_NevaliMother_DeathPose_Static_v01.glb
  - Panel04_YoungVan_Kneeling_Static_v01.glb
  - Panel04_BlueRibbon_Staff_Static_v01.glb
  - Panel04_GriefScene_StaticBlockout_v01.glb
- The Unity v05 scene imported **707 renderers**.
- The native 10.0-second, 24 FPS Unity Timeline has two tracks: the four camera cuts and narration caption activation. Timeline: Assets/Cinematics/Storyboard3D/Timelines/Panel04_A04_v05/P04_A04_Grief_ReturnHome_v05.playable.
- Panel04A04ShotLockV05.cs provides a Panel-04-only per-shot camera rotation fix. It addresses the independently measured 5°–32° quaternion animation drift in Unity v04.
- Only the scene instance of HOUSE_LEFT_WALL was disabled to avoid blocking the discovery angle. The Blender source, GLBs and approved Panels 01–03 were not modified by v05.

## Narrative cuts

| Time | Beat |
|---|---|
| 0.000–2.667 seconds | Ruined-home discovery: Van crouched beside his wife |
| 2.667–6.042 seconds | Reverse view: Van grieves and reaches toward her |
| 6.042–8.458 seconds | Mother's closed-eye expression and loss close-up |
| 8.458–10.000 seconds | Blue hair bow on the rug reveals Nevali's absence |

The narration is on-screen **text**, not voiced audio. The characters do not change pose between cuts; the camera changes.

## Measured checks

- Unity v05 batch build: **PASS**, Library/Panel04Audit/P04_A04_UnityBatchBuild_v05.log.
- Unity Editor frame QA: **PASS**, Library/Panel04Audit/P04_A04_UnityPreviewQA_v05.json.
- Four real Unity GPU renders captured at Timeline seconds 0.65, 3.20, 6.55, 8.95: camera positions exactly matched expected coordinates and rotation error was below 0.1° at all sampled cuts.
- The four captures are saved in ArtSource/Cinematics/Panel04_v04/Reviews/Unity_v05/.
- Contact sheet: ArtSource/Cinematics/Panel04_v05/Reviews/P04_A04_v05_UnityFourShot_ContactSheet.png.
- 10-second, 24 FPS review montage: ArtSource/Cinematics/Panel04_v05/Reviews/P04_A04_v05_UnityFourShot_StaticPreview_10s.mp4. Verified 240 decodable frames, 960 × 540. Montage uses captured **stills**, and is not a runtime Unity recording; no audio.
- Blender scene was verified clean with both 71-bone armatures present.

## Limits and next-stage polishing

1. The Unity character GLBs are baked **static** meshes, not verified Unity humanoid animated rigs. Editable Blender rigged sources exist separately.
2. Van's grief kneel and supportive reach read visually, but the arms do not animate to lift or embrace his wife. Physical contact remains approximate.
3. The gown is a defense-quality static floor-draped version, not simulated fabric; some Rodin texture artifacts are visible near the hem.
4. Actual Unity Play Mode playback, Android performance and voice audio were not verified in this batch. Camera.Render preview is not the same as runtime playback.
5. Present as **defense prototype / visual blockout to be polished**, not final animation or cloth physics.

## Review instructions

Open Assets/Scenes/Cinematics/Prologue_P04_A04_GriefStatic_v05.unity in Unity 6.3 LTS and select the P04_A04_TIMELINE__DESTROYED_HOME__10S GameObject. The PlayableDirector uses the v05 timeline.

Do not overwrite earlier approved Panels 01–03, mother/Van rigged sources, original Rodin models, or older Panel 04 versions.
