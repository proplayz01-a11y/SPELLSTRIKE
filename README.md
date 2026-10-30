# SPELLSTRIKE

**SPELLSTRIKE: A 3D Arena-Based Word-Combat Game for Vocabulary Development Among Youth**

Unity **6.3 LTS (6000.3.10f1)** / URP game project for Windows PC and Android. Players form valid English words from letter tiles to attack enemies in a sequence of themed arenas.

**Status:** Work in progress. The October 30, 2026 checkpoint includes an animated 42-panel prologue. Features, learning impact, and Android performance should not be described as final without appropriate QA and evidence.

## Where everything lives

| Part | Unity project path |
| --- | --- |
| Main Menu | `Assets/Scenes/Main Menu Scene.unity` |
| Stage 1 and tutorial | `Assets/Scenes/TutorialScene.unity` (plus Stage 1 assets and optimization tests) |
| Stage 2: Sunken Seas | `Assets/Scenes/Stage2Scene/Stage2Scene.unity` |
| Stage 3: Mythic Ruins | `Assets/Scenes/Stage3Scene/Stage3Scene.unity` |
| Stage 4: Convergence Citadel | `Assets/Scenes/Stage4Scene/Stage4Scene.unity` |
| Animated prologue | `Assets/Scenes/Cinematics/Prologue_Master_Sequence_P01_42_v07_SpellbookWalkPolish.unity` |
| Shared scripts and other game assets | `Assets/` |
| Cinematic timelines and models | `Assets/Cinematics/` |
| Blender source and model work | `ArtSource/` |

Do not casually rename Unity scenes or assets outside Unity: their `.meta` GUID references matter.

## Get started

1. Install Git LFS and run `git lfs install`.
2. Clone this repository, check out **`develop`** for the integrated project and run `git lfs pull`.
3. Open the project root in Unity 6.3 LTS (6000.3.10f1); allow package import to finish.
4. Open `Assets/Scenes/Main Menu Scene.unity` and test the Adventure flow in Play Mode.

## Branches and contributions

- **`develop`**: current integration branch for the full game and new work.
- **`backend-system`**: previous integration branch, retained for compatibility. It is *not* only backend code.
- **`main`**: original GitHub default branch; currently an older, divergent baseline, **not** the current tested game build.
- **`backup/spellstrike-2026-10-28`**: restore checkpoint. Keep unchanged.
- **`feature/stage3-...`**, **`feature/cinematics-...`**, **`fix/<area>-<task>`**: create short-lived branches from `develop`, then send pull requests back to `develop`.

**Do not create four independent game copies or permanent stage-specific integration branches.** They share scripts, prefabs, art, Timeline assets, and Unity references.

See [branching and project organization](docs/REPOSITORY_BRANCH_GUIDE.md) and [cinematic consultation checkpoint](docs/PROLOGUE_CONSULTATION_CHECKPOINT_20261030.md). More development guidance lives in `AGENTS.md`, `docs/`, `prompts/`, and `checklists/`.

Keep Unity `.meta` files and Git LFS objects; exclude generated `Library/`, `Temp/`, local screenshots, and temporary recordings. Do not force-push or silently change the main/default branch.
