# Sunbound Warden — Recovery Crouch → Crouch Idle

Created in Blender 5.0.1, 2026-09-26. This task merges the two recovery sources only. The supplied slash was inspected and retained as a separate source/reference action; no slash is baked into the recovery sequence and no proximity-triggered attack behavior is implemented.

## New deliverables

- NLA authoring: `C:/Users/David/PROTOTYPE/SPELLSTRIKE/SourceAssets/Stage3/Recovery_CrouchIdle_NLA/SunboundWarden_Recovery_CrouchIdle_NLA.blend`
- Baked action: `C:/Users/David/PROTOTYPE/SPELLSTRIKE/SourceAssets/Stage3/Recovery_CrouchIdle_NLA/SunboundWarden_Recovery_CrouchIdle_Baked.blend`
- Unity export: `C:/Users/David/PROTOTYPE/SPELLSTRIKE/Assets/Stage 3/Enemies/STAGE 3 ENEMY NO. 1/Animations/SunboundWarden_Recovery_CrouchIdle.fbx`
- Action/take name: `SunboundWarden_Recovery_CrouchIdle`.
- Preview: `Recovery_CrouchIdle_Preview.gif` in this folder. Preview repetition is for viewing; the merged take is not a seamless loop.
- Full-frame visual evidence: `Final_Sheet_1.jpg` through `Final_Sheet_4.jpg`, and `Side_Sheet_1.jpg` through `Side_Sheet_4.jpg`.

## Sources and exact sequence

All sources are 30 FPS. Source frame numbering below uses Blender FBX import `anim_offset=0`; default Blender import adds 1 to these source frame labels.

| Source | Full source range | New timeline | Blend-in |
|---|---|---|---|
| `C:/Users/David/Downloads/STAGE 3 ENEMY NO 1 FINAL@Sword And Shield Crouch.fbx` | 0–21 | 1–22 | 0 |
| `C:/Users/David/Downloads/STAGE 3 ENEMY NO 1 FINAL@Sword And Shield Crouch Idle.fbx` | 0–73 | 18–91 | 4 frames |

Both complete source ranges are retained at native speed. NLA REPLACE mode, HOLD extrapolation, strip scale 1, repeat 1, auto-blend off, blend-out 0. The second track fades in across output 18–22 (0.133333 seconds). Nothing is trimmed, shortened or repeated within either strip.

Final range: **1–91 at 30 FPS; 91 integer poses, 90 intervals = 3.000 seconds**. The result begins in standing guard, lowers into crouched guard, then remains in crouch idle. It contains no stand-up exit.

The source actions imported into the existing rig scene as `Armature.001|mixamo.com|Layer0` (the .001 is Blender's object-name collision suffix). Raw imported actions are retained as `SOURCE_UNMODIFIED_Crouch` and `SOURCE_UNMODIFIED_CrouchIdle`; separate `ADAPTED_Crouch_Canonical_InPlace` and `ADAPTED_CrouchIdle_Canonical_InPlace` actions drive the NLA. External FBXs are unchanged.

Inspected separately: `C:/Users/David/Downloads/STAGE 3 ENEMY NO 1 FINAL@Sword And Shield Slash (1).fbx`, full source 0–41. It starts and ends in a crouched guard, with wind-up, slash and recovery between them. Its raw and adapted actions remain available in the source .blend as reference only, excluded from the final take/export.

## Rig and root handling

The unchanged canonical rig/reference mesh comes from `C:/Users/David/PROTOTYPE/SPELLSTRIKE/SourceAssets/Stage3/SunboundWarden_Rig_HELMET_FIXED.blend`.

- All 57 bone names, parents, rest matrices and lengths are retained. Source rotations are transferred into separate actions on that canonical skeleton because source bind poses differ.
- Armature stays at the origin, scale 0.01, X rotation 90 degrees. Horizontal hip displacement is neutralized; vertical crouch compression and idle motion are retained.
- No animated root travel, target turning, helper bones or constraints are exported.
- Quaternion keys maintain hemisphere continuity. No source root anomaly required a pose repair in these two clips.
- One baked take, quarter-frame sampling, no curve simplification, -Z forward / Y up. The unchanged reference mesh is included for bind-pose compatibility; preview cameras/grid and slash reference are excluded.

## Validation

- Inspected all source frames before merging: 22 crouch, 74 crouch-idle, 42 slash-reference poses.
- Inspected the complete 91-frame merged result from front-oblique and side views in Blender-rendered frames, including both sides of the 18–22 join, idle breathing and final crouched pose. No abrupt pose pop or root flip was observed.
- Compared the exported/reimported FBX against the NLA at **361 quarter-frame samples**. Maximum world bone-position difference: approximately **0.00000714 m** (0.00714 mm). No angular discrepancy measurable with the float comparison used.
- All 57 names and parents match after FBX roundtrip; maximum rest-matrix difference approximately 0.0000458 in armature units.
- Maximum hip angular step across quarter-frame samples: approximately 0.345 degrees. Horizontal hip range is below 0.000000067 m (numerical noise).
- Pre-existing files were SHA-256 checked before and after. See `preservation_manifest.json`, `export_validation.json`, and `delivery_validation.json` for exact counts and evidence.
- Validation used Blender Python evaluation and offscreen renders, not a live interactive Blender UI session. No Unity import/Play Mode/device or actual attached-weapon validation was performed.

## Unity setup / remaining behavior

The new FBX and its fresh .meta are in the Warden Animations folder. Import settings are authored for Humanoid, Create From This Model, Loop Time OFF, no compression, source curves retained, root transforms baked into pose, and no Animation Events. Unity import acceptance remains unverified.

1. Preview this new take on the existing Warden with the actual sword/grip setup. Verify a valid Avatar, correct scale/facing and foot contact. Keep Apply Root Motion OFF.
2. Assign the clip to a recovery state only when integrating recovery behavior. No Animator state, transition, scene, prefab or script was edited here.
3. Do not enable looping on the entire merged clip: that would restart the standing-to-crouch entry every three seconds. An indefinite recovery hold needs a separately looping crouch-idle state/take in the later wiring pass.
4. The requested approach-triggered slash requires a separate transition/behavior from recovery to the slash and back, with range/cooldown and recovery-timer semantics reviewed in that integration task. The animation merge alone does not make the Warden attack approaching players.
5. No existing Animation Events were altered or copied. No damage timing or proximity behavior is claimed as implemented.

No original animation/source asset was replaced. Runtime C# scripts and Inspector references are unchanged. Supporting authoring/verification scripts and evidence are in this folder; the changelog records the standalone asset delivery.
