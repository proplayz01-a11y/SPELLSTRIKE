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
