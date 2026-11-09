# SPELLSTRIKE Main Menu V2: Codex & Multi-Agent Workplan

**Specification checkpoint:** 2026-10-30  
**Status:** Design direction agreed; implementation **not yet complete**. This document is a task contract, not a claim of working menu functionality.  
**Repository:** https://github.com/proplayz01-a11y/SPELLSTRIKE  
**Integration target:** `develop`, with isolated `feature/menu-v2-...` branches/worktrees.  
**Local workspace:** `C:\Users\David\PROTOTYPE\SPELLSTRIKE` (currently `backend-system` with numerous unrelated uncommitted changes; **do not switch, reset, clean or stash it automatically**).

## Product direction

Create a distinct, original **SPELLSTRIKE** 2.5D fantasy Main Menu. Bookworm Adventures supplied the broad inspiration of a picturesque world containing playable object-buttons, **not** artwork, layout, logos, or assets to copy.

- **Background**: connected continuous landscape inspired by the actual stages, winding journey path and atmospheric perspective. Stage 1 Enchanted Kingdom (forest/shrine), Stage 2 Sunken Seas (ship/ruined temple), Stage 3 Mythic Ruins (desert gateway, pillars and stone ruins). Use the actual Unity stage assets as visual references. An isolated, rough prototype already exists at `Assets/Scenes/MenuConcepts/StageLandmarksMenuBackground_v01.unity`; avoid assuming it is final.
- **Foreground**: Van Aert in an intentional relaxed/heroic pose (never T-pose), magical cyan staff, and **4–5 actual 3D letter tiles** floating and gently rotating on their own render layer.
- **Adventure**: the primary interactive object: large original enchanted stone gateway with a cyan spiral portal. On desktop hover, the spiral starts spinning faster and the rune glow intensifies; on touch, provide equivalent press feedback. Preserve existing Adventure story/cinematic routing.
- **Minigame**: replaces **Arena as the visible main-menu label and object**. A custom purple-and-gold wizard tent, drawing broad style inspiration from the user's earlier ImageGen tent (not copied Bookworm art). Curtains subtly sway and interior glow brightens on hover/selection. **Do not silently delete or redirect the existing Arena gameplay route**: inspect the actual minigame target, and use an honest disabled/coming-soon state until it exists.
- **Spellbook**: lower-right, actual physical enchanted tome, cyan hover glow. Click opens a **two-page full-screen/parchment overlay**, with successfully used words on the left and selected word/definition/length/usage count on the right. Search, scrolling/page navigation and close/back support PC and touch. Reuse existing `SpellbookManager`, `GameDatabaseManager`, `DictionaryManager`, and `MainMenuSpellbookUI` where safe; **no second database**. Validate that failed challenge attempts are not classified as discovered words.
- **Options**: lower-left themed wizard utility object, but retaining genuine settings controls.
- **Quit**: small original environmental exit object with clear label and touch target.
- **Branding**: original SPELLSTRIKE title/logo with readable visual hierarchy. Do not bake text/buttons/interactive props into a single ImageGen picture.

## Technical model

1. **Art background** can use an original ImageGen composition referencing screenshots from real Stage 1–3 assets. Bake no controls into that illustration. For PC/Android, support landscape crop/safe areas.
2. **Foreground interactive props** remain separate GameObjects or UI prefabs. Hover effects, click/hit testing and accessibility cannot depend on background pixels.
3. **3D letter tiles** use a lightweight scene layer/camera composited beneath an independent Screen Space Overlay Canvas. Reuse letter-tile geometry if suitable. Animate transform sine-wave bob and slow rotation; no costly physics simulation.
4. **Canvas** uses a `CanvasScaler` with a landscape reference resolution `2400 × 1080`; preserve large, readable touch targets and TextMeshPro.
5. **Do not overwrite** `Assets/Scenes/Main Menu Scene.unity` until the new concept scene is visually approved and fully routed/tested. Prototype as an isolated scene or UI prefab.

## Ownership: no shared-scene collision

| Owner | Files owned | Deliverable |
| --- | --- | --- |
| **Codex A: Art & landmark staging** | New art under `ArtSource/MenuV2/` and `Assets/MenuV2/Art/` only | Original menu background layers, reusable landmark meshes/prefabs as approved, source and license notes; no editing production scenes |
| **Codex B: Menu behaviors** | New scripts under `Assets/Scripts/MenuV2/` only | Components for gateway spiral/hover and touch feedback, tent hover, 3D letter bobbing, object-button input; Inspector contracts and isolated test prefabs |
| **Codex C: Spellbook & data** | `Assets/Scripts/Spellbook/`, UI under `Assets/MenuV2/Spellbook/`, targeted database glue **only if audited** | Reuse existing history and SQLite persistence, implement book-shaped word viewer, validate successful-word recording; no new persistence subsystem |
| **Prime/integration owner** | **Only owner** of `Assets/Scenes/MenuConcepts/MainMenuV2_Prototype.unity` and later production `Assets/Scenes/Main Menu Scene.unity` | Assemble approved assets, assign scripts and bindings, verify menu routing, record screenshots/video and save after visual QA |
| **QA reviewer** | Read-only test reports under `docs/` or `Library/` local evidence | Compilation and Play Mode checks, Android/device QA, regressions and route/word-history tests |

Workers must read `AGENTS.md` and `docs/SPELLSTRIKE_MODEL_ROUTING_POLICY.md` first. **Never run multiple Unity GUI mutation owners on the same editor/project at once.** Unity asset imports and scene serialization need exclusive ownership; use separate worktrees or isolated branches for independent code changes.

## Recommended sequence

1. **Read-only audits first**: verify current menu route, actual minigame destination, Spellbook history/save, scene dependencies, and art source paths.
2. **Art proof**: single original background concept and composition approved before producing multiple assets. Keep Van, clickable gateway, tent and spellbook separable from background.
3. **Independent Codex tasks**: A/B/C implement owned components in separate worktrees/branches from `develop`; each supplies patch, files changed, explicit Unity setup steps, and tests. Don't assume cloud Codex has uncommitted local prototype assets: cloud sees committed repository state only.
4. **Integration owner** assembles Menu V2 prototype, wiring Adventure to existing tested route; only enable Minigame after its target is confirmed; wire Word Chronicle into existing data.
5. **Actual Play Mode QA**: render/frame at `2400 × 1080`, inspect no clipping and clean hit targets; test keyboard/mouse and Android touch; verify backend persistence across menu/game restart; benchmark a Galaxy A05s.
6. **Signoff**: preserve original main menu as backup; promote the new scene carefully with `.meta` GUIDs and build settings checked. Review, selectively commit, open PR to `develop`. Do not change `main` or delete historical branches as part of UI redesign.

## Acceptance criteria

- The new menu uses **original** fantasy art and actually recognizable Stage 1–3 landmarks; no borrowed Bookworm visuals, buttons or audio remain in the production menu.
- Van is correctly posed, with 4–5 visually smooth yet low-cost floating letter tiles.
- Adventure portal hover spins the cyan spiral and its click reaches the existing Adventure mode.
- Minigame tent behaves as a real selectable object; route is functional or honestly identified as unavailable (never silently launches a wrong mode).
- Spellbook visually opens the two-page Chronicle and lists **successfully submitted words** with unique counts; definitions and persistence are correct after restart.
- Options and Quit remain functional. Mouse and Android touch controls are usable, with visible focus/selection indication.
- Main Menu, Stage 1–4, existing prologue cinematic and original Bookworm-derived menu are not mutated until visual approval and scoped migration.
- Each PR states exact files changed, scene tested, screenshots, performance notes, and known limitations.

## Codex handoff prompt (copy into a Codex task)

> Read `AGENTS.md` and `docs/MAIN_MENU_V2_CODEX_WORKPLAN.md` first. Work exclusively on the workstream assigned to you. Start with a read-only audit and identify the smallest safe integration points. Do not rewrite existing functioning gameplay or storage code. Do not edit `Assets/Scenes/Main Menu Scene.unity`, other stage scenes, cinematic scenes, or someone else's ownership paths. Do not reset, clean, stash or otherwise discard uncommitted changes. If VALORANT is running on the Windows machine, stop GUI automation. Implement small, reviewable changes on an isolated `feature/menu-v2-...` branch/worktree from `develop`. Return changed files, Inspector assignments, tests performed, and blockers. The prime integrator alone controls the Unity Menu V2 scene and production scene promotion.

## Concrete setup options

- **Codex desktop/local**: Open the SPELLSTRIKE repository as a project in the Codex app; use separate agent tasks/worktrees. Local agents may inspect approved local assets after permission; retain the main dirty working tree unchanged.
- **Codex Cloud**: Connect `proplayz01-a11y/SPELLSTRIKE` and use a prepared environment targeting `develop`. Cloud tasks only see repository state available in that environment; don't assume access to the uncommitted local preview scene, downloaded art, or the live Unity editor. Cloud is best for C# logic/unit tests, documentation and narrow PRs; Unity scene visual integration remains local.
- **Chat On Steroids workers**: Can run read-only audits in parallel now. Later, assign one explicit mutation owner at a time while other workers implement only disjoint scripts/files. Workers are not automatically identical to standalone Codex sessions; share this task contract with Codex.
