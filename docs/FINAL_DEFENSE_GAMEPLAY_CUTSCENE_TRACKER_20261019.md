# SPELLSTRIKE: Final-Defense Gameplay and Cutscene Closure

**Opened:** 2026-10-19. **Status:** active execution tracker; no claim of full-game completion.

## User-locked direction

- Finish and validate playable gameplay before producing new post-stage/ending art.
- The current final defense adventure has **four playable stages total**: the existing Stage 1 (Enchanted Kingdom), Stage 2 (Sunken Seas), Stage 3 (Mythic Ruins), then the **Final Stage**. The old standalone Cursed Gothic stage is no longer in scope.
- The Final Stage is a boss gauntlet reusing bosses from Stages 1–3, concluding with Vander Poel as the final boss.
- Use GPT-6.1 Sol Ultra for the actual Blender final-stage map; COS owns inspectable Unity import, wiring and QA. Maintain only one live owner for a production Unity scene.
- After gameplay closure, generate **post-Stage-1, post-Stage-2, post-Stage-3** illustrations and the **Final Stage ending sequence**. Reuse the existing comic-page cutscene system and approved visual continuity.
- Protect user-authored scenes/prefabs, existing save state and unsaved Unity changes; no destructive clean/reset and no automatic stage unlock/debug skip.

## Verified baseline at initial audit (2026-10-19)

| Area | Evidence | Current gate |
| --- | --- | --- |
| Stage 1 | Production SampleScene has Nodes 1–4, Hollow Knight, StageCompletePanel and gameplay HUD; prior Stage 1 PC vertical-slice validation in implementation checklist. | Re-run full current UI/attack/damage/XP and stage exit regression; Mage HUD production art was recently changed and its new proportions/Play QA have not been signed off. |
| Stage 2 | Stage2Scene includes Nodes 1–4, Drowned Captain, vocabulary practice and Stage Complete; earlier PC end-to-end and A05s enemy checkpoints were recorded. | Re-run with latest shared changes; A05s approximately 19.51 FPS baseline has an outstanding ~30 FPS optimization goal. Preserve **409 serialized MeshColliders**. |
| Stage 3 | Stage3Scene contains four nodes, vocabulary targets, fragments and StageCompleteCanvas. Titan V017 four real ground-contact stomps passed original Stage3 Play QA (2026-10-14). | Full player-driven Node 1→4, fourth-riddle gate, boss damage/death, review, rewards and Android regression remain unverified as one uninterrupted run. Isolated QA does not certify them. |
| Final Stage | No Final Stage scene present in the enabled EditorBuildSettings list and no matching production scene discovered during this audit. | Map, scene, boss-gauntlet transition logic, Vander Poel fight, victory/ending still to be built and tested. |
| Stage select | `StageSelectManager.stageSceneNames` still includes `Stage4_CursedGothic` and `Stage5_Final`; Stage Select scene is in Build Settings. | Migrate carefully to four-stage scope without breaking save/unlock indices, StageRunSelection, ItemSelection, and mandatory review. Do not blindly shorten inspector arrays or wipe PlayerPrefs. |
| Cutscenes | `StoryScene`, `StorySequenceController`, and `StorySequenceProfile` exist; 28-page Prologue runtime was tested. | No post-stage profiles/routing verified. `StageCompleteUI.OnContinuePressed` currently loads StageSelect directly. Story routing must be inserted without bypassing review or completion recording. |

## Closure order and acceptance checks

### G1. Preserve current production and verify current combat loop

- [*] Layered Mage HUD size/position corrected and saved in SampleScene. On 2026-10-19, actual Unity Editor Play at **2400×1080 and 1920×1080** displayed live `100 / 100` Text after the dedicated HP Text Canvas sorting fix. Existing portrait, Slider, empty XP, STRIKE, WordBar and 16-tile pool remained visible. Damage/heal transitions and physical-device test remain pending.
- [ ] Full user-controlled Stage 1 chain: collect letters, build words, valid STRIKE damage, enemy damage/heal, four nodes, fragment, stage clear, mandatory review, unlocked next stage.
- [ ] Stage 2 chain likewise, including locking/cracking/timed-pressure attacks and real A05s performance evidence.
- [ ] Stage 3 chain in production, including Titan's fourth-riddle-gated arrival, real attacks, visible death, Node 4 completion, stage review and selection unlock. Distinguish editor-injected damage tests from a genuine player attack.
- [ ] Runtime logging free from progression-blocking exceptions; ensure replay remains safe.

**Stage 3 deep-audit status (read-only worker evidence, 2026-10-19):**
Original Stage3Scene serializes all four progression controllers and their
correct defeat and fragment receiver bindings. Node4's initially disabled
`Stage3_Node4_VioletFragment` is intentional: the defeat UnityEvent enables
it, and StageNodeController unlocks pickup before Stage Complete. Actual
completion remains unverified by a normal full player run.
Two separate failure-path risks need explicit testing:

- Node1/2/3 have `startCombatIfVocabularyFlowFails=1`; a failed teach/
  challenge setup can start combat without completing the pre-combat
  educational gate. Switching the bool alone is **not** safe: the null-flow
  path and sticky Failed state in `VocabularyEncounterFlowController`
  need a recoverable gate path before any fail-closed migration.
- Node4's `Stage3RiddleController` can mark trial complete before exact tile
  pool restoration and before successfully spawning the final Titan laser.
  A technical failure can leave the trial consumed with Titan unawakened
  and `StageNodeTrigger` still marked as started. A guarded recovery must
  restore the exact pool first, then rearm the existing trigger only with
  a living player and still-dormant Titan. No bypassing the four riddles,
  faking the laser, or loosening the real Titan IK gates.

**2026-10-19 Node4 guarded recovery implementation (static checks passed; runtime still pending):**
Modified only `Assets/Stage 3/Scripts/Stage3RiddleController.cs` and
`Assets/Scripts/StageNodeTrigger.cs`, preserving pre-edit source backups
and SHA manifest in `Library/Stage3Node4Recovery_20261019/`. Completion is
now credited **only after** exact normal-tile-pool restoration, successful
final Titan-directed beam and verified `TitanEarthbreakerController`
`Dormant→Awakening` handoff. Technical failures during tile restoration,
final laser or awakening invoke a shared guarded cleanup; `StageNodeTrigger`
may rearm the Node4 trial after a 1-second cooldown **only** with a clean
normal pool, living player, valid stage progression and still-dormant Titan.
All four riddles must be answered again, and the previous damage bonus is
cleared. Failures without provable recovery stay blocked with errors; no
faked success. This does not change Stage 1–3 scenes, Titan V017 attack/IK,
wrong-answer penalty or normal enemy combat behavior. Structural source
assertions, backup hashes and `git diff --check` passed; Unity C# recompile,
recoverable/unrecoverable fault injection and real Stage3 Play QA must still
pass before acceptance.

**2026-10-19 review-gate correction (implemented, runtime regression pending):**
`StageCompleteUI.ShowStageComplete()` previously called `RecordStageCompletion()`
before making any persistent review request. The Stage Select pending-review key
was written only on Continue, so closing the app with the clear modal open
could leave the next stage unlocked with no pending mandatory review.
`Assets/Scripts/StageCompleteUI.cs` now calls
`EnsureStageSelectReviewPending()` before `RecordStageCompletion()`.
The same helper remains in Continue as a safe fallback. It writes through the
existing `StageVocabularyReviewPendingState.MarkPending()` PlayerPrefs.Save,
and Stage Select still owns the questions and successful clear. The source was
backed up at `Library/StageReviewPendingQA_20261019/` before this change.
This requires **restart-at-clear-panel** Unity validation without overwriting
or resetting existing saved progress; code ordering alone is not a Play PASS.

**2026-10-19 runtime observation:** Unity Editor Play logs `SQLite initialization failed: The type initializer for 'Mono.Data.Sqlite.UnsafeNativeMethods' threw an exception.` Persistence may be affected until investigated; this is not a confirmed save-system pass. Unity Remote also reports a connection requirements failure when no Android device is attached. These are independent of the visual Mage HUD Canvas fix.

**2026-10-19 persistence-preservation follow-up:** Verified the real
`DefaultCompany/PROTOTYPE/spellstrike_save.json` parses successfully, includes
the stage/inventory/vocabulary schema, and has 11 stage-progress entries,
358 vocabulary attempts and 966 word-history records (file contents not
modified). No `spellstrike.db` exists beside it. Copied a hash-identical
save backup to `Library/FinalDefensePersistenceBackup_20261019/`.
Unity logs additionally report `DontDestroyOnLoad only works for root
GameObjects`: the database manager is serialized as a child of `_Managers`
and was not guaranteed to survive transitions. Added a targeted root detach
before `DontDestroyOnLoad(gameObject)` in
`Assets/Scripts/GameDatabaseManager.cs`, with the original script preserved
in the same backup folder. Source ordering and `git diff --check` passed;
scene-transition runtime QA remains pending. **Native SQLite remains broken
and is not claimed repaired**; avoid creating a new SQLite file over the
existing JSON progress. A future SQLite migration must explicitly arbitrate
data authority to prevent newly created/old SQLite data from replacing JSON.

### G2. Final-stage production

- [ ] Lock 4-stage progression model and migration-safe Stage Select/Item Selection routing with no broken old saves.
- [ ] Approve functional Blender map contract: separate interconnected boss arenas, clear spawn/checkpoint paths, colliders/export, LOD/mobile triangle and texture budgets; visual design is not Unity gameplay.
- [ ] Generate and inspect true .blend source and exported map via Sol Ultra; validate topology and import scale.
- [ ] COS integration into a separately backed-up **new** final-stage Unity scene; bake/test NavMesh, set trigger boundaries and performance-friendly streaming/visibility.
- [ ] Reuse Stage 1–3 boss mechanics with explicit cleanup/reset between encounters; preserve existing source production scenes.
- [ ] Implement Vander Poel's distinct final encounter and fail/win flows without assuming an unapproved mechanic.
- [ ] Full PC run and physical Galaxy A05s run, including mobile controls, collisions and persistence.

### G3. Story art and sequences (after gameplay gates)

- [ ] Lock post-Stage-1 visual beats and relation to the **Voice** fragment.
- [ ] Lock post-Stage-2 visual beats and relation to the **Memories** fragment.
- [ ] Lock post-Stage-3 visual beats and relation to the **Emotions** fragment.
- [ ] Reconcile old five-fragment narrative (Voice, Memories, Emotions, Identity, Soul) against the now-four-stage gameplay before writing the final ending; **do not quietly drop Identity or Soul**.
- [ ] Lock Final Stage Vander Poel confrontation, Nevali recovery/epilogue and credits/return route.
- [ ] Generate each numbered source panel using the existing approved 16:9 fantasy-comic / bold-outline / painterly cel-shaded style, canonical character and staff references. Keep source artwork separate from editorial captions/layout; do not use rejected candidate assets.
- [ ] Assemble approved final 1920×1080 pages and create independent sequence profiles, preserving existing Prologue and its completion state.
- [ ] Route Stage Complete → post-stage sequence → existing mandatory Stage Select review/next stage; final stage routes to ending/credits. Validate Next, Back, Skip, checkpoint/replay and phone safe area.

### G4. Defense freeze

- [ ] PC full-adventure start-to-credits acceptance video/evidence.
- [ ] A05s full-adventure build/install with FPS, crashes and touch controls captured.
- [ ] Dataset and mandatory vocabulary objective verification, with no review bypass.
- [ ] Defense-critical demonstrations: word tier bands, stage mechanics, persistence, story continuity, final boss, ending.
- [ ] Sync `docs/PROJECT_STATUS.md`, `docs/CHANGELOG.md`, and the formal checklist with only actually verified work. Do not treat old September checklist's Stage 4/5 placeholders as the current user-approved scope.

## Immediate next gate

The first implementation pass should close a *real production* Stage 3 end-to-end test and the current Stage 1 HUD runtime regression, while defining a non-destructive four-stage progression migration. Do not start bulk final-scene wiring or image generation until its dependent behavior is known.

## Boundaries

- The active Unity Editor was on SampleScene at initial audit, and the git tree contained many user modifications. Do not restore or overwrite them.
- Do not use hidden Unity MCP tools. Follow `.codex/skills/spellstrike/SKILL.md` with local file auditing and visible Unity Desktop.
- Do not run Unity or heavy development operations while VALORANT is active.
- This document is a test/implementation tracker, not a declaration that any pending box has passed.
