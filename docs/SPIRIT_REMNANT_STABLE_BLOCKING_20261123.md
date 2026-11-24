# Stage 1 Spirit Remnant — Stable Cinematic Blocking (2026-11-23)

## Bug report

The player could press TALK at different sides of the Spirit (or nearly
inside the large character mesh). The dialogue Cinemachine shots were
derived from the actors' current world positions, so the camera would
produce different and sometimes unusable compositions: a missing actor,
body overlap, or close-up collision/occlusion.

## Fix

- **Stage 1 scene** contains an editor-authored, non-rendering
  `SpiritRemnant_VanCinematicMark` at approximately
  `(75, -122.25, 648)`. It is referenced by the **existing** Spirit
  Remnant NPC's `SpiritRemnantCinematic` component.
- On TALK, the cinematic **locks movement and attacks**, places Van at
  the staging mark with his scaled `CharacterController` disabled only
  during the warp, then re-enables it. Actor-facing and existing
  dialogue-driven shots use the **staged** positions rather than the
  arbitrary approach angle. The Spirit's legacy prefab root, head
  anchor, animation loop, dialogue, and progress wiring remain intact.
- The authored mark is roughly **20 world units** horizontally from the
  Spirit's head in Edit Mode. The component validates a minimum **16**
  world-unit horizontal separation before staging.
- If the player is already at the staging mark, do not repeatedly
  reset his grounded Y; only set his facing.
- The position is intentionally left at the safe staging point after
  dialogue finishes, instead of restoring an unsafe overlapping
  approach position.
- No change to camera collision guards, virtual-camera ownership,
  the Echo Rune Core model, acquisition state, or Spirit dialogue.

## QA

- Entered TALK from extreme close range on opposing sides of the Spirit
  using Play Mode, confirmed Van reached the **same** staging position
  and the opening `SpiritMedium` shot every time.
- In the final Play Mode positioning test, Van stayed at
  `(75, -122.25, 648)` after settling, roughly 19.5 units horizontally
  from the animated Spirit head.
- Reviewed Game View at 0.61x in Stage 1: the opening dialogue showed
  both separated characters, with the Spirit as the speaking subject.
  `VanReaction` correctly framed Van for his line.
- Canceled dialogue via the existing completion/cancel path:
  virtual camera ended, movement unlocked, and both persistent
  `stage1SpiritRemnantMet` and `stage1EchoRuneCoreAcquired`
  remained **false**. No progress was granted on cancellation.
- Existing save had been backed up previously, before the test,
  under `Temp/SpiritRemnantFullReplayBackup_20261123/`.
  Separate scene/script backups for this fix are under
  `Temp/SpiritRemnantBlockingBackup_20261123/`.
- Full device Android QA and a full 25-line manual walkthrough from
  every possible direction remain unverified.

## Git and licensing

These edits remain local and uncommitted. The public repository's
unlicensed Hovl Studio, Vefects, and HomebrewIK demo packs must remain
untracked and undistributed.
