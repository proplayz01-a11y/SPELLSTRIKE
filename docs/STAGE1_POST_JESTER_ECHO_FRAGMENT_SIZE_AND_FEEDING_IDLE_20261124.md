# SPELLSTRIKE — post-Jester Echo Rune call, crystal size and feeding idle

Local Unity 6.3 implementation on 2026-11-23/24. This work has **not**
been committed or pushed to the public GitHub repository.

## Canonical user requirements

1. **After collecting** the Cursed Jester's soul fragment—not merely
   defeating him—Van Aert initiates a magical conversation with the Spirit
   Remnant via the **Cracked Echo Rune Core**. The Spirit warns Van about
   Hollow Knight and the castle. Cinemachine is used; the Spirit's body
   is *not* teleported to the player's location.
2. Increase existing collectible crystals by **5–10%**; chosen **8%**.
3. **Every feeding enemy** must stand idle before battle while the Magic
   Heal upper-body feeding loop runs, never walking in place. Resume each
   enemy's authored combat animation when battle begins.
4. Do not redistribute local third-party Unity asset packs.

## Story implementation

- `Assets/Scripts/Story/Stage1PostJesterEchoCall.cs` contains a seven-line
  scripted call with Van as its initiating speaker, the Spirit's remote
  warning, a floating rune model, temporary high-priority
  `CinemachineCamera`, typewriter dialogue, mobile/keyboard NEXT button,
  movement/attack locks and HUD hide/restore. The approved original
  Spirit Remnant dialogue remains unchanged.
- `Node3Controller.OnFragmentCollected()` requests the call. The coroutine
  starts **after end-of-frame**, allowing `FragmentPickup.TryCollectFragment()`
  to deactivate the collected crystal before filming. Node 3 completion
  and arena boundary behavior remain authoritative.
- The game's existing persistent camera collision guard now bypasses
  camera adjustments while `Stage1PostJesterEchoCall.IsCinematicActive`
  is true, just as it does for the Spirit and first-fragment cinematics.
- `Stage1FragmentGuidance` also hides wayfinding during the conversation.
- `GameDatabaseManager` persists `stage1JesterEchoCallCompleted` in
  JSON and SQLite formats; **only completing the final dialogue line**
  sets this flag, allowing duplicate calls to be skipped. An editor-only
  preview deliberately never awards story progress.
- Van is straightened upright during the story shot and restored afterward.
  The earlier long castle camera shot intersected scenery, so the Spirit's
  warning is now a close shot of Van with the remotely speaking rune.
  Real Game View screenshots were inspected for the first/Spirit shots.

## +8% crystal visuals

- `Assets/Scripts/Story/FragmentCrystalSizeStamp.cs` stores each visual's
  original local scale and applies **1.08×**, idempotently.
- `Assets/Editor/StageFragmentStoryAndIdleInstaller.cs` applies that
  increase to **13** existing `FragmentCrystal_Visual` children:
  Stage 1 (4), Stage 2 (4), Stage 3 (4), Stage 4 (1).
- Only the crystal visuals are scaled; pickup collider, parent root,
  animations, color variants and particles are left untouched.

## Feeding idle

- Bramble retains its approved dedicated `Soul Feeding Idle`, while its
  independent Humanoid Magic Heal layer loops frames 28–42.
- Stone Sentinel's original `Walking` state and Cursed Jester's
  `Walk` state are preserved for battle, with a new frozen
  `Soul Feeding Idle` base state (speed 0) before combat.
- `SoulFragmentFeeding` now optionally selects this idle and switches
  back to the proper original state on combat interruption.
- Stage 2 Barnacle Husk, Tide Wraith, Drowned Captain and Corsair Phantom
  already have authored `Idle`/`Float Idle` states, so those were
  preserved. Stage 3 Desert Archer and Sunbound Warden retained their Idle;
  Titan's dormant statue and Stone Golem's non-Humanoid setup were preserved.
- No blanket replacement of all Animator controllers or boss-activation
  code was performed. The right-hand energy endpoint standard remains.

## Verification completed

- Unity Editor scripts compiled with **zero new errors** after the fixes.
- Stage 1 Play Mode: all 3 feeders remained on stationary base idle
  (`normalizedTime == 0`), with the independent Magic Heal layer at
  full weight and >90 loop iterations. Direct `StartBattle()` checks
  stopped feeding for Stone Sentinel and Cursed Jester and returned them
  to their original moving/combat states.
- Stage 2 Play Mode: the 3 active humanoid feeders stayed in Idle while
  the upper-body animation continued. Corsair Phantom was inactive and
  retained its Float Idle default.
- Stage 3 Play Mode: active humanoid feeders had no walking base state;
  Titan remained dormant; Stone Golem was not assigned an invented
  Humanoid animation.
- Cinematic preview verified all seven dialogue beats, Van's upright
  camera framing, core model, text UI, HUD/input release and temporary
  Cinemachine camera cleanup. The preview did **not** set a story flag.
- Actual pickup flow exercised in Play Mode through the real
  `FragmentPickup.TryCollectFragment()` receiver:
  Cursed Jester defeated → fragment unlock → player-range interaction →
  pickup deactivates → Node 3 progress completes → remote
  Cinemachine camera and rune appear. The one-time completed-call flag
  was later present in the test JSON save. This was a controlled
  editor simulation of the pickup, **not** a complete player-operated
  beginning-to-end stage walkthrough.
- The saved scene and scene references were inspected in Edit Mode.

## Safety, backups and remaining QA

Backups are in:

`Temp/Stage1PostJesterCallAndFeedingIdle_20261123/`

The QA copy of the modified JSON save is
`spellstrike_save.after_actual_pickup_QA.json`.
The original pretest JSON save has been restored byte-for-byte and verified
by SHA-256. The project had no SQLite database file in the active save
directory during these tests.

Still outstanding: a full manual gameplay progression test from Spirit
through Cursed Jester, different actual approach angles, Android touch
device build/QA, and later story/Hollow Knight pre-boss confrontation.
Existing approved features and local vendor packages have not been
reset, deleted, committed or redistributed.
