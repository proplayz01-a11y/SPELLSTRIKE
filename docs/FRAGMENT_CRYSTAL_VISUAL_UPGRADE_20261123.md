# SPELLSTRIKE — Floating Fragment Crystal Visual Upgrade

Implementation date: 2026-11-23

## Approved design

- Collectible **fragment pieces** remain the original first-party, elongated,
  faceted crystals, with glowing seams, a luminous center, gentle levitation,
  and slow rotation.
- Preserve the **authored RGB color of every original pickup**. The project
  currently yields cyan `72ADBE`, purple `B500C5`, and violet `751FE0`.
  Stage 2 contains both cyan and purple sources; Stage 3's fragments are violet.
- **Final user-approved correction:** Remove the previously generated custom
  particles and the rejected Hovl final crystal visual. Reuse only the **actual
  Hovl Studio Sparks and Flares** particle systems on each fragment, spawning
  on a widened spherical shell *outside* the crystal instead of within it.

## Actual scenes updated

| Scene | Visuals upgraded | Gameplay component preserved |
| --- | ---: | --- |
| Stage 1: `Assets/Scenes/SampleScene.unity` | 4 | 3 `FragmentPickup`, 1 `BossFragmentPickup` |
| Stage 2: `Assets/Scenes/Stage2Scene/Stage2Scene.unity` | 4 | 4 `FragmentPickup` |
| Stage 3: `Assets/Scenes/Stage3Scene/Stage3Scene.unity` | 4 | 4 `FragmentPickup` |
| Stage 4: `Assets/Scenes/Stage4Scene/Stage4Scene.unity` | 1 | 1 existing `BossFragmentPickup` |

**Total: 13 pickup visuals installed.** The original `MeshFilter`,
`MeshRenderer`, materials, and collider remain on the source pickup object.
The original `MeshRenderer.enabled` is set false, not deleted. The new
`FragmentCrystal_Visual` child owns the appearance and motion;
`FragmentCrystalFloat` never changes the pickup root or collider position.

## First-party / shareable art

- `Assets/Scripts/Story/FragmentCrystalFloat.cs`: visual-only hovering, slow
  spin, gentle tilt. Restores the child visual's resting transform when disabled.
- `Assets/Editor/FragmentCrystalUpgradeInstaller.cs`: backed-up, idempotent
  installer for the first-party mesh/float, no longer creates a final Hovl
  crystal prefab or the rejected custom particle systems.
- `Assets/Art/Fragments/`: original procedural meshes
  (`FragmentShard_Faceted.asset`, `FragmentShard_ArcaneEdges.asset`) and
  first-party crystal face/edge/inner-light materials.
- `Assets/Editor/FragmentHovlParticleInstaller.cs`: replaces rejected
  particle objects with original Hovl prefab-instance particles, preserving
  the fragment tint and keeping the Hovl data in its installed local folder.
  No pickup root, objective counter, or collider was modified.

## Final Hovl particle halo implementation

- **Source:** Hovl Studio `Prefabs/Environment/Crystal effect blue.prefab`
  (must be installed locally). The per-fragment `HovlCrystalParticles_LOCAL`
  object is a prefab instance referencing the *original* Hovl content.
- **Hovl crystal mesh particle system:** emission disabled and renderer hidden,
  so the rejected Hovl crystal body never replaces our own shard mesh.
- **Hovl Glow:** disabled; user rejected effects stuck inside the crystal.
- **Hovl Sparks:** sphere-shell shape with radius **1.32** and
  `radiusThickness = 0`, 21 emitted/sec, size 0.14–0.29, gentle velocity.
- **Hovl Flares:** sphere-shell shape with radius **1.21** and
  `radiusThickness = 0`, 6 emitted/sec, size 0.28–0.50.
- Start-color tints preserve the original source pickup RGB (cyan/purple/
  violet). Fading uses an RGB-neutral alpha-over-lifetime gradient.
- The rejected `SoftMysticSparkles`, `CrystalStarFlares`, and
  `CrystalSoftAura` children are **removed**. The obsolete custom particle
  installer, nine generated custom particle materials, two generated
  sprite PNGs, and rejected `FinalAssembledCrystal_LOCAL.prefab` were
  removed. Original installed Hovl pack assets are untouched.
- Local-only gallery:
  `Assets/Hovl Studio/Magic effects pack/SPELLSTRIKE Local Assembly/FragmentCrystalGallery_LOCAL.unity`
  showcases only the three colored fragments, with no rejected fourth crystal.
  The separate final-assembly gameplay mechanic remains unimplemented.

## QA and rollback

- Editor install completed without script compilation errors.
- Saved and re-opened all 4 game scenes; 13/13 expected targets have the
  new `FragmentCrystalFloat` component and visual child, their original
  renderer disabled for visual replacement, and their original collider
  present. Stage scenes were not left dirty.
- Unity audit after new particle installation: all 13 source pickups and
  the three gallery examples had Hovl `Sparks` and `Flares`, a hollow
  emission shell, an invisible Hovl mesh renderer, and **zero custom
  particle objects**. Scene objects and colliders were preserved.
- Local gallery inspected in Play Mode after the final change: star flares
  visibly appeared **outside** cyan/purple/violet shards; the rejected fourth
  finished crystal was absent. Floating animation remained unchanged.
- Backups were created **before** each source scene was modified:
  `Temp/FragmentCrystalUpgrade_20261123/*before_crystals.unity`, and
  `Temp/FragmentHovlHalo_20261123/*before_hovl.unity`.
- Not yet performed: full interactive collection regression on all 13
  gameplay pickups, real Android device QA, performance profiling/batching,
  or final crystal gameplay acquisition/assembly wiring.

## Git safety

- Preserve the three unlicensed vendor directories as **untracked local**
  dependencies. Do not add or push them to public GitHub.
- Do not stage `Assets/Hovl Studio/Magic effects pack/` or other vendor
  content. Stage scenes now contain references to Hovl local assets, so a
  clean clone without the licensed pack will show missing VFX dependencies.
- The original Hovl pack has **not** been redistributed. If committing
  these stage changes, review license and dependency implications first.
- No Git commit or push was requested in this task.
