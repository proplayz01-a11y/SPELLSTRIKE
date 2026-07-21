# SPELLSTRIKE Changelog

> **Purpose**
>
> This file records notable project changes in plain language.
> This is not a replacement for Git. It is a human/Codex-friendly summary of what changed and why.

---

# Format

```md
## YYYY-MM-DD

### Added
- 

### Changed
- 

### Fixed
- Rewrote `$spellstrike` skill files as UTF-8 without BOM so Codex can parse skill frontmatter more reliably.

### Removed
- 

### Notes
- 
```

---

## 2026-07-15

### Added
- Added a one-word Stage 1 vocabulary data profile and editor command for the `MITIGATE` prototype.
- Added opt-in Teach, combat barrier, mandatory review, explanatory feedback, corrective retry, and runtime attempt-record components.
- Added a deterministic Stage 1 setup and manual test guide.

### Changed
- Extended attacks to carry the submitted word into confirmed projectile impact without changing existing projectile call sites.
- Added a safe Tile Pool helper that guarantees target letters while keeping them shuffled.
- Added an optional Stage Complete review gate that can proceed directly to `StageSelectScene` after the recorded summary.

### Notes
- Runtime and editor C# assemblies compile with zero errors.
- Scene/Inspector wiring, Unity Play Mode testing, Android testing, expert content validation, and persistent result storage remain incomplete.
- This is a coded vertical-slice foundation, not evidence that vocabulary improved.

---

## 2026-07-10

### Added
- Added reusable `CircularArenaBoundary` support for circular combat-lock walls.

### Changed
- Wired Stage 2 Node 2 to use `C_ArenaBoundary` instead of the Node 1 boundary.
- Connected the Stage 2 Corsair Phantom arena visual ring to the Node 2 boundary activation flow.
- Connected Corsair Phantom's existing Phantom Shot charge VFX to the Phantom Shot windup.
- Updated TD-011 to track Unity validation/tuning instead of missing boundary colliders.

### Fixed
- Prevented Node 2 Corsair Phantom combat from activating the Node 1 arena boundary.
- Fixed Corsair Phantom root tagging so player homing projectiles can target and damage it.
- Fixed shared enemy health bar syncing so Corsair Phantom damage visibly reduces HP after Barnacle Husk.

### Notes
- Requires Unity playtest to confirm the generated collider wall matches the visible ring and blocks the player cleanly.

## 2026-07-09

### Added
- Added a Stage 2 `Spawner` object with `TileSpawner` wired to `3d_Tile`, `TilePoolPanel`, Node 1 arena center, Node 1 shrine exclusion center, and the `Ground` layer mask.
- Added TD-010 to track duplicate startup tile-pool spawning between `TileSpawner` and `TileManager`.
- Added final-frame animation events to Corsair Phantom's `BeingHit` and `Die` animation clips.
- Added Stage 2 Node 2 `CP_ArenaCenter`, `CP_ShrineCenter`, and `C_ArenaBoundary` scene objects.
- Added TD-011 to track final Node 2 arena boundary collider placement.

### Changed
- Updated Stage 2 tile spawning status/debt notes to reflect the restored scene-level `TileSpawner` and remaining Unity validation.
- Wired the Node 2 trigger to configure the shared `TileSpawner` for Corsair Phantom's arena.
- Wired Corsair Phantom's serialized `TileDebuffManager` scene reference.

### Fixed
- Made `TileSpawner.SpawnTilesInPool()` tolerate the existing `3d_Tile` prefab's `WorldTile` component when `WorldTilePickup` is absent.

### Notes
- Requires Unity playtest to confirm valid-word attacks spawn refill world tiles around Node 1.

## 2026-07-08

### Added
- Connected Corsair Phantom's Piercing Scream cone hit to `TileDebuffManager.ApplyTileCracking`.
- Added player damage to Corsair Phantom's Piercing Scream when the player is inside the preserved gameplay cone hit check.
- Added a standalone Bezi SPELLSTRIKE skill prompt for Unity-only VFX and scene work.
- Added Corsair Phantom BeingHit and Death animator transitions with script fallback timers.
- Added direct enemy-defeat node completion support to `StageNodeController`.
- Wired Stage 2 Node 2 to start Corsair Phantom from the Node 1 exit trigger and complete Node 2 on Corsair death.
- Added Stage 2 Node 1 tile-refill spawning scene wiring with a `TileSpawner` and Node 1 ground spawn surface.

### Changed
- Tuned `PiercingScreamWaveVFX` into a faster mouth-origin cone-wave with expanding spectral rings.
- Adjusted Piercing Scream VFX aiming to use full mouth-to-player-body direction while preserving the existing flat gameplay cone check.
- Updated Stage 2 Node 1 combat trigger to send `StartBattle` to `BarnacleHuskController`.

### Fixed
- Fixed Corsair Phantom's missing death receiver so its defeat can advance Stage 2 progression.
- Fixed the Stage 2 player `AttackController` serialized `TileDebuffManager` reference for successful-attack tile debuff reduction.

### Removed
- None.

### Notes
- Requires Unity playtest to confirm Node 1 to Node 2 progression, Node 1 attack-refill tile spawning, Corsair hit/death animation timing, and tile cracking feedback in Stage 2.

## 2026-07-07

### Added
- Added proper project-local `$spellstrike` skill metadata and workflow instructions.

### Changed
- Reworked `.codex/skills/spellstrike/SKILL.md` to use the canonical project memory documents as source of truth.
- Synced the project-local `$spellstrike` skill to the global Codex skills folder for easier detection.

### Fixed
- 

### Removed
- 

### Notes
- No Unity gameplay files were changed.

## 2026-07-06

### Added
- Added Codex support documents to the project:
  - `AGENTS.md`
  - `.codex/skills/spellstrike/SKILL.md`
  - `docs/`
  - `checklists/`
  - `prompts/`
  - `TECH_DEBT.md`
  - `PROJECT_STATUS.md`
  - `ARCHITECTURE.md`
  - `DECISIONS.md`
  - `CHANGELOG.md`

### Changed
- Project now has a dedicated AI/Codex workflow for reading architecture, following layer-based implementation, and avoiding unsafe rewrites.

### Fixed
- Corrected Stage 2 scene `TileManager.currentStage` from `1` to `2`.

### Notes
- Codex should inspect before editing.
- Codex should update project docs when major architecture or progress changes.
- Technical debt should be documented before being fixed.
