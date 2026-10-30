# SPELLSTRIKE 42-Panel Prologue: Consultation Checkpoint (2026-10-30)

## Current status

- Animated adventure mode routes to the versioned `Prologue_Master_Sequence_P01_42_v07_SpellbookWalkPolish.unity` scene, while preserving historical v06 and the existing `TutorialScene`.
- The cinematic uses a 336.6-second master Timeline with 39 activation blocks representing all 42 story panels.
- The new detailed physical spellbook is instantiated through `SpellstrikeSpellbook_CANONICAL_FIXED_SIZE_v02.prefab` in all identified archive, table, and carrying appearances. The prefab's imported visual has a canonical `localScale` of `(0.12, 0.12, 0.12)`; the book's world size is kept consistent even under differently scaled character rig parents. Framing and page pose, not arbitrary per-panel scaling, change the apparent size.
- Earlier approved war/faceless illustrations are mapped to the new page meshes; Nevali's handwritten-ink geometry remains preserved in the applicable shots.
- The smaller static forest Van meshes in P40–P42 were frozen into an idle pose rather than remaining in a T-pose. Animator components for these static copies stay disabled to avoid sliding.

## Validation completed in Unity

- Master Timeline: **117/117** shot-activation and single-camera checks passed (three samples each across 39 shot blocks).
- An additional 18 representative master-Timeline camera frames were captured for visual review.
- The animated selection in Play Mode loaded the v07 cinematic with navigation and dialogue overlay present.
- End-of-cinematic runtime handoff loaded `TutorialScene` and placed the persistent Van at the intended start area, not the shrine. Original scene-authored player spawn remained `(35.30, -9.68, -164.60)`; the live character root may have a different Y coordinate due to character-controller/grounding offsets.
- Protected v06 and TutorialScene assets were not modified as part of this checkpoint.

## Deferred to final-defense polish

- Conduct a full normal-speed cinematic visual review and approve all 42 panels from actual Adventure Mode playback, including gait, staff grip, forest occlusion, and page readability.
- Finalize any remaining subtle character, camera, dialogue-timing, or forest-shot aesthetic issues; P39 is intentionally a first-person upward-looking awakening shot.
- Optimize and profile the detailed 3D spellbook on the target Android device. An alternate mobile-LOD GLB exists but has not undergone device validation.
- Do not delete QA recordings or historical comparison assets merely because this checkpoint has been committed.

## Paths

- Master scene: `Assets/Scenes/Cinematics/Prologue_Master_Sequence_P01_42_v07_SpellbookWalkPolish.unity`
- Fixed-size prefab: `Assets/Cinematics/Storyboard3D/Props/SpellstrikeSpellbook_v02/SpellstrikeSpellbook_CANONICAL_FIXED_SIZE_v02.prefab`
- Editable model source and exported mobile variant: `ArtSource/Cinematics/SpellstrikeSpellbook/`
- Local QA (not for Git): `Library/PrologueRecordings_20261029/CanonicalSpellbook_v02_20261030/`

This checkpoint means **consultation-ready**, not that every final-defense visual polish item is complete.
