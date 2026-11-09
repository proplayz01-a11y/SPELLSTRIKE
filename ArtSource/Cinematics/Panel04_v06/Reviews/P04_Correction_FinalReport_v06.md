# SPELLSTRIKE Panel 04 correction pass — v15 Blender / v06 Unity

**Targeted correction PASS.** Latest saved v14 Blender, shared GLB v01 and Unity v05 were inspected before changes. New versions now preserve the grief beat while correcting ruin readability, the mother's wrist presentation and staff continuity. Four actual Unity GPU stills were captured through a visible Editor menu and visually reviewed.

## 1. Files created

- `C:\Users\David\PROTOTYPE\SPELLSTRIKE\ArtSource\LibraryProps\DefenseSets\Prologue_Home_04_Ruined_v15_CorrectedRuinsHandCanonicalStaff.blend`
- `C:\Users\David\PROTOTYPE\SPELLSTRIKE\Assets\Cinematics\Storyboard3D\Characters\Panel04\Panel04_GriefScene_StaticBlockout_v02.glb`
- `C:\Users\David\PROTOTYPE\SPELLSTRIKE\Assets\Scenes\Cinematics\Prologue_P04_A04_GriefStatic_v06.unity`
- `C:\Users\David\PROTOTYPE\SPELLSTRIKE\Assets\Cinematics\Storyboard3D\Timelines\Panel04_A04_v06\P04_A04_Grief_ReturnHome_v06.playable`
- `C:\Users\David\PROTOTYPE\SPELLSTRIKE\Assets\Cinematics\Storyboard3D\Timelines\Panel04_A04_v06\P04_A04_Camera_CutSequence_v06.anim`
- `C:\Users\David\PROTOTYPE\SPELLSTRIKE\Assets\Editor\Cinematics\Panel04CorrectionV06Builder.cs`
- `C:\Users\David\PROTOTYPE\SPELLSTRIKE\Assets\Editor\Cinematics\Panel04CorrectionV06Preview.cs`
- `C:\Users\David\PROTOTYPE\SPELLSTRIKE\ArtSource\Cinematics\Panel04_v06\P04_CorrectionManifest_v06.json`

- Updated Unity reviews: `C:\Users\David\PROTOTYPE\SPELLSTRIKE\ArtSource\Cinematics\Panel04_v06\Reviews\Unity_v06`
- Blender reviews and attachment proof: `C:\Users\David\PROTOTYPE\SPELLSTRIKE\ArtSource\Cinematics\Panel04_v06\Reviews\Blender_v15`
- This report, JSON validation and contact sheets are beside the deliverable copies.

The protected original environment, Unity v05, shared GLB v01, character pose sources, staff source and approved P01–P03 scene hashes remain unchanged. No old scene or shared asset was overwritten. The new static GLB is 118,028 triangles / 753 renderers in the verified Unity scene.

## 2. Environment fix

- Reframed discovery with a wider, lower establishing camera that shows the broken home, collapsed roof members and existing devastated street/buildings around the grieving couple. The emotional close shots follow it rather than using the wide shot for the whole scene.
- Reused the 291-object surrounding-ruin collection. Opened the intact foreground wall in the new version, added 30 small foundation/splinter transition pieces, and extended the reverse-angle setting with a ground strip, two broken neighboring facade stumps and sparse burnt timber.
- Made scene-local ashed material copies with valid PBR base colors, correcting the prior white Unity floor fallback. Character textures, canonical staff material, global shaders and renderer/project settings were not changed by this correction.
- Laid unsupported family frames on the debris-strewn floor. Moved seven small rubble pieces clear of the canonical staff.

## 3. Hand fix

The mother's sleeve was already baked into a static floor-draped gown, while both hands still used evaluated rig poses. Their wrist heights no longer agreed with the draped cuffs.

- Created scene-only evaluated static hand copies, preserving digit shape, UVs and materials; seated the whole wrists beneath the existing cuffs. Front-hand translation: (-0.018,+0.004,-0.035)m; rear hand: (-0.018,-0.004,-0.018)m. Original rigged hand instances are hidden only in the new version.
- No new cuff cylinder, disk, blood or body redesign. The close-up shows continuous sleeve-to-hand overlap without an exposed floating cap/gap. Original source characters and mother T-pose assets remain untouched.
- A small scene-only Van supporting-arm adjustment moves the hand toward the shoulder/clavicle, clearing the wife's eyes; his identity, costume and kneeling grief posture remain. The mother close camera was reframed to show her closed-eye expression.

## 4. Staff fix

Used the actual continuity asset `C:\Users\David\PROTOTYPE\SPELLSTRIKE\Assets\Character\STAFF.glb`: GUID `85e409f88beb1c94abee8973c66cf04c`, the exact mesh/material referenced by approved P02/P03 scenes. It is imported with the same uniform 1.9 scale used in P02, placed on the floor and kept readable without dominating the couple.

- Canonical source: 5,029 triangles; its embedded JPEG matches the source exactly (SHA-256 `9cc64095443c16348ab7270abb36d7a9be479d8e45abb633b1d24f8d197b4a05`).
- The three placeholder objects `P04_v08_VanAertStaff_WalnutOnFloor`, `P04_v08_Staff_GoldClamp` and `P04_v08_Staff_SingleBlueDiamond` are hidden in the new Blender version and absent from the new export.
- All five blue-bow components are retained; the clue remains readable in its dedicated shot.

## Unity verification

- New visible build/capture commands produced only scene/Timeline v06 and static GLB v02. Guards protect existing outputs, dirty scenes and the VALORANT gate. No Unity MCP/hidden Editor command was used.
- Four GPU PNGs at 960 x 540: discovery/wide, Van grief, mother close, ribbon clue. All position/rotation/FOV checks passed. The two-track, 10-second camera-cut Timeline evaluated correctly; the prior v05 scene setup was restored and verified.
- The capture manifest and exported GLB SHA-256 values match the final files. Protected source hashes were checked again after capture.

![Four Unity shots](C:/Users/David/Documents/Codex/2026-10-25/task-improve-the-spellstrike-panel-01/outputs/P04Correction/P04_v06_Unity_FourShot_ContactSheet.png)

![Wrist correction](C:/Users/David/Documents/Codex/2026-10-25/task-improve-the-spellstrike-panel-01/outputs/P04Correction/P04_Mother_Wrist_Before_After.png)

## 5. Remaining limitations

- Existing floor-draped gown artifacts and rough Van hand detail remain visible prototype polish debt. This is not final cloth, hand contact, rig-deformation or character-animation acceptance.
- These are actual GPU camera stills, not a recorded Play Mode movie. The character poses remain static; recorded narration/audio and transitions to other panels are not validated here.
- PC Editor rendering was checked. Physical Android performance/device playback and physical debris support/simulation are untested.
- The new Unity scene references the existing project's runtime components and new GLB GUID; open it in the SPELLSTRIKE project. The standalone source copies are provided for inspection, not as a self-contained replacement Unity project.


## Exact created-file inventory

- `C:\Users\David\PROTOTYPE\SPELLSTRIKE\ArtSource\Cinematics\Panel04_v06\P04_CorrectionManifest_v06.json`
- `C:\Users\David\PROTOTYPE\SPELLSTRIKE\ArtSource\Cinematics\Panel04_v06\P04_CorrectionReport_v06.json`
- `C:\Users\David\PROTOTYPE\SPELLSTRIKE\ArtSource\Cinematics\Panel04_v06\Reviews\Blender_v15\01_DISCOVERY_WIDE_Draft.png`
- `C:\Users\David\PROTOTYPE\SPELLSTRIKE\ArtSource\Cinematics\Panel04_v06\Reviews\Blender_v15\01_DISCOVERY_WIDE_Final.png`
- `C:\Users\David\PROTOTYPE\SPELLSTRIKE\ArtSource\Cinematics\Panel04_v06\Reviews\Blender_v15\02_VAN_GRIEF_Draft.png`
- `C:\Users\David\PROTOTYPE\SPELLSTRIKE\ArtSource\Cinematics\Panel04_v06\Reviews\Blender_v15\02_VAN_GRIEF_Final.png`
- `C:\Users\David\PROTOTYPE\SPELLSTRIKE\ArtSource\Cinematics\Panel04_v06\Reviews\Blender_v15\03_MOTHER_CLOSE_Draft.png`
- `C:\Users\David\PROTOTYPE\SPELLSTRIKE\ArtSource\Cinematics\Panel04_v06\Reviews\Blender_v15\03_MOTHER_CLOSE_Final.png`
- `C:\Users\David\PROTOTYPE\SPELLSTRIKE\ArtSource\Cinematics\Panel04_v06\Reviews\Blender_v15\04_RIBBON_CLUE_Draft.png`
- `C:\Users\David\PROTOTYPE\SPELLSTRIKE\ArtSource\Cinematics\Panel04_v06\Reviews\Blender_v15\04_RIBBON_CLUE_Final.png`
- `C:\Users\David\PROTOTYPE\SPELLSTRIKE\ArtSource\Cinematics\Panel04_v06\Reviews\Blender_v15\MotherHand_After_v15.png`
- `C:\Users\David\PROTOTYPE\SPELLSTRIKE\ArtSource\Cinematics\Panel04_v06\Reviews\Blender_v15\MotherHand_Before_v14.png`
- `C:\Users\David\PROTOTYPE\SPELLSTRIKE\ArtSource\Cinematics\Panel04_v06\Reviews\Independent_FinalAudit.json`
- `C:\Users\David\PROTOTYPE\SPELLSTRIKE\ArtSource\Cinematics\Panel04_v06\Reviews\P04_Correction_FinalReport_v06.md`
- `C:\Users\David\PROTOTYPE\SPELLSTRIKE\ArtSource\Cinematics\Panel04_v06\Reviews\P04_Discovery_Before_v05_After_v06.png`
- `C:\Users\David\PROTOTYPE\SPELLSTRIKE\ArtSource\Cinematics\Panel04_v06\Reviews\P04_Mother_Wrist_Before_After.png`
- `C:\Users\David\PROTOTYPE\SPELLSTRIKE\ArtSource\Cinematics\Panel04_v06\Reviews\P04_v06_Unity_FourShot_ContactSheet.png`
- `C:\Users\David\PROTOTYPE\SPELLSTRIKE\ArtSource\Cinematics\Panel04_v06\Reviews\Unity_v06\P04_Corrected_UnityPreviewQA_v06.json`
- `C:\Users\David\PROTOTYPE\SPELLSTRIKE\ArtSource\Cinematics\Panel04_v06\Reviews\Unity_v06\P04_Unity_01_DISCOVERY_WIDE_v06.png`
- `C:\Users\David\PROTOTYPE\SPELLSTRIKE\ArtSource\Cinematics\Panel04_v06\Reviews\Unity_v06\P04_Unity_02_VAN_GRIEF_v06.png`
- `C:\Users\David\PROTOTYPE\SPELLSTRIKE\ArtSource\Cinematics\Panel04_v06\Reviews\Unity_v06\P04_Unity_03_MOTHER_CLOSE_v06.png`
- `C:\Users\David\PROTOTYPE\SPELLSTRIKE\ArtSource\Cinematics\Panel04_v06\Reviews\Unity_v06\P04_Unity_04_RIBBON_CLUE_v06.png`
- `C:\Users\David\PROTOTYPE\SPELLSTRIKE\ArtSource\LibraryProps\DefenseSets\Prologue_Home_04_Ruined_v15_CorrectedRuinsHandCanonicalStaff.blend`
- `C:\Users\David\PROTOTYPE\SPELLSTRIKE\Assets\Cinematics\Storyboard3D\Characters\Panel04\Panel04_GriefScene_StaticBlockout_v02.glb`
- `C:\Users\David\PROTOTYPE\SPELLSTRIKE\Assets\Cinematics\Storyboard3D\Characters\Panel04\Panel04_GriefScene_StaticBlockout_v02.glb.meta`
- `C:\Users\David\PROTOTYPE\SPELLSTRIKE\Assets\Cinematics\Storyboard3D\Timelines\Panel04_A04_v06.meta`
- `C:\Users\David\PROTOTYPE\SPELLSTRIKE\Assets\Cinematics\Storyboard3D\Timelines\Panel04_A04_v06\P04_A04_Camera_CutSequence_v06.anim`
- `C:\Users\David\PROTOTYPE\SPELLSTRIKE\Assets\Cinematics\Storyboard3D\Timelines\Panel04_A04_v06\P04_A04_Camera_CutSequence_v06.anim.meta`
- `C:\Users\David\PROTOTYPE\SPELLSTRIKE\Assets\Cinematics\Storyboard3D\Timelines\Panel04_A04_v06\P04_A04_Grief_ReturnHome_v06.playable`
- `C:\Users\David\PROTOTYPE\SPELLSTRIKE\Assets\Cinematics\Storyboard3D\Timelines\Panel04_A04_v06\P04_A04_Grief_ReturnHome_v06.playable.meta`
- `C:\Users\David\PROTOTYPE\SPELLSTRIKE\Assets\Editor\Cinematics\Panel04CorrectionV06Builder.cs`
- `C:\Users\David\PROTOTYPE\SPELLSTRIKE\Assets\Editor\Cinematics\Panel04CorrectionV06Builder.cs.meta`
- `C:\Users\David\PROTOTYPE\SPELLSTRIKE\Assets\Editor\Cinematics\Panel04CorrectionV06Preview.cs`
- `C:\Users\David\PROTOTYPE\SPELLSTRIKE\Assets\Editor\Cinematics\Panel04CorrectionV06Preview.cs.meta`
- `C:\Users\David\PROTOTYPE\SPELLSTRIKE\Assets\Scenes\Cinematics\Prologue_P04_A04_GriefStatic_v06.unity`
- `C:\Users\David\PROTOTYPE\SPELLSTRIKE\Assets\Scenes\Cinematics\Prologue_P04_A04_GriefStatic_v06.unity.meta`

The static hand copies match this staging only; rebake/re-align them if the mother is re-posed.

- `C:\Users\David\PROTOTYPE\SPELLSTRIKE\ArtSource\Cinematics\Panel04_v06\Reviews\P04_Correction_FinalReport_v06.json`
