# SPELLSTRIKE — Hollow Knight pre-boss cinematic (Stage 1)

Implemented locally on 2026-11-24 in Unity 6.3 (6000.3.10f1).
No Git commit, push or third-party asset redistribution was performed.

## Approved narrative and trigger

The first explosive confrontation is **Van Aert versus Hollow Knight**.
After Van finishes Node 4's **LIBERATE** vocabulary objective, and only
while the boss is still dormant, he asks where the Knight took his
daughter **Nevali**. The Knight mocks Van and says he will never see
her again. The dialogue ends in Van's resolve and flows directly into
the existing `HollowKnightController.StartBattle()`.

The dialogue does **not** claim Nevali is dead or reveal Vander Poel.
It is distinct from the earlier post-Cursed-Jester Echo Rune call.

## Implementation

- New `Assets/Scripts/Story/Stage1HollowKnightConfrontation.cs` with
  seven scripted, touch/keyboard-progressed lines and typewriter subtitle
  panel, multiple Van/Hollow Knight close-ups, a temporary high-priority
  Cinemachine virtual camera (one actual Main Camera), movement/attack
  locks, HUD hide/restore, and cleanup before combat starts.
- The existing
  `Assets/Scripts/EnemyScripts/HollowKnightTriggerController.cs`
  calls this story sequence from `HandleLiberateCompleted()`.
  Successful cinematic completion invokes the **same** existing
  `StartBossCombat()` method. A missing/failed cinematic falls back to
  normal boss combat, avoiding a closed-arena softlock.
- The existing pre-LIBERATE gate, failure recovery, arena lock,
  `EnemyEncounterHUD`, tile bounds and actual Hollow Knight AI are
  not replaced. Older scenes without a vocabulary gate retain the
  original immediate-battle behavior.
- `PersistentThirdPersonCamera.cs` exempts this cinematic from its
  gameplay-only floor collision guard.
- Van temporarily faces Hollow Knight during the confrontation; his
  original facing is restored afterward. Camera shots use actual
  Humanoid head bones and spherecasts. As the two actors may be
  hundreds of world units apart, cuts between their close-ups avoid
  an inappropriate, lengthy dolly across the level.
- The `Stage1HollowKnightConfrontation` component and trigger
  reference are saved on `HollowKnight_TriggerEncounter` in
  `Assets/Scenes/SampleScene.unity`.

## Mocap-ready, not yet mocap-finished

The cinematic provides separate serialized slots for
**Van Aert Talking Mocap** and **Hollow Knight Talking Mocap**.
When properly retargeted and assigned, each clip will run only for
that speaker's dialogue line through a temporary Playables graph,
without replacing their combat Animator Controllers. The graph is
destroyed and root-motion settings restored before combat.

**No user-authorized talking mocap clip was available or assigned** in
this pass. Do not describe the characters as already having finished
talking/lip-sync animation or voice acting. Final polish still needs
a validated mocap FBX, rig retargeting, gesture/face sync, visual QA
and (if desired) recorded dialogue audio.

## Play Mode validation

- Unity script compile: **0 new C# errors**. The existing unrelated
  Unity Remote requirements warning was still present.
- Editor-only preview began successfully; first Van and Hollow Knight
  dialogue shots were inspected in Game View. Both faces and subtitle
  panels were clearly legible.
- Preview complete: Cinemachine camera and UI destroyed after one
  rendered frame, player movement restored, and **boss combat not
  activated**.
- Direct controlled invocation of the actual
  `HandleLiberateCompleted(vocabularyEncounterFlow)` callback:
  **dialogue opened with the boss dormant**, and
  `bossCombatStarted == false`. On the seventh line's completion,
  the trigger's real combat flag and `HollowKnightController.battleStarted`
  became **true**. The temporary camera and dialogue UI were
  destroyed, movement/attacks unlocked, and gameplay Main Camera
  resumed normally.
- The event callback test bypassed player execution of the actual
  LIBERATE word puzzle. A full manual gameplay test, multiple
  approach angles, Android touch-device QA and actual mocap playback
  are still outstanding.

## Backups

Files backed up at
`Temp/HollowKnightPreBossDialogue_20261124/`:
the prior Stage 1 scene, Hollow Knight trigger code, gameplay camera
guard and pre-test `spellstrike_save.json`. The test run is not
a reason to overwrite any other user's progress.
