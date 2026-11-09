# Radiant Rush ShortHeavy — standalone animation variant

Created in Blender 5.0.1 on 2026-09-26. Animation authoring and FBX roundtrip are validated; Unity import, attached-weapon playback, Animator integration, Play Mode, and device testing remain unverified. No existing Warden animation or Animation Event was changed.

## Deliverables

Project root: `C:/Users/David/PROTOTYPE/SPELLSTRIKE`.

- Editable three-track NLA source: `C:/Users/David/PROTOTYPE/SPELLSTRIKE/SourceAssets/Stage3/RadiantRush_ShortHeavy/SunboundWarden_RadiantRush_ShortHeavy_NLA.blend`
- Baked action source: `C:/Users/David/PROTOTYPE/SPELLSTRIKE/SourceAssets/Stage3/RadiantRush_ShortHeavy/SunboundWarden_RadiantRush_ShortHeavy_Baked.blend`
- Unity export: `C:/Users/David/PROTOTYPE/SPELLSTRIKE/Assets/Stage 3/Enemies/STAGE 3 ENEMY NO. 1/Animations/SunboundWarden_RadiantRush_ShortHeavy.fbx`
- New action/take: `SunboundWarden_RadiantRush_ShortHeavy`.
- Preview: `RadiantRush_ShortHeavy_Preview.gif` in this folder. GIF repeats for viewing only; the animation is a one-shot.
- Complete two-view frame sheets: `Final_Sheet_1.jpg` through `Final_Sheet_3.jpg`, and `Side_Sheet_1.jpg` through `Side_Sheet_3.jpg`.
- Fractional-frame join/root evidence: `Fractional_Sheet.jpg`.

The FBX staged in this source folder is byte-identical to the Unity export. The NLA .blend is the editing authority; the baked .blend contains the named export action and muted NLA tracks. Blender-generated .blend1 files are intermediate backups, not the final deliverables.

## Exact sources and frame convention

Sources were imported with Blender `anim_offset=0`. The ranges below refer to that explicit timeline. Blender's default FBX import offset of +1 would display each listed source frame one higher. All source clips are 30 FPS.

The two new Mixamo files actually exist as flat filenames containing `FINAL@` in Downloads. The task's `FINAL/@...` subfolder paths do not exist on this PC.

| Role | Exact file | Original imported action | Used source frames |
|---|---|---|---|
| Crouch / anticipation / push-off | `C:/Users/David/Downloads/STAGE 3 ENEMY NO 1 FINAL@Crouched To Sprinting (1).fbx` | `Armature\|mixamo.com\|Layer0` | 0–18, full clip |
| Prepared heavy sprint | `C:/Users/David/Downloads/STAGE 3 ENEMY NO 1 FINAL@Sword And Shield Run.fbx` | `Armature\|mixamo.com\|Layer0` | 0–20, one short burst |
| Existing attack entry / release / recovery | `C:/Users/David/PROTOTYPE/SPELLSTRIKE/Assets/Stage 3/Enemies/STAGE 3 ENEMY NO. 1/Animations/SunboundWarden_RadiantRush.fbx` | `RadiantRush_AnimationMaster_v2\|Scene` | 42–84 |
| Unchanged canonical skeleton / reference mesh | `C:/Users/David/PROTOTYPE/SPELLSTRIKE/SourceAssets/Stage3/SunboundWarden_Rig_HELMET_FIXED.blend` | No source animation used | Canonical 57-bone rest skeleton |

The canonical skeleton was also checked against `Assets/Stage 3/Enemies/STAGE 3 ENEMY NO. 1/SunboundWarden_Rig.fbx`. Their rest matrices matched before authoring.

`Radiant Rush v2.anim` and the FBX importer settings were inspected read-only. The v2 clip contains Humanoid muscle curves rather than a Blender armature action. Its existing events are `OnRadiantRushChargeStart` at 0.53333336 s and `OnRadiantRushWave` at 1.8 s; neither was changed or copied to the new clip.

## Motion inspection and cuts

- New crouch source 0–8: low anticipation; source 9–18: committed push-off into sprint. Retained at native timing. Output 1–17 matches the adapted source exactly, including the start of the overlap; only its final exit participates in the blend.
- Prepared Run source 0–20: forward-leaning armed run with clear stride/airborne phases. Used once at native speed. No old Rush sprint retiming and no repeated run cycles.
- Current Rush source 42: braking/attack-entry pose; 43–44: visible transition into the sword wind-up. Source 42 provides the entry handle, followed by the existing wind-up, spin, release and complete recovery through 84. No earlier crouch or run section from the old Rush was used.
- Source frame 78 contains the visible one-frame hip flip. The neighboring 77 and 79 poses are coherent. The existing Unity clip's relative frame-76 neighborhood corresponds to this part of the FBX. The new timeline maps source 77/78/79 to output 68/69/70.

## Exact NLA settings

Final authoring range **1–75 at 30 FPS**: 75 integer-frame poses, 74 intervals, **2.4666667 seconds** from first pose to last pose.

| Track/action | Source | Output timeline | Scale | Repeat | Blend-in |
|---|---|---|---|---|---|
| `ShortHeavy_Crouch_Canonical_InPlace` | 0–18 | 1–19 | 1.0 | 1.0 | 0 |
| `ShortHeavy_Run_Canonical_InPlace` | 0–20 | 17–37 | 1.0 | 1.0 | 4 frames |
| `ShortHeavy_CurrentRush_Canonical_InPlace` | 42–84 | 33–75 | 1.0 | 1.0 | 4 frames |

All strips: REPLACE, HOLD extrapolation, auto-blend off, blend-out 0. Upper strips fade in over the lower tracks. Crouch-to-run blend is output 17–21; run-to-attack blend is 33–37. Each blend is 0.1333333 s. The crouch's last pose holds under the last two frames of the entry crossfade.

The heavy-run strip lasts **20 / 30 = 0.6666667 s**, including both crossfades. It has 0.4 s at full weight between frames 21 and 33. No additional slowdown was applied: the provided Run source already supplies the intended cadence.

## Skeleton, in-place handling and root repair

- Kept canonical `Armature`, all 57 bone names, parents, rest matrices and lengths. No new bones, sockets, constraints or leaf bones were exported.
- Sources share bone names but have different rest poses. Separate derived actions transfer evaluated armature-space rotations onto the unchanged canonical rest skeleton, with canonical offsets. This avoids changing the rig to fit each FBX's bind pose.
- Armature object stays at origin, scale `(0.01, 0.01, 0.01)`, X rotation 90 degrees. No animated object travel or heading, target tracking, homing or steering was added.
- Horizontal hip travel is neutralized in armature space; vertical crouch/stride compression is retained. World horizontal hip range is less than 0.000000034 m (floating-point noise).
- Existing sword-attack body rotation/spin is retained as skeletal motion; it does not rotate the controller/root object.
- Only the copied attack action's hip quaternion at source 78 is replaced with a shortest-path midpoint between 77 and 79. Quaternion signs are made consistent across adjacent keys so there is no hidden half-frame twist.
- Raw imported source actions remain available as `SOURCE_UNMODIFIED_Crouch`, `SOURCE_UNMODIFIED_Run`, and `SOURCE_UNMODIFIED_CurrentRush`; the external files are untouched.
- FBX: -Z forward / Y up, global scale 1, unit scale applied, no simplification, one baked take, quarter-frame sampling (120 samples/s on the 30 FPS authoring timeline). The unchanged reference mesh is included for bind-pose preservation. Cameras/grid and other source actions are excluded from the export.

## Validation performed

Actual Blender frame evaluation and offscreen rendering were used, not a live interactive Editor session.

- Inspected every source frame: 19 crouch poses, 21 run poses and all 84 current Rush poses, before choosing cut points.
- Inspected all 75 result frames from front-oblique and side views: low anticipation, push-off, first/middle/final run poses, both blends, full sword attack and recovery.
- Inspected fractional-frame images at both joins and output 67–71. No one-frame root flip or pose teleport was observed in these previews. Final output 69 no longer contains the corrupted source pose.
- Compared 297 quarter-frame samples of the NLA result with the reimported FBX. Maximum world bone-position difference: approximately **0.00001047 m** (0.01047 mm). No angular difference was measurable with the float-precision comparison used.
- Maximum hip angular change per quarter-frame: approximately **8.545 degrees** across the whole clip; no 170-degree root spike remains. The repaired neighborhood is smoothly interpolated.
- FBX roundtrip preserves all 57 names and parents. Maximum rest-matrix difference is approximately 0.0000458 in armature units; differences are numerical roundtrip error.
- SHA-256 preservation check: **65 pre-existing files unchanged**, including Warden animations/import settings/controllers, the two new Mixamo source FBXs and reference .blend. See `preservation_manifest.json`, `export_validation.json`, `continuous_motion_validation.json`, and `delivery_validation.json`.
- Weapon compatibility is preserved structurally through the unchanged right-hand/finger hierarchy and bind pose. The Blender render uses the character body without the actual Unity sword attachment. Its final grip/contact appearance still needs the Unity preview below.

## Unity setup afterward

Only the new FBX and its new .meta were added to Assets. The new importer is authored for Humanoid / Create From This Model, one non-looping clip, compression off, source curves retained, root rotation/position baked into pose, and no events. These settings have not been validated through a Unity Editor import in this task.

1. Let Unity import the new FBX and verify the Humanoid Avatar is valid. Preview only this new clip on the canonical Warden with its actual sword and existing grip component. Do not replace the character rig or mesh with the reference export.
2. Verify Loop Time OFF, Loop Pose OFF, Apply Root Motion OFF, and the complete 2.4667 s take with no leading blank frame. Keep compression off for the initial root/join inspection.
3. Check sword grip/arc, floor contact, the two blends, the existing body spin and output frames 68–70 on the actual character. Entry from/exit to the live Animator states remain untested.
4. When integrating this variant, assign the new clip to the intended Radiant Rush state/override. No assignment or transition edits were made here.
5. Add/review events on the NEW clip only in a separate integration pass. Run-strip onset is output 17, approximately 0.5333 s relative to output frame 1. The retained old wave timing maps approximately to output 47, 1.5333 s; treat this as a review marker, not a tested weapon-release event. The old 1.8 s wave timestamp must not be copied blindly to the new timing.
6. Review controller-driven charge duration against the shorter visual sequence separately. This task did not change charge travel, NavMesh, damage, wave logic or existing events, and does not establish runtime straight-line behavior by itself.

No gameplay C# files, Inspector scene references, Animator transitions, scenes, prefabs or existing animation assets were edited. No Unity/PC/Android runtime acceptance is claimed. Supporting changes are limited to this source/evidence folder, the new FBX/.meta and a changelog entry.
