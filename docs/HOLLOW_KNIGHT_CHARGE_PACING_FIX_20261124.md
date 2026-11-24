# SPELLSTRIKE — Hollow Knight charge pacing and anti-teleport fix

Applied locally on 2026-11-24. No Git commit or push was performed.

## Reported gameplay bug

Hollow Knight appeared to teleport into Van when charging, then slide
afterward. This was a movement and animation-timing problem, not an
actual teleport attack.

## Root cause

- In the saved Stage 1 scene, `HollowKnightController` had
  `walkSpeed = 124.7` and `runSpeed = 300` world units/second.
  This could cross a large part of the arena in a fraction of a second.
- The movement loop for BOTH ChargeAttack and CloseAttack2 used
  `agent.Move(direction * chargeSpeed * Time.deltaTime)` continually
  whenever `isCharging` was true. The serialized
  `chargeDuration = 0.7` was never used, while the actual
  charge animations last approximately 1.93 and 2.10 seconds.
- Direction was sampled once at attack start but there was no
  acceleration, windup, braking or minimum stopping distance.

## Fix

Changed `Assets/Scripts/EnemyScripts/HollowKnightController.cs` and
only Hollow Knight's component in `Assets/Scenes/SampleScene.unity`:

- Walk speed 24, run speed 42, plus script-side caps (24 and 42)
  as defense against accidental high inspector overrides.
- Charge trigger range capped at 82 world units, replacing 102.4.
- Charge peak remains 55 world units/second, but now has
  a **0.23-second windup**, **1.10-second active movement window**,
  **0.16-second acceleration** and **0.22-second braking**.
- The charge aims toward Van's position when triggered, rather than
  re-homing into a later player position. This makes dodges fair.
- Stops the rush approximately 24 units short of its locked
  target. Movement speed and per-frame displacement are bounded.
- This timed movement applies to the ChargeAttack/SlideAttack and
  CloseAttack2 variants, without replacing their original animation
  events, sword damage windows or recovery logic.
- The boss and other character rigs, item drops and story
  cinematics remain untouched.

## Unity Play Mode verification

- No new Unity C# errors. Existing unrelated
  `Unity Remote requirements check failed` warning remains.
- ChargeAttack test with Van about 60 world units away:
  the Knight traveled approximately **36** world units and stopped
  around **25** world units away, rather than instantly traversing
  the entire distance.
- CloseAttack2 test with Van about 70 world units away:
  approximately **46** units traveled, with roughly **25** units
  remaining. Both attacks completed without a positional teleport.
- Scene saved and checked in Edit Mode. Final serialized values
  confirmed in the scene YAML.
- `git diff --check` passed for the modified
  `HollowKnightController.cs`.
- The local game-save file was byte-for-byte unchanged after QA.

### QA limits

This was a controlled Editor Play Mode positioning and charge test.
A complete player-controlled boss fight, multiple FPS targets, Android
hardware and visual feel checks remain outstanding. During testing,
other existing gameplay/UI behavior sometimes set
`Time.timeScale = 0`; this paused recovery. It was resumed for the
test run only and is not an authored change to the project.

Original scene, controller script and pre-test local save are backed up
under `Temp/HollowKnightChargePacingFix_20261124/`.

Unlicensed third-party asset packs remained untouched and untracked.
