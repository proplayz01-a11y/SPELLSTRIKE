# SPELLSTRIKE Panel 02 A02 v04: Canonical Younger Van and Current Staff

**Date:** October 25, 2026. **Status:** Unity Play Mode validated, review preview generated; first establishing-shot candidate, not final character acting.

## Open

- Scene: `Assets/Scenes/Cinematics/Prologue_P02_A02_GreatWar_v04_YoungVanFixed.unity`
- MP4: `ArtSource/Cinematics/Panel02_v04/Reviews/P02_A02_v04_RealYoungerVan_CorrectStaff_8p5s_UnityPreview.mp4`
- In-Game proof: `ArtSource/Cinematics/Panel02_v04/Reviews/P02_v04_CanonicalYoungerVan_Staff_GameView_4p2s.png`
- Visual comparison: `ArtSource/Cinematics/Panel02_v04/Reviews/P02_ApprovedStoryboard_vs_YoungerVan_v04.png`

## Correction made

- **Removed:** incorrect `YoungVanAert_Prototype_v01.glb` placeholder from the v04 scene.
- **Used exact Panel 01 approved Younger Van geometry/material:** `Assets/Cinematics/Storyboard3D/Characters/Panel01_v02/YoungVanAert_ArmsCrossed_StaticPose_v06.glb`, rendered with `YoungVanAert_Rodin_PBR_Restored`.
- **Reused the current staff from `Assets/Prefabs/VAN AERT.prefab`:** copied its existing `STAFF` renderer/mesh/material, renamed `P02_VAN_AERT_CURRENT_STAFF__CANONICAL`, and planted it beside the hero. This replaces the earlier detached temporary staff instance; the current existing staff's mesh and material were not redesigned.
- Added a small scene-only cyan point light by the staff crystal to preserve its recognizable cool-blue identity against the war sky.
- This approved Younger Van static model is still **arms-crossed**. The staff is visibly planted **beside** him, rather than incorrectly claiming a physically constrained handgrip. A proper war stance/hand grip would need a separate character rig pose/animation pass.

## Preserved and tested

- Reused the existing v03 **715-renderer detailed ruins** city, storm matte, violet abstraction, and war lighting.
- Existing **2-track native Timeline** retains its bindings, original **8.5 seconds**, approved Panel 02 narration, and **0.500 m** lateral camera reveal.
- Real Unity Play Mode samples at 0 / 2.5 / 5.0 / 8.5s found actor translation 0.000000 m; Younger Van screen X moves about 0.345 to 0.326 only due to the camera.
- Both actual Young Van mesh and current staff were visibly rendered. Source mesh path and staff material were verified programmatically in the running Unity Editor.
- Captured 57 real 2400x1080 Unity Game View screenshot frames; encoded a readable **8.6s silent, 1920x1080 letterboxed** review MP4 (~6.6 screenshot-capture FPS, **not** a gameplay framerate measurement).
- Editor console contained a Unity Remote requirements failure and a connection timeout unrelated to the scene; do not treat the full-project Console as error-free. There were no failures in the directed shot regression checks.

## Isolation and next work

Original Panel 01 v08, Panel 02 v03, original shared Great War v01 scene, story assets, gameplay and Build Settings were not modified. The new v04 scene is the only location of this swap.

The ruined-city visuals are still a defense prototype; future fixes can improve terrain/fire/corruption, and separately retarget Younger Van's war acting and hand grip, without replacing his face/identity. 
