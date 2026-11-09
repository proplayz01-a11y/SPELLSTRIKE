# SPELLSTRIKE Implementation Progress Checklist

**Purpose:** Canonical execution checklist for the remaining defense scope.

**Legend**

- `[x]` Validated complete
- `[-]` In progress
- `[ ]` Not started / pending
- `[*]` Implemented but still needs runtime or device validation

## Confirmed Foundation

- [x] Stage 1 PC vocabulary/combat vertical slice and four-question shared review regression
- [x] Stage 2 PC end-to-end vertical slice: Nodes 1-4, ERODE environmental encounter, Drowned Captain, rewards, Stage Complete, five-question review, Stage Select return, and replay/persistence
- [x] Stage 2 Purifying Pearl and Tidecarved Shell reward foundations
- [x] Tidecarved Shell Bag presentation and 25% knockback/stun mitigation validation
- [x] Shared exact-16 Normal Combat lifecycle guard
- [x] Canonical Van Aert player prefab shared by Tutorial, Stage 1, and Stage 2 (Unity batch wiring audit passed; Stage 1 and Stage 2 post-migration Play Mode regression passed; Tutorial remains covered by its prior migrated-player Play Mode validation)
- [x] Stage 2 A05s enemy gameplay checkpoint: Barnacle Husk, Corsair Phantom, Tide Wraith, and Drowned Captain upright, chasing, and attacking after the fresh NavMesh rebake
- [ ] Add canonical Van Aert jump animation/state-machine presentation (future player-polish implementation; gameplay jump logic already exists, and this does not imply adding a mobile Jump control)
- [ ] TD-019 complete-future-pool long-word refill planning (explicitly deferred future work)

## 1. Story and Cutscene Foundation

**Status:** `[*] Prologue/opening-flow PC runtime validation is largely complete. StoryScene and TutorialScene now have successful Samsung Galaxy A05s presentation/input validation, including the corrected responsive Tutorial HUD and mobile controls. Missing-art fallback remains. Stage 1/2 ending cutscenes are explicitly deferred future implementation.`

Reference: [STORY_CUTSCENE_IMPLEMENTATION_HANDOFF.md](STORY_CUTSCENE_IMPLEMENTATION_HANDOFF.md)

- [x] Canonical narrative source identified: `C:\Users\David\Downloads\SPELLSTRIKE_STORY.docx`
- [x] Lock Panels 1-42 through the Tutorial-map wake-up, robe cleanup, staff retrieval, and bridge-route handoff
- [x] Finalized 28-page Canva package statically audited and imported at 1920 x 1080 under production filenames
- [x] Lock Panels 1-4
  - [x] Panel 1 source approved v2 and represented in the final 1920 x 1080 Canva page package
  - [x] Panel 2 source approved v2 and represented in the final 1920 x 1080 Canva page package
  - [x] Panel 3 source approved and represented in the final 1920 x 1080 Canva page package
  - [x] Panel 4 source approved v2 and represented in the final 1920 x 1080 Canva page package
- [x] Lock post-Panel-11 bridge through Panel 42: book entry, TutorialForest awakening, robe cleanup, staff retrieval, and bridge-route handoff are source approved
- [x] Lock character visual bible: Van Aert, Vander Poel, Nevali, robes, age, expressions, color continuity, and Vander's distinct 360-degree staff reference are approved
- [x] Lock art direction and final page presentation: Canva-owned flattened comic pages, embedded copy, 1920 x 1080 output, and the approved visual treatment
- [x] Create and inspect the reusable 28-page `Prologue_Sequence.asset` profile with ordered production Sprite references
- [x] Create reusable `StorySequenceController` with invisible left-half Back, invisible right-half Next, top-center page progress, keyboard support, fades, Skip, and direct final-page routing to TutorialScene (mouse/touch-style navigation, rapid-input guard, Skip, final-page handoff, and keyboard controls passed Play Mode validation)
- [*] Create shared `StoryScene` with click/tap, keyboard, fades, progress, Skip, and missing-art fallback (all 28 production pages traversed cleanly in Play Mode; keyboard navigation passed; initial A05s presentation worked well; missing-art fallback and final Android regression remain pending)
- [x] Add durable sequence-completion persistence (fresh/interrupted restart behavior and confirmed-Skip Prologue persistence passed Play Mode validation)
- [x] Route first Adventure: Main Menu -> Prologue -> Tutorial -> Stage Select
      (fresh, interrupted-after-Prologue, and completed-returning persistence
      states all passed Unity Play Mode routing validation)
- [x] Wire Tutorial completion through the post-Spellbook reward modal:
      `CONTINUE` loads Stage Select and `MAIN MENU` loads the Main Menu
      (implemented, compiled, and previously validated in Unity Play Mode)
- [x] Add Editor-only onboarding QA presets for Fresh Player, Prologue Complete Only,
      Both Complete, and current-state logging without wiping unrelated save data
- [x] Add StoryScene to Build Settings
- [x] Add TutorialScene to the active shared Build Settings list
- [x] Final 28-page Prologue art/caption package exported, statically audited, normalized, renamed, and imported into `Assets/Story/Pages`
- [x] Validate first run, Skip, restart persistence, rapid-input guard, and representative PC layout
      (routing matrix, confirmed Skip, persistence, all-page traversal, mouse Back/Next,
      rapid double-click guard, and representative 16:9 PC readability passed)
- [x] Validate TutorialScene gameplay HUD at the 1920 x 1080 / 16:9 target after
      correcting legacy gameplay Canvas scaling and the off-screen Attack button layout;
      the intended layout no longer depends on Free Aspect
- [x] Manually spot-check StoryScene keyboard navigation: Space, Enter, Left Arrow, and Right Arrow
- [ ] Validate StoryScene missing-art fallback in Play Mode
- [x] Validate StoryScene/TutorialScene Android safe area, text readability, touch presentation, and UI scaling
      - Initial A05s install/launch and StoryScene presentation passed; cutscene presentation was reported working well.
      - Tutorial gameplay HUD exposed mobile-fit issues on-device, leading to the canonical responsive mobile combat-HUD redesign.
      - Canonical Tutorial mobile HUD was revalidated on the physical A05s after the responsive redesign; player/enemy health, Word Bar, 8 x 2 Tile Pool, STRIKE, joystick, and safe-area placement now present correctly on-device.
- [ ] Add Stage 1 ending cutscene using the same system (explicitly deferred future implementation)
- [ ] Add Stage 2 ending cutscene using the same system (explicitly deferred future implementation)

## 2. Android Build and Device Baseline

**Status:** `[-] Full-game Android navigation/input baseline physically validated on A05s; representative performance profiling remains ongoing.`

- [x] Produce installable Android APK/AAB
      - `Builds/Android/SPELLSTRIKE-dev.apk` exists from the 2026-09-11 build pass
        (1,016,007,816 bytes; SHA-256 `48D02CDA7E5C3173F24BFAE7424876B22B5217F97285502B94800058034F2DB3`).
- [x] Install and launch on a real Android device
      - Initial Samsung Galaxy A05s test successfully launched the game, rendered 3D content, ran the StoryScene cutscene flow, and entered Tutorial gameplay.
- [x] Validate touch controls and primary Tutorial HUD actions on the real A05s
      - Left joystick movement works on-device.
      - Right-side touch camera control is implemented separately from movement.
      - Tutorial combat HUD and touch interactions were exercised on the physical device without observed runtime errors.
      - Mobile Spellbook collection now has an explicit `COLLECT` control rather than relying on the PC-only `E` interaction.
- [x] Validate safe area, portrait/landscape policy, text size, and UI scaling for the Story/Tutorial baseline
      - Landscape-only Android orientation is serialized and verified.
      - `Screen.safeArea`-aware Tutorial layout logic is implemented.
      - Canonical A05s combat-HUD layout is Editor-validated at 2400 x 1080: player HP top-left, enemy HP top-right, single-row adaptive Word Bar, 8 x 2 Tile Pool, and STRIKE bottom-center.
      - Canonical Tile Pool mobile grid is locked at cell `71.17 x 66.20`, spacing `5.92 x 5`, padding `L10/R10/T21/B5`, eight columns, two rows.
      - Decorative Tile Pool layers ignore raycasts so they cannot block tile input.
      - Physical A05s regression of the corrected responsive HUD passed; the previously observed fit/layout issue is resolved.
- [x] Make the canonical Tutorial mobile combat-HUD layout authoritative at runtime
      - Responsive authority now lives on the always-active `GameplayCanvas`, so disabling the Tutorial overlay no longer stops mobile layout enforcement.
      - Late-frame mobile grid reassertion prevents legacy `TileManager` recalculation from restoring oversized desktop-style cells.
      - Enemy health bar mobile local scale is explicitly normalized to `1,1,1`.
- [x] Author shared Stage 1 mobile gameplay controls: joystick movement, right-side camera look, and contextual interaction button
      - Normal fragments, the boss fragment, and the Hollow Knight sword reward expose the shared contextual `COLLECT` action while preserving desktop `E`.
- [x] Author shared Stage 2 mobile gameplay controls: joystick movement, right-side camera look, and contextual interaction button
      - Normal fragments and passive reward pickups expose the shared contextual `COLLECT` action while preserving desktop `E`.
- [x] Make the Tutorial combat HUD layout the shared runtime authority for Tutorial, Stage 1, and Stage 2
      - Player/enemy health, Word Bar, 8 x 2 Tile Pool, STRIKE, safe-area rules, and mobile layout dimensions now come from the same responsive authority.
      - Tutorial-only objective/dialogue presentation remains Tutorial-only rather than leaking into stage gameplay.
- [x] Static Stage 1/2 mobile input audit
      - STRIKE and generated word tiles use Unity UI `Button` pointer input, so the gameplay actions are touch-capable without keyboard emulation.
      - Stage world pickups no longer require Android players to have a physical `E` key.
      - Remaining keyboard-only hits in the gameplay code are desktop controls or debug/test shortcuts; no additional Stage 1/2 progression-critical `Press E` prompt was found in the scene text audit.
- [x] Standardize all 12 enabled production scenes on the Android-compatible UI input baseline
      - Each audited scene has one `EventSystem` and one `StandaloneInputModule`.
      - No audited scene retains `InputSystemUIInputModule` or the stale input-action GUID.
      - Existing button and `GraphicRaycaster` counts were preserved during the five-scene repair sweep.
- [x] Build, install, launch, and physically validate the post-sweep full-game Android APK on Samsung Galaxy A05s
      - Fresh APK timestamp: 2026-09-17 02:26:51; size: 1,015,894,325 bytes.
      - ADB streamed install returned `Success`, and `UnityPlayerGameActivity` launched successfully.
      - Final physical-device navigation test passed; the Main Menu Adventure and Stage Select Enter touch regressions are resolved.
- [x] Run Stage 1 device smoke/navigation test as part of the final A05s regression pass
- [x] Run Stage 2 device smoke/navigation test as part of the final A05s regression pass
- [x] Preserve the user-owned Stage 2 collider baseline during Android/input repairs
      - Current production Stage 2 invariant is exactly **409 serialized `MeshCollider` components**; the older 432-enabled-collider snapshot is superseded. Automated cleanup must not modify them or the user-managed ground collider.
- [x] Record clean Stage 2 production FPS, memory, thermal behavior, and major errors
      - SurfaceFlinger baseline: **19.51 FPS**, 51.25 ms average, 66.84 ms P95, 83.56 ms P99.
      - Thermal status `0`; TOTAL PSS `560,393 KB`; TOTAL SWAP PSS `373,769 KB`.
      - Vulkan / Adreno 610 confirmed on the physical A05s.
- [-] Optimize Stage 2 toward stable ~30 FPS on the A05s without gameplay/collider regression
      - Render audit: 769 active renderers, ~926.8k triangle instances, 768 shadow casters, zero LODGroups; `Plane.002` alone is ~526.3k triangles.
      - Pass 1 disables Android Stage 2 static-environment/water shadow casting while preserving shadow receiving; authored but not yet device-measured because the first headless build stopped before producing a fresh APK.
- [ ] Repeat device checks after major VFX/stage milestones

## 3. Four Attack-Tier VFX System

**Status:** `[-] Foundation started; temporarily behind the active Stage 2 A05s performance pass`

- [x] Damage length bands exist: 3-5, 6-9, 10-13, and 14-16 letters
- [ ] Word Cast: distinct basic projectile VFX for 3-5 letters
- [ ] Spell Strike: stronger green projectile VFX for 6-9 letters
- [ ] Arcane Surge: lightning attack VFX for 10-13 letters
- [ ] Absolute Word: large beam VFX for 14-16 letters
- [ ] Distinct impact feedback/audio for all four tiers
- [ ] Boundary validation: 5/6, 9/10, and 13/14 letters
- [ ] PC and Android VFX performance validation

## 4. Stage 3 â€” Mythic Ruins

**Status:** `[ ] Pending`

- [ ] Environment, route, node structure, and arena boundaries
- [ ] Runic Golem
- [ ] Specter Archer
- [ ] Minotaur Shade
- [ ] Titan Colossus boss
- [ ] Stage 3 vocabulary profile, Teach/Target Challenge gates, and post-stage review
- [ ] Fragment/reward, Stage Complete, Stage Select return, and replay persistence
- [ ] PC validation
- [ ] Android validation

## 5. Stage 4 â€” Cursed Gothic

**Status:** `[ ] Pending`

- [ ] Environment, route, node structure, and arena boundaries
- [ ] Plague Wraith
- [ ] Vampiric Shade
- [ ] Gargoyle Sentinel
- [ ] Crimson Duchess boss
- [ ] Stage 4 vocabulary profile, Teach/Target Challenge gates, and post-stage review
- [ ] Fragment/reward, Stage Complete, Stage Select return, and replay persistence
- [ ] PC validation
- [ ] Android validation

## 6. Stage 5 â€” Final Chapter and DEFY

**Status:** `[ ] Pending`

- [ ] Final Chapter environment and progression route
- [ ] Story Remnant Knight
- [ ] Story Remnant Captain
- [ ] Story Remnant Colossus
- [ ] Vander Poel boss
- [ ] DEFY mechanic: trigger, forced letters, timer, success/failure consequences, and repetition rules
- [ ] Stage 5 vocabulary profile and final post-stage review
- [ ] Nevali soul recovery, ending sequence, and epilogue
- [ ] PC validation
- [ ] Android validation

## 7. Score, Leaderboard, Stage Results, and Credits

**Status:** `[ ] Pending`

- [ ] Define authoritative score rules
- [ ] Persist stage score, total score, average word length, longest word, and completion time
- [ ] Stage Results screen
- [ ] Local leaderboard presentation and ranking logic
- [ ] Credits screen
- [ ] Validate score calculations and saved results across restart

## 8. Minigames and Main-Menu Regression

**Status:** `[*] Scenes and code exist; full runtime validation pending.`

- [ ] Validate Word Match / Word Master naming against the proposal
- [ ] Validate Spell It Out
- [ ] Validate Word Excavation
- [ ] Validate Main Menu navigation: Adventure, Minigames, Leaderboard, Settings, Exit, and return paths
- [ ] Confirm minigames remain separate from mandatory Adventure review
- [ ] PC validation
- [ ] Android validation

## 9. Dataset Finalization and Expert Validation

**Status:** `[ ] Pending`

- [ ] Finalize English word dataset source and license
- [ ] Document filtering rules and verified runtime count
- [ ] Validate rarity and word-length rules
- [ ] Review selected vocabulary, IPA, definitions, sentences, distractors, and review items with a qualified English/education expert
- [ ] Apply approved content revisions
- [ ] Preserve evidence of expert review and revisions

## 10. Final PC/Android Defense Regression Audit

**Status:** `[ ] Pending`

- [ ] Verify every approved scope statement against actual gameplay evidence
- [ ] Validate full five-stage story progression
- [ ] Validate all reward and persistence paths
- [ ] Validate all four attack tiers
- [ ] Validate story/cutscene flow, skip behavior, and replay behavior
- [ ] Validate PC build
- [ ] Validate Android build
- [ ] Audit scope, delimitations, objectives, diagrams, data dictionary, and methodology for implementation alignment
- [ ] Produce defense demo path and fallback plan

## Explicitly Removed From Scope

- [x] Pre-test/post-test system removed by scope decision; proposal document still requires coordinated revision
- [x] Mandatory missed-word tile retry removed by scope decision; proposal document still requires coordinated revision

## Current Next Action

Prioritize shared major systems before deferred content-only work:

1. **Stage 2 A05s performance optimization** — move the clean production baseline
   of **19.51 FPS** toward a practical stable ~30 FPS target with real-device
   build/install/measurement after each accepted pass.
2. **Four Attack-Tier VFX System** — resume the shared 3-5, 6-9, 10-13, and
   14-16-letter attack presentation after the Stage 2 performance baseline is stable.
3. **Android performance follow-through** — repeat FPS, memory, thermal, and runtime
   checks after VFX and other major milestones rather than assuming one pass generalizes.
4. **Stage 3 / Stage 4 production** — proceed only after the shared PC/Android and
   attack-presentation foundations are stable enough to reuse safely.
5. **Score / Stage Results / Leaderboard** and remaining cross-stage progression
   systems should be implemented before final defense regression, but they do not
   block the Android baseline.

**Cross-platform completion rule:** from 2026-09-17 onward, player-facing features
are not treated as fully complete until their applicable PC and Android integration
and validation path has been addressed. Editor/debug tooling is exempt.

StoryScene missing-art fallback remains a small validation item. Stage 1 and Stage
2 ending cutscenes remain explicitly deferred and do not block the current major-
systems track.

