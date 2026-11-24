# SPELLSTRIKE — Bramble seamless loop + ground-level gold arrows

Implemented locally on 2026-11-23, after user approval. Nothing was committed
or pushed to the public GitHub repository.

## Bramble Sprite: exact frame 28 to frame 42 seam

- Existing imported Magic Heal clip spans frames **28–42** at 30 FPS
  (14 frames = ~0.4667 seconds). The original FBX and shared source clip
  are untouched.
- New Bramble-only editable clip:
  `Assets/Animations/SoulFeeding/Bramble_MagicHeal_Seamless.anim`.
- Installer:
  `Assets/Editor/BrambleMagicHealSeamInstaller.cs`.
- All **130 animated Humanoid curves** (including Root, hand, foot, and
  muscle channels) were copied from the source and the **last keyframe
  values** were set to exactly match the **first keyframe values**.
  End tangents match the first outgoing tangents to avoid a velocity jump.
- Replaced only the state motion in
  `SampleScene_BrambleSprite_MagicHeal.controller`.
- The general `SoulFeedingMagicHealInstaller` now respects that custom
  override when rerun; other enemy controllers retain their shared source.
- Editor verification: 130/130 bindings, **zero value mismatches** at
  the beginning and end; clip valid Humanoid, looping enabled.
- Play Mode: Bramble ran the user-specified clip at full layer weight and
  reached **normalizedTime >97**, verifying sustained looping.

## Stage 1 navigation arrows: one grounded gold silhouette

- `TutorialDirectionalArrowTrail.cs` no longer constructs the large
  `GoldArrowVisibilityHalo` second SpriteRenderer.
- All arrows use **one identical-size SpriteRenderer**, a smaller 7-world-unit
  width rather than the previous overlaid 17/26-unit combination.
- Stage 1-only ground snapping samples walkable geometry at ~0.25-second
  intervals; arrows hover about 0.55 world units above the ground,
  near Van's ankles rather than +10.5 units over his feet.
- HDR gold intensity remains 5× with a gentle pulse, but its new shader
  produces a softer, warmer gold edge **inside the same sprite quad**:
  `Assets/Shaders/Story/Stage1SingleArrowGoldGlow.shader` and
  `Assets/Materials/Story/Stage1SingleArrowGoldGlow.mat`.
- The original tutorial arrow remains untouched: settings and material
  are only assigned to the Stage 1 `Stage1_SpiritFragmentGuidance` object.
- Full-route coverage and the 120-arrow pool limit remain.
- Editor/Play Mode: zero new C# compile errors; shader supported;
  119 visible arrows during forced waypoint QA; zero duplicate halo objects;
  single-material SpriteRenderers aligned with sloping ground.
- Game View screenshot inspected after tuning gold shader tint (rather than
  washed-out white). No real Android device build/performance test yet.

## Recovery and safety

Backup: `Temp/BrambleSeamAndArrowCleanup_20261123/`, including original
Stage 1 scene, arrow code, Bramble-specific controller, shared editable
animation, and pre-test `spellstrike_save.json`.

The local game save was **byte-for-byte unchanged** after tests.
Unity was returned to Edit Mode. The repository already had other local,
uncommitted Stage and Spirit changes; none were reset or committed.
Third-party packages and animation source were not redistributed.
