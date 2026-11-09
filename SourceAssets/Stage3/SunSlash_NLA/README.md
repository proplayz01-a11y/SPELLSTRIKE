# Sun Slash - full-source 158-frame merge

Current deliverable: FULL158 revision. Both complete source ranges are retained, including the Inward Slash strike and every strike/spin/low finish/recovery in Sword And Shield Slash. The earlier 51-frame authoring checkpoints are superseded. Only one new Unity FBX output is maintained.

## Files
- Unity export: `Assets/Stage 3/Enemies/STAGE 3 ENEMY NO. 1/Animations/SunboundWarden_SunSlash_NLAMerged.fbx`
- Editable NLA: `SourceAssets/Stage3/SunSlash_NLA/Full158/SunboundWarden_SunSlash_Full158_NLA.blend`
- Baked action: `SunboundWarden_SunSlash_Full158`, in `Full158/SunboundWarden_SunSlash_Full158_Baked.blend` relative to this report.
- Visual preview: `Full158/SunSlash_Full158_Preview.gif` (repeats for viewing; Unity clip should not loop).
- Every-frame sheets: `Full158/AllFrames_1.jpg` through `AllFrames_4.jpg`.
- Numerical evidence: `Full158/validation.json` and `Full158/fbx_roundtrip.json`.

## Exact sources and rig
- `C:/Users/David/Downloads/STAGE 3 ENEMY NO 1 FINAL@Stable Sword Inward Slash.fbx`: imported action `Armature|mixamo.com|Layer0`, frames 1-48 at 30 FPS.
- `C:/Users/David/Downloads/STAGE 3 ENEMY NO 1 FINAL@Sword And Shield Slash.fbx`: imported action `Armature|mixamo.com|Layer0`, frames 1-107 at 30 FPS.
- Canonical rig checked: `Assets/Stage 3/Enemies/STAGE 3 ENEMY NO. 1/SunboundWarden_Rig.fbx`.
- Working armature/reference mesh: `SourceAssets/Stage3/SunboundWarden_Rig_HELMET_FIXED.blend`, object `Armature`, 57 bones. Its skeleton matched the canonical rig at inspection.
- Source FBXs, source blend, and existing `SunboundWarden_SunSlash.fbx` were not overwritten.

## Exact NLA timing
Authoring frames 1-158 inclusive; 30 FPS; 157 frame intervals = 5.233333 seconds. No source range is trimmed and no artificial end hold is added.

| Strip | Source range | Timeline | Strip scale | Blend-in | Blend-out |
|---|---|---|---|---|---|
| 01 FULL Inward Slash | 1-48 | 1-56 | 55/47 = 1.170212766 | 0 | 0 |
| 02 FULL Sword And Shield Slash | 1-107 | 46-158 | 112/106 = 1.056603774 | 10 | 0 |

REPLACE blend, HOLD extrapolation, auto-blend off. The upper slash strip fades in during timeline 46-56, replacing the inward recovery with the next attack preparation. Source playback speeds are approximately 85.45% and 94.64%, respectively. Actual attack content from both clips is retained; the overlapping boundary poses are blended.

## Rig and root handling
The source files share the 57 bone names but differ in arm rest poses. Copied ADAPTED/FULL158 actions transfer source armature-space rotations onto the canonical rest skeleton, preserving canonical bone offsets, names, parents and count. All 57 bones, including fingers, are baked every frame, with no curve simplification.

Horizontal hip translation is held at the initial position to remove source travel; vertical compression, turns, leg lifts and steps remain. Temporary two-bone IK contact targets stabilize detected grounded intervals, with 3-frame influence ramps. They are baked and removed from the final rig. This is an in-place adaptation, not an unchanged reproduction of source foot translation. Pivot/foot contact quality should still be checked on the Unity character with its weapon and ground surface.

Armature scale remains 0.01, X rotation 90 degrees, object translation zero. FBX uses -Z forward / Y up, global scale 1, unit scale applied, no extra leaf bones. The unchanged reference mesh is included for bind-pose preservation; it is not intended to replace the canonical character mesh. Preview grid, camera and contact helpers are excluded from export. Only the single baked take is exported.

## Validation
- Blender 5.0.1 used for NLA construction, evaluation, rendering, baking and FBX export.
- All 158 rendered frames inspected, including the Inward strike, 46-56 blend, the full second sequence, spin, low finish and recovery. No abrupt pose jump was observed at the NLA join. Grounded contact correction retains moving/airborne legs rather than pinning both feet throughout.
- Full sequence played in a separate visible Blender 5.0.1 window at displayed 30 FPS with both NLA strips visible. Existing Blender sessions preserved.
- Bake-to-NLA maximum matrix component difference: 0.000350953 in armature-local units.
- FBX roundtrip: one take, 57 bones, identical names and parent hierarchy. World pose maximum matrix component difference: 0.000008166. Rest matrix maximum difference: 0.000045776 in armature units. Scale/orientation retained.
- FBX take name: `SunboundWarden_SunSlash_Full158`. Blender reimports it as `Armature|SunboundWarden_SunSlash_Full158` on frames 2-159 because the importer adds its default one-frame offset; this is still 158 samples and the same duration as authoring 1-158.
- Unity import, attached-weapon validation, Animator transitions, Play Mode and Android validation have NOT been performed.

## Before canonical controller integration
1. Preview the new FBX on the actual canonical Warden; match the existing Generic rig import settings (original animationType 2), root node, scale and orientation. Do not assume Humanoid.
2. Loop Time OFF; Apply Root Motion OFF. Check the complete take and that no leading empty frame is imported.
3. Check facing/turn recovery, foot contact during pivots, floor penetration, weapon grip/arc and body intersections. The Blender preview uses the body without the gameplay weapon attachment.
4. Check entry from CombatIdle and exit back to CombatIdle. The animation is not authored as a seamless loop.
5. Set damage/event timing only after reviewing the full weapon sequence. No attack events were inserted here.
6. This longer multi-strike visual must not be silently paired with the old short attack duration; review gameplay timing separately when controller integration is authorized.

Canonical SunboundWardenAnimController, Unity scenes, prefabs, gameplay scripts, Stage 1 and Stage 2 were not edited by this task. No Unity/runtime acceptance is claimed.
