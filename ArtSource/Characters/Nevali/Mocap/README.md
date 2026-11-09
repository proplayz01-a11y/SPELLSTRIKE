# Nevali human-motion to Mixamo LLM Mocap

October 23, 2026 latest status: **Nevali v13 is Mixamo-skinned; a separately
versioned, real video-driven MediaPipe rough animation is now exported and
round-trip validated. Full GVHMR/SMPL-X inference and production retarget
are STILL PENDING.** Do not confuse the lightweight rough cut with GVHMR.

Current best rough output, not yet approved for production:

- `RiggedReturn_v13/Nevali_v13_MediaPipe_BlockPlay_Rough_v04.blend`
- `RiggedReturn_v13/Nevali_v13_MediaPipe_BlockPlay_Rough_v04.fbx`
- `RiggedReturn_v13/Nevali_v13_RoughMotion_FBXRoundtripQA_v04.json`
- `RiggedReturn_v13/Nevali_v13_RoughMotion_VisualQA_v03/` (same
  animation content; v04 corrects embedded FBX textures only).

**Panel 01 viewable Blender rough-cut is now available:**

- `Panel01_RoughCut_v03/SPELLSTRIKE_P01_FamilyHome_NevaliV13_RoughCut_v03.blend`
- `Panel01_RoughCut_v03/SPELLSTRIKE_P01_FamilyHome_NevaliV13_RoughCut_8sec_v03.mp4`
- `Panel01_RoughCut_v03/FrameQA_v03/` (four direct in-scene 16:9 frames)
- `RiggedReturn_v13/Nevali_v13_AnimatedFBX_VisualQA_v04/` (four separate animated-FBX reimport frames)

The scene uses real Blender 3D Family Home geometry, the current rigged v13
Nevali, manually animated block props, and a TEMPORARY Young Van prototype.
The exact 240/240 MP4 frames decode at 30 FPS, 960x540; duration 8.000s,
H.264. Blender renders and visible Windows Media Player playback verified.
No narration, contact IK, final Young Van look-at or Unity Timeline/Play Mode
were completed. The original `Prologue_Home_01_Peaceful_v01.blend`, Unity
P01 scene and character source were not edited.

The CPU-only MediaPipe model successfully extracted all 192 source frames
at 24 FPS as world/2D landmarks. Key arms and head tracked well; the
legs and feet were obscured, so the rough Mixamo FK retarget keeps
manually authored seated leg positions. It is resampled at 30 FPS
for 240 frames (8 seconds) with no shortening. v01-v03 were retained
for review; v03 reduces skirt deformation and boot floor penetration.
The v04 Blender FBX import check PASSED for 57 bones, 74,083/74,083
skinned vertices, 118,962 triangles, one full animated action, and
all four color images embedded/loaded. See the v04 QA JSON.

Still NOT tested: Unity import/Animator playback, final Panel 01 hand
to toy-block contact, high-quality seated skirt deformation, expressive
face animation, full-body GVHMR/SMPL-X fidelity, physical Android QA.
Do not silently replace the existing Unity stand-ins or original v13.

Local source copied byte-identically from Downloads to:

C:\Users\David\Documents\Codex\MocapLab\mixamo-llm-mocap\plates\nevali_blockstack_flow_20261023\nevali_blockstack_source.mp4

ffprobe confirms H.264, 1280x720, 24 FPS, 192 frames, eight seconds.

Copy Nevali_BlockStack_v01.json to the installed repository action_specs directory for the eventual retarget. Use Run-Nevali-PoseExtraction.ps1 from PowerShell with ExecutionPolicy Bypass at process scope only, when ample GPU and RAM are free. It refuses to start during VALORANT and refuses to run below 6500 MiB free RAM or 4800 MiB free GPU VRAM. Guard is only a precaution; GVHMR authors recommend 8 GB GPU while user RTX 4050 has 6 GB. Do not close unsaved user apps for memory.

2026-10-23 retest: initial memory 3357 MiB free RAM and 2442 MiB free VRAM. The original 2500/4400 thresholds happened to pass transiently when free GPU VRAM rose, allowing GVHMR to launch. System free RAM dropped to ~467 MiB. The extraction was immediately interrupted with Ctrl+C; both estimator Python processes ended, and ~5256 MiB free RAM / ~5920 MiB free VRAM were verified after stopping. No smplx.npz, landmarks.json or motion clip was produced. New higher preflight thresholds prevent this specific under-provisioned launch. Do not claim motion extraction success.

Target production pipeline: local GVHMR SMPL-X from video, independent pose and motion QA, then retarget to known Mixamo v13 rest rig and correct child seated pose/finger-to-block contact in Blender. The actual Mixamo-rigged v13 FBX is now under RiggedReturn_v13; the separately validated Nevali-specific profile is Nevali_v13_Mixamo_RigProfile_v01.json. Do NOT use the installed Y Bot rig_profile.json for Nevali. The earlier 18-bone Nevali stand-in is not a standard Mixamo rig.

The plate performer sits cross-legged, not kneeling. Foot contact and toy block prop should be corrected manually after extraction. Production licensing of upstream SMPL-X and other model assets needs review.
