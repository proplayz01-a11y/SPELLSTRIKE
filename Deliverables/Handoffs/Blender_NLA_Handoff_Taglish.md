# Blender NLA handoff — paano natin mina-merge ang animations

**Para sa tropa ni David · SPELLSTRIKE · 19 October 2026**  
**Target workflow:** Blender 5.0 / 5.0.1 → isang baked animation → separate FBX for Unity.

## 1. Ano ang ginagawa natin?

May dalawa o higit pang existing animations. Inaayos natin ang sequence at transition nila sa **NLA Editor (Nonlinear Animation)**, then bina-bake ang evaluated result bilang bagong Action.

```text
Source Action A ─┐
                ├─ NLA: sequence + timing + blend ─ NEW baked Action ─ FBX
Source Action B ─┘
```

Parang timeline ng video editor, pero **bone/object animation data** ang pinagsasama. Hindi video clips at hindi mesh objects ang minemerge. Hindi rin ito simpleng pag-join ng dalawang armature.

**Goal:** buo ang requested movement, smooth ang transition, compatible ang rig, at editable pa rin ang original NLA setup.

## 2. Non-negotiable ni David: walang source trimming

Gamitin ang **buong source frame range** ng bawat animation: wind-up, actual strikes, follow-through, at recovery. Huwag magtanggal ng strikes dahil tinawag itong “basic attack.” Huwag paikliin ang source range para lang maging madaling i-blend.

May importanteng distinction:

- **Full source range:** walang frame range na tinanggal sa Action Clip settings.
- **Overlap:** sabay na nag-evaluate ang dalawang clip; blended ang poses sa overlap. Hindi parehong poses ang lalabas nang separately at full influence.
- **Retiming:** binabago ang playback duration; nireresample ang motion. Hindi nito ginagarantiya na bawat original frame ay magiging sariling integer output frame.

Kung kailangan lahat ng source poses ay maipakita individually at original speed, gamitin ang **sequential placement**, at magdagdag ng bridge/transition kung kailangan. Huwag ipresent ang overlapping/retimed output bilang exact one-to-one preservation ng bawat original sample.

## 3. Quick glossary

| Term | Praktikal na ibig sabihin |
|---|---|
| Armature / rig | Skeleton na gumagalaw sa character. |
| Action | Container ng animation/keyframes. |
| Action Slot | Binding ng animation data sa tamang object sa newer Blender versions. |
| NLA Strip | Isang paggamit ng Action sa timeline. |
| NLA Track | Row/layer na may strips. |
| Source / Action Clip range | Aling frames ng source Action ang ginagamit. |
| Strip / scene range | Saan tumatakbo ang clip sa combined timeline. |
| Scale | Playback timing multiplier; mas mataas = mas mahaba/mabagal. |
| Influence | Gaano kalakas ang contribution ng strip. |
| Bake | I-record ang evaluated result bilang bagong keyframes. |

Ang dalawang strips na gumagamit ng parehong Action ay references sa parehong data. Kapag ine-edit ang Action mismo, puwedeng parehong strips ang maapektuhan. Gumawa ng independent working Action copy bago source-keyframe corrections.

## 4. Inspect muna bago mag-blend

1. Save a **new working `.blend`**. Preserve original FBXs and existing production assets.
2. Import the character/rig and source clips. Panoorin ang bawat clip independently.
3. Record: filename, actual Action name, full source range, FPS, bone count/names, hierarchy, rest pose, scale/orientation, at root movement.
4. Maraming Mixamo files ang parehong `mixamo.com` ang action name. Rename working copies descriptively para hindi mapagpalit.
5. Check na same rig talaga. **Same bone names alone are not enough**—maaaring iba ang rest pose, bone lengths o axes.
6. Kung incompatible, retarget explicitly to the target rig before sequencing. Huwag basta i-assign at umasa na okay na.

Sa SPELLSTRIKE: check VALORANT before running Blender/Unity automation. Preserve unsaved user sessions. Never use Unity MCP for this repository. Isang animation task muna; production Animator/scene integration is separate scope.

## 5. Basic NLA setup sa Blender

1. Piliin ang **target armature**, hindi ang mesh o camera.
2. Sa Action Editor, piliin ang source/working Action at tamang slot. Scrub para masigurong iyon ang gumagalaw.
3. Use **Push Down Action** to put the active Action on an NLA track. Maaari ring mag-add ng Action strip from the NLA Add menu. Kung iba ang menu placement sa version mo, use `F3` search.
4. Repeat for the second Action. Para sa overlap, ilagay ang later clip sa track above the earlier clip.
5. Sa NLA, select a strip and press `N` for properties. I-check separately ang source range at scene/strip range.
6. Habang nagse-sequence, alisin ang unintended active Action sa Action Track, o kontrolin ang influence nito. Puwede nitong takpan ang NLA stack.
7. Huwag iwang may unintended Solo track, muted clip, o Tweak Mode bago mag-judge ng full result.

`Push Down` puts an Action into a playing strip; `Stash` sets it aside on a muted track. `Tab` can enter/exit Tweak Mode on a selected strip. See [Blender Action Editor documentation](https://docs.blender.org/manual/de/5.0/editors/dope_sheet/modes/action.html).

### Settings na kailangang maintindihan

| Setting | Starting choice para sa full-body merge |
|---|---|
| Blending | `Replace`; avoid accidental Add/Multiply stacking. |
| Influence | Full contribution outside the intended transition. |
| Repeat | 1 unless repetition is requested. |
| Auto Blend | Off while setting explicit timings. |
| Blend In | Ramp for the incoming clip; only when overlap is needed. |
| Blend Out | Usually 0 for a handoff to an upper Replace strip; inspect the actual stack. |
| Extrapolation | `Nothing` or deliberately `Hold Forward`, depending on required end behavior. |

**Critical: `Hold` can apply a strip's first pose before its start**, especially when it is the first strip on its own track. An upper later-action track can therefore hide earlier motion. `Hold Forward` keeps the end pose afterward; `Nothing` limits the strip's influence to its span. These are different behaviors, not interchangeable defaults. [Blender NLA sidebar documentation](https://docs.blender.org/manual/uk/5.0/editors/nla/sidebar.html).

## 6. Hanapin ang “common ground”

Compare the **end of A** and **start of B**, then inspect their neighboring frames:

- Hand/weapon position and facing direction.
- Spine, shoulders, hips and overall balance.
- Planted foot versus lifted foot; body weight transfer.
- Movement direction and speed. Matching poses can still produce a jerk if velocities differ.
- Root location/rotation. Same arm pose does not guarantee same world position.

**If poses already match:** try sequential strips first. A long crossfade may unnecessarily soften the attack.

**If close but not identical:** try a short overlap, then adjust based on playback. Six frames is 0.2 seconds at 30 FPS, but it is a starting experiment, not a universal rule.

**If far apart:** preserve both complete clips and author a bridge between their endpoints. Huwag mag-trim ng actual movement to hide the mismatch. Keep any corrected/bridge Action clearly named and separate from originals.

## 7. Actual examples from this project

### A. Draw Arrow → Aim Recoil: matching-pose handoff

Saved setup uses the supplied Desert Archer clips, both at 30 FPS. Inspection recorded identical 65-bone rest skeletons, and the end/start aiming poses matched within tiny numerical differences.

| Strip | Full source range | Combined timeline | Scale | Blend |
|---|---:|---:|---:|---:|
| Standing Draw Arrow | 1–32 | 1–32 | 1 | 0 |
| Standing Aim Recoil | 1–22 | 33–54 | 1 | 0 |

```text
Timeline:   1................32 33.............54
Draw:       [ FULL DRAW ARROW ]
Recoil:                         [ FULL RECOIL ]
                               ↑ same aiming pose
```

Both source endpoints remain as successive samples. **54 sampled frames**, with 53 intervals between first and last sample: approximately **1.77 seconds** at 30 FPS. The saved builder uses `HOLD_FORWARD`, not backward Hold.

Project-relative example folder:
`ArtSource/Stage3/Node2/Animations/DrawAimRecoil_v001/`

Open `S3N2_DesertArcher_DrawAimRecoil_NLA_v001.blend`. The folder also currently contains `S3N2_DesertArcher_DrawAimRecoil_v001.fbx`; its presence alone is not a fresh validation of its contents.

### B. Sun Slash: full ranges with retiming + overlap

Historical saved timing record:

| Strip | Full source range | Timeline | Scale | Blend In |
|---|---:|---:|---:|---:|
| Stable Sword Inward Slash | 1–48 | 1–56 | 55/47 ≈ 1.1702 | 0 |
| Sword And Shield Slash | 1–107 | 46–158 | 112/106 ≈ 1.0566 | 10 |

```text
Timeline:   1...............46.....56......................158
Inward:     [ FULL INWARD ---------]
Slash:                      [ FULL SWORD/SHIELD SLASH -------]
                            <10-frame overlap>
```

The longer timing retains both full source ranges while reaching 158 sampled frames. Elapsed time is `(158 - 1) / 30 ≈ 5.23 seconds`.

**Do not copy the historical setup blindly:** its README/builder records `HOLD` extrapolation. That can mask early motion on an upper track. Use it as a timing reference, then audit extrapolation and actual source contributions. This handoff does not re-certify that legacy blend or silently fix it.

Folder: `SourceAssets/Stage3/SunSlash_NLA/Full158/`  
Editable example: `SunboundWarden_SunSlash_Full158_NLA.blend`  
Timing evidence: `validation.json`  
Context/limitations: `SourceAssets/Stage3/SunSlash_NLA/README.md`

## 8. Root motion and foot contact

For this game's usual NavMesh-driven enemies, gameplay movement should remain authoritative. But **do not blindly flatten all Hip X/Z curves**—that can turn real stepping into foot sliding. Blender and Unity axes can also differ.

Inspect object/root-bone versus hips motion first. If an in-place conversion is needed, work on copied Actions, preserve vertical compression and intended body turns, and correct foot contacts only where the source actually plants them. Do not pin both feet through an animation that deliberately steps or jumps.

Any root correction or foot IK is an additional authored change. Record it and re-check the entire movement, not just the seam.

## 9. Bake into a new dedicated Action

Save the editable NLA `.blend` first. Use a separate bake/export copy so the strip setup remains available.

Select the intended armature and use `F3 → Bake Action` (also available through animation/NLA menus). Baseline:

| Option | Starting value |
|---|---|
| Start / End | Exact full combined range |
| Frame Step | 1 |
| Visual Keying | On |
| Only Selected Bones | Off, to include all relevant bones |
| Overwrite Current Action | Off: create a new Action |
| Clean Curves | Off during validation |
| Bake Data | Pose; include Object when object transforms are part of the result |
| Channels | Required location, rotation and scale |
| Clear Parents | Off |
| Clear Constraints | Off by default; remove/bypass copied authoring constraints only deliberately after a verified bake |

Rename it clearly, e.g. `DesertArcher_DrawAimRecoil_BAKED_v001`. Ensure the intended Action Slot is assigned.

**Mute the source NLA tracks when checking the active baked Action.** Otherwise both may evaluate. If constraints were visually baked, ensure they are not being applied again during baked-only validation. Compare source NLA and baked-only output at the same frames. [Blender Bake Action documentation](https://docs.blender.org/manual/id/5.0/editors/nla/editing/strip.html).

## 10. Export and verify

Export a new FBX, selecting only the intended rig and any deliberately included reference mesh. Exclude duplicate imported rigs, helpers, cameras and preview objects.

For the single-take baked workflow, keep the baked Action active, source NLA tracks muted, and export only the current animation: **All Actions OFF**, **NLA Strips OFF**, animation baking ON. Include all relevant bones, avoid added leaf bones, and start with sampling step 1 / simplification 0. For fast or retimed movement, verify subframes or use finer sampling when needed.

Use scale/orientation settings proven against the actual target character. `-Z Forward / Y Up` was used in our FBX work, but do not blindly apply transforms to an established rig. Animation-only export must preserve the correct bind/rest pose; include a reference mesh if required and verify rather than guessing.

Reimport into a separate clean scene. Check one intended take, matching bone names/parents, rest pose, size, orientation, duration and poses. Importer frame offsets do not automatically mean a source frame was cut: compare equivalent time samples.

Before Unity controller integration: preview on the actual character, match its Generic/Humanoid setup, verify root behavior, Loop Time, weapon attachment and idle transitions. Author damage/release/end events separately from the merged clip's visual acceptance.

## 11. “Done” checklist

- [ ] Every source's full start/end range is recorded and retained.
- [ ] Entire motion actually appears; no later Hold/active Action hides the beginning.
- [ ] Watch normal speed and slow playback; frame-step the joins.
- [ ] Inspect front + side/three-quarter views, feet, hips and attached weapon.
- [ ] No observed pose pop, snap, root jump or unintended spin at the joins.
- [ ] Foot contacts and intended steps/jumps are preserved.
- [ ] Baked-only result matches the NLA result.
- [ ] Export reimport confirms rig, timing and single intended take.
- [ ] Original assets preserved; native NLA and baked copy both saved.
- [ ] Report what was tested, what was only inspected, and what remains unverified.

Numerical checks and successful export do not establish perfect visual quality or working Unity gameplay.

## 12. Common failures

| Symptom | Check first |
|---|---|
| First animation missing | Later strip set to Hold, active Action overriding, wrong track/slot, Solo/Muted state. |
| Armature does not move | Selected mesh instead of rig; wrong Action Slot; no curves bound to target bones. |
| Unexpected full-body spin | Rest/axis mismatch, rotation interpolation, quaternion sign discontinuity. Fix a copy and inspect subframes. |
| Foot sliding after in-place conversion | Root flattened without corresponding contact correction. |
| Baked result looks doubled | Source tracks or constraints still applying with the baked Action. |
| Too many FBX takes | All Actions or NLA Strips export enabled unintentionally. |
| Wrong size/facing | Unit/axis conversion or unverified transform application. |
| “Smooth” but attack vanished | Excessive overlap or source movement hidden by the stack. |

## 13. What to send back to David

```text
Output name:
Source filenames + original Action names:
Full source frame ranges + FPS:
Target rig / compatibility check:
NLA source ranges → scene ranges:
Overlap, scale, influence, extrapolation:
Any copied-action/root/contact corrections:
New baked Action name and range:
Editable .blend + export paths:
Full-motion / seam / foot / weapon checks:
Export reimport result:
Unity tests performed or still pending:
```

## Status of this handoff

Prepared from the current saved project scripts, timing records and source-inspection artifacts on **2026-10-19**. No Blender playback, animation rebake, Unity runtime test or animation-asset edit was performed for this documentation task. Project paths above are relative to the SPELLSTRIKE root; your friend needs the actual referenced files to open the examples.

Official UI references: [Action Editor](https://docs.blender.org/manual/de/5.0/editors/dope_sheet/modes/action.html), [NLA Sidebar](https://docs.blender.org/manual/uk/5.0/editors/nla/sidebar.html), [Bake Action](https://docs.blender.org/manual/id/5.0/editors/nla/editing/strip.html). These localized manual URLs expose the relevant Blender 5.0 reference text; UI labels can vary by version/language. The project-specific examples/settings above are working decisions, not universal Blender defaults.

