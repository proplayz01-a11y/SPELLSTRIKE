# Stage 1 — Cursed Jester Approach / Combat-AI Audit (2026-11-11)

## Observed causes in the running encounter

1. Broken navigation route, even though combat had begun. The real
   Cursed Jester in SampleScene had a valid player target about 59.5
   world units away, battleStarted=true, no hit/recovery state initially,
   an enabled NavMeshAgent on navmesh with speed 25.2, but zero velocity.
   NavMesh.CalculatePath reported PathPartial with no useful route.
   The Jester stood on an isolated piece of the baked navigation mesh
   near a Stone Platform. Ground raycasts found continuous solid Plane.003
   between Jester and player, so a physical corridor exists even though
   the NavMesh graph does not connect it.
2. Ranged ability camping. Authored scene ranges were approximately
   Taunt 44.2 units, ConfettiBlast 42.3 units, BasicAttack 8.95 units.
   The old DecideAction prioritized Taunt/Confetti as soon as the enemy
   entered their outer range, stopping most approach movement.
3. Combat state could stick after a missed animation event. During
   the audit, isAttacking=true while the Animator was playing a looping
   Walk state. The original late events OnTauntFinished,
   OnConfettiBlastFinished, and OnBasicAttackFinished exist in the clips,
   but interrupted/skipped events could strand the isAttacking flag.
   The BeingHit flag had the same hazard.
4. Current gameplay was paused for a separate reason. During final QA,
   Time.timeScale=0 with StageCompleteCanvas active; NULLIFY/FRACTURE
   flows showed Completed and Teach panels were hidden. The Jester
   controller must not override that global pause. QA instead used
   explicitly supplied simulation timesteps.

## Source changes

Modified: Assets/Characters/CursedJester/CursedJesterController.cs

- ChasePlayer throttles path planning at 0.4-second intervals, uses
  NavMeshAgent.SetPath only for PathComplete routes, and avoids resetting
  the same incomplete SetDestination every rendered frame.
- If the mesh is disconnected, a ground-steering fallback temporarily
  disables the stuck NavMeshAgent, advances toward the player at the
  authored walkSpeed/acceleration, checks ahead with SphereCast for
  solid walls, and validates ground with a downward ray before moving.
  It refuses ungrounded gaps and large drops or steps.
- The fallback periodically searches for a complete route using the
  original agent type/area mask and restores the NavMeshAgent near the
  current position when the navigation mesh reconnects.
- The Jester approaches closer midrange before choosing ranged abilities
  (rangedApproachFraction=0.45), rather than camping at long range.
- Attack (4 s) and BeingHit (3 s) animation-finish watchdogs restore
  pursuit if a near-end event is skipped; stale finish callbacks are ignored.
- NavMeshPath is created during Awake and lazily after hot reload, never
  in a MonoBehaviour instance field initializer.
- NULLIFY prerequisites, combat damage, ability events, actor stats,
  scene geometry and baked NavMesh assets are unchanged.

## Verification

Tools/Test-CursedJesterApproach.ps1 passes 30 source/animation checks.
Assembly-CSharp builds with 0 errors (34 existing warnings). Runtime
QA used a temporary Jester, with no scene/prefab writes:

- Original actor route to player: PathPartial, agent velocity zero.
- Controlled 45 grounded movement steps of 0.02 s: Jester moved 16.65
  world units; player distance shrank from 59.47 to 42.83, feet stayed
  on Plane.003. Global StageComplete Time.timeScale stayed 0.
- Temporary solid blocking wall: motor stopped after 6.31 units
  before crossing the collider.
- No walkable floor: motor refused to advance into empty space.
- Expired attack timer: watchdog cleared isAttacking and entered recovery.
- From a connected area near the player: fallback recovered the normal
  NavMeshAgent, PathComplete, agent enabled and fallback disabled.
- Temporary QA objects were destroyed afterward. No scene save, prefab
  overwrite, commit or git push.

Recommended authoring work: eventually rebake/reconnect the Stage 1
NavMesh around the Stone Platform and Plane.003. The fallback removes
the immediate softlock without making invasive scene/NavMesh changes.
