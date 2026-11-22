# SPELLSTRIKE — Spirit Remnant Encounter (First Pass)

Date: 2026-11-23

## Source and story intent

Based on `SPELLSTRIKE_STORY(1).docx` and the user's updated Stage 1 direction:

- A faint spirit at the edge of the Enchanted Kingdom village explains Broken Language.
- Van Aert asks about his missing daughter Nevali, identifiable by bright blue eyes.
- The spirit recounts that the Hollow Knight took the crying child for sacrifice.
- The spirit reveals that Nevali's existence is scattered into fragments, but does not
  know all their locations.
- Van Aert is shocked and asks where the pieces went before asking where the
  Hollow Knight can be found. The spirit points him toward the Royal Castle.
- Don't reveal Vander Poel's betrayal in this dialogue.

## Implemented, compiled

- `Assets/Scripts/Story/SpiritRemnantEncounter.cs`:
  first encounter (16 authored dialogue lines) and shorter repeat conversation;
  E on PC, `MobileStageControls` TALK/NEXT on Android, plus a small Next button.
  A runtime ScreenSpaceOverlay dialogue panel shows the speaker and dialogue.
  On **full completion**, marks Stage 1 Goal 2 (`Find the Spirit Remnant`).
  The scene's combat, fragments and other goals are untouched.
- `Assets/Scripts/Story/SpiritRemnantAmbientMotion.cs`:
  lightweight idle float, speaking head nod and light pulse. This is NOT
  lip-sync, voiceover, cinematic camera animation or a Timeline yet.
- `GameDatabaseManager.GameData.stage1SpiritRemnantMet`, persisted to JSON
  and the existing SQLite player key table. Repeats don't award the objective
  again. `GoalsPanelUI` restores Goal 2 when loading Stage 1 after a
  previously completed conversation.
- `Assets/Prefabs/Story/SpiritRemnant_Prototype.prefab` and 3 translucent
  materials under `Assets/Materials/Story/` built with the new safe
  `Assets/Editor/Story/SpiritRemnantPrototypeBuilder.cs` menu command.
  The prefab has six low-poly proxy mesh pieces, one ethereal light, no
  colliders, the dialogue controller, and the hover/talking animation.
  The geometry is TEMPORARY art and should be replaced with the approved NPC
  model/rig when available.

## QA / scene integration status

- Unity successfully compiled the runtime scripts and Editor prefab builder.
- Inspector validation: prefab contains both controllers, 6 mesh renderers,
  3 correctly alpha-configured materials, 1 light, 0 colliders.
- Console had zero errors after compilation and prefab creation.
- **At initial prototype creation, NOT YET INSTALLED IN `Assets/Scenes/SampleScene.unity`**: the active
  stage scene was already dirty with unsaved changes when installation began.
  The tool purposely created the prefab in an isolated Unity preview scene.
  The stage will need the prefab placed by the village route and tested for
  mobile and keyboard range, UI, talking animation, and persistent state.
- A full Play Mode or Android gameplay test was NOT done here.
- Do not confuse the existing generic `NPC` object in Stage 1 with the
  Spirit Remnant. That `NPC` uses the Bramble Sprite T-Pose mesh and has no
  dedicated Spirit Remnant dialogue or Timeline.

## Next

Safely place the prefab in Stage 1 after the current unsaved scene changes are
resolved; bind it to the planned interaction location near the village entrance
and test the dialogue. Then replace the temporary spectral proxy with the
approved Spirit Remnant model and author a proper camera/gesture Timeline if
desired. Keep Nevali's four major Soul Aspects and the deferred Cursed Gothic
concept in separate narrative/data tasks.

## 2026-11-23 — Source GLB texturing integrated into Stage 1

The user supplied `disintegrating humanoid 3d model (1).glb`, with embedded
4096×4096 base-color JPEG and UVs. The matching file was found in the local
Downloads folder, with source SHA-256
`9A9A0DB69455F6B839DD6D6C9DCC8BE7C73F4716F74BA8DC5FD833A7DD6DC9B2`.
This is **not** the same mesh as the original scene's `T-Pose (3).fbx` from
the Bramble Sprite folder, so applying the JPEG directly to that FBX would
mis-map its UVs.

- Imported original textured mesh via glTFast to
  `Assets/Characters/SpiritRemnant/SpiritRemnant_SourceTextured.glb`.
  Imported material: `tripo_mat_79615e27`, mesh:
  `tripo_mesh_79615e27`, image:
  `disintegrating+humanoid+3d+model_basecolor.jpg`.
- Authored an independent translucent URP material using that *actual* GLB
  texture:
  `Assets/Characters/SpiritRemnant/Materials/SpiritRemnant_TexturedSpectral.mat`.
  Kept textured details visible (0.92 alpha) with a cool tint and faint
  emission.
- In `Assets/Scenes/SampleScene.unity`, retained the original
  `Spirit Remnant` prefab instance and attached the correct textured GLB as
  a nested `SpiritRemnant_TexturedVisual` child. Kept the old untextured
  FBX mesh/component but disabled its SkinnedMeshRenderer for easy rollback.
  Added one nonshadowcasting soft blue aura light and
  `SpiritRemnantAmbientMotion` with subtle hovering and light pulse.
  No original source model or boss/objective component was replaced.
- Verified the Spirit Remnant is **upright** in Scene View and textured
  correctly (dark weathered body and blue accents); checked in Play Mode
  that the imported texture is bound, the original white renderer stays
  disabled, and the hover position changes across frames. Saved the
  Stage 1 scene and stopped Play Mode afterward.
- Exact Stage 1 scene backup before the texture work:
  `Temp/SpiritRemnantTextureBackup_20261123/SampleScene.before_glb_texturing.unity`.
- One existing unrelated Unity Remote requirements error appeared when
  entering Play Mode; no texture/missing-material compilation errors.

The previously created `SpiritRemnant_Prototype.prefab` remains a
separate prototype asset. Latest encounter integration supersedes the
initial status described above.

## 2026-11-23 — Dialogue hooked onto actual textured Stage 1 spirit

- Confirmed the source GLB is **not skinned or rigged**: glTF 2.0 with 2 nodes,
  1 static mesh, 0 skins and 0 animations. Unity likewise reports 1
  MeshRenderer, 0 SkinnedMeshRenderers, 0 Animators and 0 AnimationClips.
  Natural bone-driven head and arms will require a rig and suitable clips.
  Existing floating and aura animation works procedurally without rigging.
- Added the existing `SpiritRemnantEncounter` controller directly to the
  **actual** `Spirit Remnant` scene object (NOT a duplicated character).
  Linked the already-installed ambient animation component and
  `ObjectivesCanvas`. Its interaction radius is 11m horizontally, with
  30m vertical tolerance to account for the hovering spirit pivot.
- Existing Stage 1 scene was dirty at integration time. Created a disk backup
  at `Temp/SpiritRemnantTextureBackup_20261123/SampleScene.before_dialogue_hookup.unity`,
  then saved the scene including its previously pending edits; nothing was
  reverted or overwritten with an older scene copy.
- **Runtime Play Mode verified**: the dialogue panel instantiates, first
  SPIRIT REMNANT line displays, advancing changes speaker to VAN AERT
  and text appropriately. This was tested with reflection-set proximity to
  avoid walking through the level; the complete dialogue was deliberately
  NOT triggered so no story-progress flag was written to the user's save.
- Fixed a UI issue discovered in the first runtime test (duplicate Android
  NEXT button displayed behind the dialogue). Dialogue now uses the panel's
  own NEXT button; the shared mobile interaction button is for TALK only
  when conversation is not active. A fresh Unity assembly compilation
  succeeded after this fix, but a complete post-recompile device/UI
  walkthrough remains outstanding.
- No Mixamo rigging, authored character gesture clips, lip synchronization,
  voice acting, or cinematic camera Timeline were added.

## 2026-11-23 — Replaced Spirit Remnant visual with user-supplied 71-bone rig

The user explicitly requested that `character_rest_pose (1).glb` replace
the Spirit Remnant. Verified the exact same bytes were available in
`C:\Users\David\Downloads\character_rest_pose (1).glb` (SHA-256
`566FB998700FE01F16244EAFE426E2902A6B4C0454328F10C2263F85BE3B9C44`,
identical to the uploaded file). The GLB has 1 skin, 71 joints, 1 skinned
mesh, 1 embedded 2048x2048 texture, and no built-in animation clips.

- Imported through glTFast as
  `Assets/Characters/SpiritRemnant/SpiritRemnant_Rigged.glb`.
- Duplicated the existing URP spectral material with **the rigged model's own
  UV-matched texture** at
  `Assets/Characters/SpiritRemnant/Materials/SpiritRemnant_RiggedSpectral.mat`.
- In `Assets/Scenes/SampleScene.unity`, **kept the existing `Spirit Remnant`
  root unchanged** (same Transform, scene identity, dialogue controller,
  objective and database references). Added `SpiritRemnant_RiggedVisual`
  as the visible skinned replacement and deactivated the old
  `SpiritRemnant_TexturedVisual` (retained for rollback).
- Preserved roughly the original on-screen character size and vertical placement.
  Compensated for the historical parent root's unusual rotation so the rigged
  model stands upright. Retained the existing blue aura light.
- Rebound `SpiritRemnantAmbientMotion` to the new rigged visual **and to its
  actual `Head` bone** for subtle talking head motion. Existing idle float and
  aura pulse continue; no authored Animator Controller/Timeline was needed for
  these light movements. T-pose/rest pose remains until gesture animation
  clips are authored.
- Saved Stage 1, reopened the scene and verified 71 SkinnedMeshRenderer bones,
  live rigged material/texture, disabled prior visual, valid Head reference,
  and unchanged `SpiritRemnantEncounter` / `ObjectivesCanvas` references.
- In Play Mode verified 71-bone actor is active, hover position and head bone
  rotation vary, conversation opens with its first line, and the mobile
  interaction button is not owned while the dialogue is open. Did not advance
  to completion or write a new story-progress save.
- No compile errors related to the swap. Preexisting unrelated
  `Unity Remote requirements check failed` console error remains.
- Stage 1 before this swap was backed up to
  `Temp/SpiritRemnantRigSwapBackup_20261123/SampleScene.before_rig_swap.unity`.
  No Git commit, push, Android build, lip-sync, or device test performed.

## 2026-11-23 — Talking animation loop installed

User uploaded three `a_tpose_blender_composite_fit@Talking*.fbx`
variants. Verified that exact matching copies were available in the local
Downloads folder, and imported two candidates as independent assets:

- `Assets/Characters/SpiritRemnant/Animations/SpiritRemnant_Talking_01.fbx`:
  3.77-second Humanoid talking clip.
- `Assets/Characters/SpiritRemnant/Animations/SpiritRemnant_Talking_02.fbx`:
  10.27-second Humanoid talking clip, sourced from user-provided
  `a_tpose_blender_composite_fit@Talking (2).fbx` (SHA-256
  `94EA8F0150CA51799BB275E6E9E0E654E4E53C9E250E3B0371C59DDEE8684A64`).
  This longer version was selected for use; all 71 of the existing GLB's
  bone names are shared with the imported FBX.

The long clip has a nearly seamless end/start pose and has been configured
as `Spirit_Talking_Loop` with `loopTime=true` and `loopPose=true`.
The animator root motion settings prevent world-position drift.

Created:

- `Assets/Characters/SpiritRemnant/Animations/SpiritRemnant_HumanoidAvatar.asset`
  via `AvatarBuilder.BuildHumanAvatar` on the **existing GLB rig**, using
  the FBX HumanDescription mapping and the GLB's own skeleton rest transforms;
  Unity confirms `Avatar.isValid=true` and `Avatar.isHuman=true`.
- `Assets/Characters/SpiritRemnant/Animations/SpiritRemnant_Talking.controller`
  with `Spirit Idle` (first frame of Talking held at speed zero) and
  `Spirit Talking` (looping clip at normal speed), plus small blend transitions
  driven by the boolean `IsSpeaking`.
- Updated `Assets/Scripts/Story/SpiritRemnantAmbientMotion.cs` to drive this
  Animator boolean through `SetSpeaking(bool)`. While a rigged Animator is
  connected, the old procedural Head rotation is suppressed so it does not
  overwrite imported gestures; floating movement and blue aura pulse remain.

On the **existing** `Spirit Remnant` scene object, added the Animator to its
existing `SpiritRemnant_RiggedVisual` child and linked it through the ambient
motion component. Did not create a new NPC or change scene placement,
dialogue, stages, boss progression, or GameDatabase.

Verified after scene save/reopen: linked controller/Avatar, real 71-bone
mesh and the existing dialogue controller are persisted. In Unity Play
Mode, `Spirit Talking` ran past normalized time 1.0 and 2.0 (actual looping),
arm/hand bones changed across frames, the character's world root stayed
stationary, and the dialogue swapped Animator `IsSpeaking` to false for
Van Aert's reply, returning to the frozen idle pose. Switching back to the
Spirit's next line set `IsSpeaking` true again. Did not finish the dialogue,
so the player's story-completion save flag was not changed during QA.

Saved scene `Assets/Scenes/SampleScene.unity`, stopped Play Mode, and
verified the scene is not dirty. Backup:
`Temp/SpiritRemnantRigSwapBackup_20261123/SampleScene.before_talking_loop.unity`.
The only console error observed was the unrelated existing Unity Remote
requirements check. No Android device build, voice acting, or lip-sync yet.

## 2026-11-23 — Cinematic dialogue presentation (Stage 1)

Added `Assets/Scripts/Story/SpiritRemnantCinematic.cs` and linked it to
the actual rigged `Spirit Remnant` and its existing dialogue system. This
is a **dialogue-driven Cinemachine shot director**, not a preauthored Unity
Timeline: its camera moves are selected by the active dialogue line so
the player can read at their own pace.

- Reuses the existing physical Main Camera and CinemachineBrain through
  a temporary high-priority virtual camera. No duplicate physical cameras
  or audio listeners.
- Shot beats: establishing two-shot, Spirit medium/close-ups, Van Aert's
  reaction close-ups, sacrifice revelation, shattered-soul revelation,
  and a wider farewell beat. Camera movement is eased with restrained
  push-ins during long lines.
- While talking, the Spirit's **actual rigged visual** turns to face Van,
  and Van faces the Spirit. Their previous world rotations are restored
  on leaving the conversation; original map placement and scene hierarchy
  are preserved.
- Camera composition was checked in Game View and revised until the
  Spirit's glowing eye and Van's face were clearly visible, avoiding
  the enormous character silhouettes clipping the camera.
- `PersistentThirdPersonCamera.cs` camera obstacle guard now skips only
  while the cinematic's static `IsCinematicActive` flag is set, leaving
  normal camera collision handling in place during gameplay.
- Existing movement/attack lock APIs freeze controls. The shot director
  hides screen-space combat HUD canvases and restores all previous canvas,
  attack, camera and movement states when the encounter exits.
- Dialogue UI now features letterboxing, a gold chapter heading,
  navy/gold panel styling, contextual title changes, progressive text
  reveal, REVEAL/CONTINUE/FINISH labels, and E or touch navigation.
  Escape closes the conversation without falsely completing the quest.
- Corrected the TALK interaction center from the inherited offset root
  to the visible rigged Head transform; uses 11m horizontal range and
  30m vertical tolerance for the hovering Spirit.

### Verification

- Unity compiled successfully. The cinematic component was added to the
  existing Spirit scene GameObject, saved, and its dialogue/head links
  verified after reopening.
- Play Mode with a QA-only nearby player placement confirmed the
  genuine proximity trigger, Android TALK ownership, Cinemachine
  camera takeover, automatic face-to-face staging, HUD suppression,
  talking-animation switching, and Van Aert's locked movement.
- Individual screenshot checks were made for the Hollow Knight
  sacrifice reveal and Van Aert's reaction to the fragmented soul.
- Cancelling without finishing restored 13 gameplay canvases, movement,
  and the persistent FreeLook camera and did not mark the conversation
  completed in the GameDatabase.
- The unrelated Unity Remote requirements check warning persists.

## 2026-11-23 — Occluded opening camera fix

The user showed a Game View screenshot with a huge tree trunk blocking
both characters during the opening Spirit Remnant line. Investigation
in Play Mode identified the Stage 1 decorative mesh
`group313951583.001` (240 triangles) as an actual camera blocker
despite the imported tree having **no Collider** in the gameplay scene.
An isolated mesh-proxy raycast confirmed the physical line-of-sight
hit while normal Physics.RaycastAll returned no hit.

- `SpiritRemnantCinematic.cs` now creates a short-lived local Physics3D
  scene containing temporary, non-gameplay MeshCollider proxies for
  nearby static decorative meshes. The proxies can be raycast by the
  cinematic but **cannot affect the Stage 1 player/colliders**. All
  proxies and the local scene are unloaded when the conversation ends.
- When a shot is selected, the director evaluates several camera azimuths
  around the target, checking the real environment collider visibility,
  imported *visual* tree meshes, both character faces and the central
  3×3 screen-view area, not just a single straight ray.
- CPU-only per-triangle checks proved too heavy in the first prototype;
  they were replaced with isolated PhysicsScene BVH queries. Play Mode
  measured ~37–48 ms for the entire encounter camera setup and about
  0.16 ms for two visibility checks on the development machine.
- The original wide **opening** two-shot still allowed enormous scenery
  to occupy the right side of the frame even with character rays clear.
  First line now intentionally uses the verified **SpiritMedium** camera,
  which puts his face, luminous eyes and talking gesture in clear view.
  The opening-shot mapping was runtime-checked via `DetermineShot`,
  and the SpiritMedium camera was visually reviewed in Game View.
- No Stage 1 meshes, colliders, prefabs, dialogue content, or physical
  camera rig were modified by this camera fix; it is code-only.

Unity compiled after the final camera change. The only Console error
was the pre-existing Unity Remote requirements check. The active
`SampleScene.unity` had unsaved Editor changes supplied by the user,
so this task deliberately did **not save or overwrite** that scene.
No real Android device or full end-to-end sequence was tested.

### Remaining scope

Full real-walk entry, a complete end-to-end conversation (including
final objective persistence), and actual Android touch-device QA
remain. No voice acting, lip-sync, bespoke music, Castle landmark
camera marker, preauthored Timeline, APK, Git commit or push. The
Royal Castle beat currently pulls back rather than pointing at an
unverified castle location.

Backup before installing this cinematic:
`Temp/SpiritRemnantCinematicBackup_20261123/SampleScene.before_cinematic.unity`.
