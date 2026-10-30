# GitHub organization and branch workflow

Established: 2026-10-30. Repository: https://github.com/proplayz01-a11y/SPELLSTRIKE

## Branch responsibilities

| Branch | Role |
| --- | --- |
| `main` | Older, divergent GitHub default branch. Do **not** force-push or merge without a separately reviewed release/reconciliation plan. It is not the latest integrated game. |
| `develop` | The single, current integration branch. New features, bug fixes and cinematic changes target this branch. |
| `backend-system` | Former all-purpose integration branch. It already contains gameplay, stages, UI and cinematic changes, so its name is legacy. Retain for backward compatibility while workflows migrate. |
| `backup/spellstrike-2026-10-28` | Pinned recovery snapshot; preserve without further feature work. |
| `feature/<area>-<task>` and `fix/<area>-<task>` | Short-lived task branches created from `develop`. PR back to `develop` after QA. |

**The stages are folders and scenes in one Unity project, not separate permanent branches or repositories.** Separate Git branches represent *work under review*, not stage ownership.

## Current scene map

- Stage 1/tutorial: `Assets/Scenes/TutorialScene.unity` and the Stage 1 assets; optional optimization test scene under `Assets/Scenes/OptimizationTests/`.
- Stage 2: `Assets/Scenes/Stage2Scene/Stage2Scene.unity`.
- Stage 3: `Assets/Scenes/Stage3Scene/Stage3Scene.unity`.
- Stage 4: `Assets/Scenes/Stage4Scene/Stage4Scene.unity`.
- Animated cinematic: `Assets/Scenes/Cinematics/Prologue_Master_Sequence_P01_42_v07_SpellbookWalkPolish.unity`.
- Shared code, prefabs, materials, scenes, animations: `Assets/`.
- Blender/model source: `ArtSource/`.

October 28's backup contains Stage 3 and Stage 4 scenes; October 30's `backend-system` checkpoint includes the subsequent consultation-ready cinematic. Presence on GitHub does **not** certify final-defense polish or Android performance.

## Team workflow

1. `git fetch origin`; inspect `git status` **before changing branches**. Protect uncommitted work.
2. Branch from `develop`, for example `feature/stage4-boss-polish` or `fix/cinematics-book-continuity`.
3. Keep commits narrow. Stage only the work you own. Keep scene and prefab `.meta` files with their asset GUIDs.
4. Run Unity compile/import and relevant Play Mode, camera, progression, UI, and (where appropriate) Android tests.
5. Push the feature branch, open a PR to `develop`, describe changed scenes and tested flows, and merge after review.
6. Tag validated milestones and keep restoration snapshots before risky Unity or Git changes.

Typical future commands **when the local workspace is clean/safely isolated**:

```bash
git fetch origin
git switch develop
git pull --ff-only origin develop
git switch -c feature/stage4-boss-polish
# implement, test, stage only relevant files, commit
git push -u origin feature/stage4-boss-polish
# open a pull request into develop
```

The original workstation has unrelated local changes. **Do not discard, clean, reset, or stash them automatically just to switch to `develop`.**

## Unity and Git safety

- Preserve `.meta` GUIDs; move/rename Unity assets using Unity-aware tooling and review dependencies.
- Use Git LFS for large `.glb`, `.fbx`, `.blend`, images, and other tracked binary assets. Run `git lfs pull` after clone/switch.
- Do not add `Library/`, `Temp/`, recordings, logs, secrets, IDE caches, or generated backups. Previously tracked recovery files require a separate audit and must not be mass-deleted.
- Report affected scene(s), gameplay/cinematic checks, and untested limitations in the PR.
- For cinematic changes verify single-camera Timeline activation, subtitle/dialogue, the correct book size, and handoff to TutorialScene.
- For stage changes verify arena nodes, bosses, inputs, game progression, and performance on actual targets.

## Future cleanup (not done by this checkpoint)

- Carefully audit outdated or duplicate scenes and tracked recovery assets **without deleting history**.
- Reconcile the divergent `main` history only after a version is confirmed stable, reviewed, and explicitly approved.
- Configure GitHub default branch and branch protections/PR requirements in repository settings after team agreement.
- Retire the misleading `backend-system` name **only after** CI, desktop scripts, and other agents have been updated.
