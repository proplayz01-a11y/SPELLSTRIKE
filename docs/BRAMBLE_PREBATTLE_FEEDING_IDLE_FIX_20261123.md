# SPELLSTRIKE — Bramble pre-battle feeding idle

Fixed locally on 2026-11-23. No Git commit or public redistribution.

## Issue and cause

Bramble was visibly walking in place while dormant beside Nevali's fragment.
Unity Play Mode showed:

- `BrambleSpriteController.IsDormant == true`, battle not started
- NavMeshAgent velocity `(0, 0, 0)`
- Base Animator Layer default state `Approach` with `Walk` animation
- Magic Heal 28–42 playing only in a masked upper-body layer

The character was **not physically walking**; the full-body default
`Approach/Walk` was still moving his legs underneath the feeding gesture.

## Fix

- Added Bramble-only `Soul Feeding Idle` state to Base Layer of
  `Assets/Animations/SoulFeeding/Controllers/SampleScene_BrambleSprite_MagicHeal.controller`.
- The base state uses the **first frame** of
  `Bramble_MagicHeal_Seamless.anim` with `speed=0`, keeping Bramble's
  lower body stationary while his independent upper-body layer loops
  that 28–42 animation continuously.
- `Assets/Scripts/EnemyScripts/BrambleSpriteController.cs` now selects
  `Soul Feeding Idle` on `SetDormant(true)`, and explicitly blends back
  into the **original Approach state** when `StartBattle()` is called.
- Existing attack, being-hit, movement and death states/transitions
  remain intact; no other enemy Animator Controllers were modified.
- Installer for safe repeatability:
  `Assets/Editor/BramblePrebattleFeedingIdleInstaller.cs`.

## Test results

- Unity 6.3 script compile: **zero new errors**.
- Play Mode before battle: dormant true, no navigation velocity,
  Base Layer was `Soul Feeding Idle` at normalized time 0.000.
  Magic Heal upper layer was at full weight and had passed 24 loops.
  Bramble root position remained unchanged.
- On `BrambleSpriteController.StartBattleDisturbed()`:
  battle started, dormant became false, SoulFragmentFeeding stopped
  and Magic Heal layer weight dropped to 0.00.
  After the animation transition, Base Layer returned to
  `Approach`, preserving his walking/attack gameplay once combat starts.
- No legacy `ArenaTrigger` components were present in the active Stage 1
  scene; Node 1 retains its MITIGATE vocabulary gate.
- Editor left out of Play Mode.

## Backups

Original Bramble controller, script and Stage 1 scene backed up under
`Temp/BrambleFeedingIdleFix_20261123/`, along with the pretest
`spellstrike_save.json`. No unlicensed vendor packages were staged,
committed or pushed.

## Still to verify

Full user-controlled Spirit -> fragment reveal -> MITIGATE -> Bramble
combat playthrough, and Android device animation performance.
