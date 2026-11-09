# SPELLSTRIKE Panel 03 — A03 Frozen Attack Release v02

**Date:** October 25, 2026. **Status:** PC Unity Editor storyboard rough cut, actual Game View and Play Mode checked, silent 7.77-second video exported. **Not** final boss/VFX, 30 FPS or Android device validation.

## Entry point

- **Unity scene:** `Assets/Scenes/Cinematics/Prologue_P03_A03_UltimateRelease_v02.unity`
- **Native Timeline:** `Assets/Cinematics/Storyboard3D/Timelines/Panel03_A03_v02/A03_YoungVan_UltimateRelease_HeroHold_v02.playable`
- **Camera clip:** `Assets/Cinematics/Storyboard3D/Timelines/Panel03_A03_v02/A03_CameraLowRear_0p32m_Push_v02.anim`
- **Static baked pose mesh:** `Assets/Cinematics/Storyboard3D/Characters/Panel03_v01/P03_YoungerVan_UltimateAttack_ReleaseAt1p00_Static.mesh`
- **Silent 7.77-second Unity preview:** `ArtSource/Cinematics/Panel03_v02/Reviews/P03_A03_UltimateRelease_StaticPose_7p6s_UnityPreview.mp4`
- **Approved-board comparison:** `ArtSource/Cinematics/Panel03_v02/Reviews/P03_ApprovedStoryboard_vs_StaticUltimateRelease_v02.png`
- **Actual Play Mode Game View:** `ArtSource/Cinematics/Panel03_v02/Reviews/P03_A03_UltimateAttack_StaticRelease_GameView_3p8s.png`

## Locked composition

Panel 03 is NOT the destroyed home scene, and the shadow is NOT a new named boss. The reference is `Assets/Story/Approved/Prologue_Panel_03_SOURCE_APPROVED.png`. Use a **low, rear-three-quarter 28–35mm cinematic camera** behind Younger Van, looking through a shattered medieval street toward faceless giant violet corruption. Van is small and visibly resisting; the shadow owns the skyline. Canonical A–G floating blue Word Magic tiles are readable intent, but must not be confused with Broken Language text. No new monster identity, extra weapon or standalone readable corruption glyphs.

## Actual gameplay attack animation, frozen

- Inspected the four **actual gameplay** clips under `Assets/VAN AERT/ANIMS` and their embedded `OnAttackRelease` event times: Basic **0.833s**, Medium **0.633s**, Strong **1.267s**, Ultimate **1.000s**.
- Chose **UltimateAttack at exactly 1.000s**, because both arms are extended in the release toward the threat; StrongAttack's precise release frame bends down toward a crouch. This choice is an artistic choice, not a new attack tier definition.
- The approved Rodin Younger Van was first a static-only mesh. A **copy** of his textured skinned source FBX was reimported separately as a valid Humanoid avatar: `Assets/Cinematics/Storyboard3D/Characters/Panel03_v01/YoungVanAert_Rodin_AttackHumanoid_v01.fbx`. Original FBX and approved Panel 01 static pose remain untouched.
- Played the gameplay **UltimateAttack humanoid clip** on that new Humanoid avatar via a temporary Unity Animation PlayableGraph; sampled exactly at **1.000s**; baked the current `SkinnedMeshRenderer` to a 10,069-vertex permanent static `Mesh` asset. Neither a looping attack animation nor ongoing skinning is needed in the final shot.
- Editor screenshot initially showed the FBX variant in untextured white due to embedded material mismatch. The final frozen renderer instead reuses the **exact imported Panel 01 Younger Van PBR material** from `YoungVanAert_ArmsCrossed_StaticPose_v06.glb` and now shows his navy-and-gold robe, black hair and correct styling.
- Original SkinnedMeshRenderer and Animator are **disabled**, frozen MeshRenderer uses authored static mesh. The existing canonical staff from Van's prefab is independent static scene geometry, aimed diagonally toward the shadow.

## Scenery and proxy

- Reused the previously built **715-renderer v03 detailed ruined city** and storm sky from Panel 02.
- Reused approved Panel 03's **14 actual tile/letter meshes** from the original shared `Prologue_GreatWar_02_03_v01.glb`, forming visible A–G magic letter blocks. They are **static in this v02 shot**.
- The faceless shadow is **six imported existing proxy meshes** scaled together to 2.65× on a scene-local presentation root. This is a temporary, dark, huge entity silhouette, not a new Rodin/Tripo boss or a polished terror sculpture. Replacing this with a better 3D silhouette is a separate authorized polish task.
- Hyper3D Rodin status was checked through Blender's COS integration and returned **disabled**. No Rodin job was submitted and no external credits spent.

## Cinematic runtime QA

- 7.6-second Unity native one-track Timeline drives only a **0.320 m camera push**, no narration (approved Panel 03 allows casting sounds to replace the preceding war narration, but those sounds were **not** added).
- Game View Play Mode measured the frozen character center movement **0.000000 m** and the staff translation **0.000000 m** while the camera moves. The original skinned character is disabled; the static 10,069-vertex mesh renders instead.
- The entity proxy has 6 independent renderer parts, and 14 A–G tile/letter renderer parts are active. Both appear with the Young Van in the actual low-angle camera.
- A 70-frame, **7.77-second silent 1920×1080** MP4 was exported from real Unity Game View frames and decoded back successfully. The 20:9 Unity shot is letterboxed, not cropped. ~9 captured FPS reflects screenshot overhead only and is **not** a gameplay frame-rate result.
- Unity Console includes Unity Remote requirements check failure and earlier COS connection disruption. No scene-specific playback failure found during targeted tests; don't claim global Console clean.

## Important unfinished parts

The approved 2D storyboard art is richer: shattered smoke-filled medieval city, bigger black tendril-wing entity, cyan-gold flying A–G spell particles, fallen defenders, fire and heavy atmospheric layering. The current shadow is visibly a simple polygonal proxy and spell tiles are posed statically; this first scene aims to show the actual animation-release silhouette and camera composition. No full Rodin boss, animated spell VFX, smoke, recorded voice/audio or physical Android check has been completed.

**Version isolation:** Panel 01 v08, Panel 02 v05/v04, production/gameplay clips, original Young Van FBX and original shared Panel 02–03 war blockout were not modified. Source Panel03 v01 exploration preserved; selected pose + camera delivered in separate v02.
