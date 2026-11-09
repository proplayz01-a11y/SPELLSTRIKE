# SPELLSTRIKE Prologue Panel 01 — Unity defense rough-cut validation

Date: 2026-10-25 (Asia/Manila)
Shot: A01, **Peace Before the Great War**
Locked narration: “Once upon a time, a wizard named Van Aert lived peacefully with his daughter, Nevali...”

## Scenes and how to preview

- **Animated defense rough-cut (recommended):** `Assets/Scenes/Cinematics/Prologue_P01_FamilyHome_v03_AnimatedRoughCut.unity` — open this in Unity 6.3 LTS and press **Play**. Young Van is posed with folded arms. Nevali uses the imported rough 8-second body/hand animation. The camera runs an approximately 10.5-second, 0.27-meter cinematic push, then holds.
- **Static fallback (saved, visually verified):** `Assets/Scenes/Cinematics/Prologue_P01_FamilyHome_v02.unity` — v13 Nevali and v06 Young Van are both deliberately held in static cinematic poses. Its latest -27-degree father rotation **was saved**. The editor assembler `Assets/Editor/Cinematics/PrologueP01V02SceneAssembler.cs` now matches that adjustment and refuses to overwrite an existing v02 scene.
- **Working test copy:** `Assets/Scenes/Cinematics/Prologue_P01_FamilyHome_v03_AnimatedTest.unity`. Preserve as a test variant. Original `Prologue_P01_FamilyHome_Defense_v01.unity`, gameplay, StoryScene and Build Settings were not edited by this Panel 01 finishing pass.

## Actors and environment

- House: Codex Blender `ArtSource/LibraryProps/DefenseSets/Prologue_Home_01_Peaceful_v02.blend` exported to Unity-compatible fixed-PBR-color `Assets/Cinematics/Storyboard3D/Environment/DefenseSets/Prologue_Home_01_Peaceful_v02_UnityPBR.glb` (74 renderers; original v01 preserved).
- Young Van Aert: approved ActionForge/Rodin rig/source, with `Assets/Cinematics/Storyboard3D/Characters/Panel01_v02/YoungVanAert_ArmsCrossed_StaticPose_v06.glb` at Unity `(1.95, 0, 2.75)`, world yaw -27 degrees applied after +90 X-axis import correction. The separate 75-frame `YoungVanAert_CrossArms_v06.fbx` source is retained; its animation is **not** played in these scenes.
- Static Nevali: `Assets/Cinematics/Storyboard3D/Characters/Panel01_v02/Nevali_Playing_StaticPose_v13.glb`, Unity `(-1.28, 0.045, 0.45)`, +90 X-axis correction.
- Animated Nevali: `Assets/Cinematics/Storyboard3D/Characters/Panel01_v03/Nevali_v13_MediaPipe_BlockPlay_v04.fbx`, configured as **Generic** animation, not validated as Humanoid retargeting. Its `Scene` clip is about 7.967 seconds, with 570 animation curves over 57 bone paths, and is assigned to `Panel01_Nevali_BlockPlay_v04.controller`. Runtime-tested on `NEVALI_V13__MEDIAPIPE_V04_ANIMATED` at Unity `(-1.28, 0.45, 0.45)` with root scale `(0.00019, 0.00019, 0.00019)`. That compensates for the FBX importer normalization and is intentionally small in the hierarchy. Four new URP materials reference the four original Nevali-v13 GLB texture subassets. The static Nevali fallback exists disabled in v03.
- Room is compact, warmly lit, and the view between father and daughter is open; a sizeable central timber frame is still visible, but it is not a solid dividing wall. A future composition pass may improve its silhouette without altering the Codex source.

## Verified evidence

1. Unity v02 Editor scene saved after the father turned -27 degrees; camera screenshot showed warm room, upright actors and readable props.
2. Unity v02 Play Mode ran. Live measurement confirmed `Panel01CinematicPlayback` reached `elapsed=10.50898` seconds and moved the camera exactly `0.270` meters from its start position before holding.
3. `Panel01_v02_FinalNarration_ActualScreenCapture.png` was captured with Unity's actual `ScreenCapture.CaptureScreenshot` while playing, not a camera-only render. The full, locked upper-left narration is readable on the translucent dark rectangle with gold accent and neither face is obscured.
4. Unity v03 first revealed unusable FBX auto-import scale and missing texture bindings; both were fixed in the **v03 instance and versioned v03 materials**, without modifying the v02 scene or source FBX. Final Play Mode screenshot confirms Nevali is normally sized, textured, seated and aligned with the toy arrangement.
5. Unity v03 Play Mode Animator was enabled/initialized, using state `Scene`. Motion sampling between normalized timeline values 0.02 and 0.62 measured approximately **44.4 degrees** of world-space right forearm rotation and **0.140 meters** of right-hand world-position change. Imported animation has 570 curve bindings across 57 rig paths. These are actual runtime measurements, not merely a claim that a clip exists.
6. Both latest final screenshots include the complete narration and color-correct family home. Unity console had an unrelated `Unity Remote requirements check failed` error; no Panel 01-specific script exception was reported in the targeted checks.
7. **Unity video captured and decoded successfully:** real Unity Game View recorded into 103 sequential screenshot frames over 10.62 seconds, then encoded into a silent **1920×1080 MP4** at approximately **9.7 FPS** using OpenCV `mp4v`. Independent frame readback checked the beginning, middle and end; frames 1 and 50 and frames 50 and 100 differ visibly. This is a watchable **low-framerate defense proof**, not a polished 30 FPS film export.

Saved visual evidence:

- Original full Game View: `ArtSource/Cinematics/Panel01_v03/Reviews/Panel01_Unity_v03_FullGameView_2400x1080.png`
- 16:9 defense frame crop: `ArtSource/Cinematics/Panel01_v03/Reviews/Panel01_Unity_v03_DefenseFrame_1920x1080.png`
- **Unity animated defense video:** `ArtSource/Cinematics/Panel01_v03/Reviews/Panel01_Unity_v03_AnimatedDefense_10s.mp4` (10.62 s, 1920×1080, ~9.7 FPS, silent, 103 frames).
- Encoded-video review montage: `ArtSource/Cinematics/Panel01_v03/Reviews/Panel01_Unity_v03_VideoFrameQA_ContactSheet.png`.
- Additional v02 and v03 screenshot/Play Mode evidence: `Library/P01UnityIntegrationQA/` (Unity Library is a temporary/non-source-controlled location).
- Earlier **Blender-only**, 8-second H.264 animation proof: `ArtSource/Characters/Nevali/Mocap/Panel01_RoughCut_v03/SPELLSTRIKE_P01_FamilyHome_NevaliV13_RoughCut_8sec_v03.mp4`. This separate video includes a temporary Young Van; it is **not** an export of the final Unity shot.

## Scope boundary / known roughness

- Nevali's **body and arm movement plays** for about 8 seconds, but **the wooden blocks remain stationary** in the Unity scene. Actual hand/block contact, reach/grab/place events, and synchronized toy animation have **not** been implemented or verified. Do not present the scene as a fully completed interaction.
- Young Van's folded-arm pose is fixed during runtime, and his visible palms/hand placement and gaze still need polish. A rigged 75-frame FBX exists for potential future playback but is not used as an animated character in this shot.
- No recorded voiceover/audio asset is assigned. Narration is an on-screen text overlay only.
- Blender procedural wood-grain material details are simplified to portable URP/PBR base colors, not a pixel-identical Blender render.
- A low-framerate Unity MP4 prototype **is** available; no polished 30 FPS/full-quality Unity Recorder export, sequence transition, 42-panel integration, Android device validation, or release/build-setting change has been performed in this pass. The isolated Unity scene itself is playable/viewable in the Editor.
- No automatic 10-minute background checker or cron was created; the Codex home improvement had already finished before this pass resumed.

## Future polish, if requested

Align toy-object motion and contact with the Nevali rig, improve father hand pose/gaze, attach voiceover, and record a full-framerate Unity cinematic video or Timeline if required for presentation. Retain versioned scene copies and do not overwrite the original gameplay or story scenes.
