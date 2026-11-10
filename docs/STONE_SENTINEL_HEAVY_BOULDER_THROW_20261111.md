# Stone Sentinel — Heavy Tumbling Boulder Throw

## Ground bounce extension — 2026-11-11

The previous physically thrown rock was destroyed immediately on first
ground contact. It now visibly **bounces twice** on an upward-facing solid
ground collision, with decreasing energy, then briefly rolls/settles before
cleanup. A player hit still applies damage ONCE and destroys the rock without
bouncing through the player. Hitting a wall or ceiling also terminates the
throw. The same Rigidbody continues to simulate both rebounds; no scripted
teleport or homing is used.

Adjustable settings in BoulderProjectile: maxGroundBounces=2;
firstBounceRetention=0.38; secondBounceRetention=0.20;
horizontalImpactRetention=0.58; minimumGroundImpactSpeed=4;
settleTime=0.55 seconds. On the first two ground contacts the rock rebounds
along the actual surface normal, loses lateral velocity and 30% of its spin;
the rebound speed is capped at 24 world units/s to avoid a rubber-ball feel.
After the final impact, strong linear/angular drag produces a short settle.
The original five-second projectile safety lifetime still applies.

**Isolated Play Mode QA:** A real BoulderPrefab in its own local 3D PhysicsScene,
dropped onto a solid floor with the existing scaled gravity, produced two
separate ground rebounds at simulation times approximately 1.00s and 1.50s.
First vertical rebound velocity was **24.00 units/s** and the second only
**3.68 units/s**. In a separate local physics test, it hit an actual
PlayerHealth target, reduced HP **from 100 to 88 exactly once**, did not
bounce through the player, and marked the projectile terminal after impact.
Both tests left the active user SampleScene unchanged. Visual tuning in the
actual arena is still outstanding.

Implemented in Assets/Scripts/StoneSentinelsController.cs and
Assets/Scripts/BoulderProjectile.cs. The original
Assets/Animations/StoneSentinelAnim/BoulderThrow.anim events are unchanged:

1. OnBoulderPickup: show the held rock at 1.20 times its authored scale.
   Cache the initial scale once, avoiding accumulated size between attacks.
2. OnBoulderThrow: hide held rock; spawn the original BoulderPrefab at the
   animation hand release point, size it 1.20 times larger, and aim at the
   player's current body-collider center through a physics ballistic solution.
3. OnBoulderThrowFinished: preserve the original recover and cooldown.

## Physics settings

- boulderSpeedMultiplier = 1.25; retain scene-authored boulderSpeed
  (observed 101.9 world units/s in Stage 1), while allowing a minimum loft
  time at shorter distances to form a natural throwing arc.
- boulderGravityMultiplier = 9; Unity Rigidbody gravity is enabled and
  FixedUpdate applies scaled downward acceleration to suit the giant
  characters and world scale. The throw's initial velocity is solved for
  the same gravity and a single sampled target position: no mid-flight homing.
- The existing boulderArcAngle sets minimum loft/flight time.
- boulderTumbleSpeed = 17 radians/s, applied as Rigidbody angular velocity
  about a sideways throw axis. Max angular velocity is raised accordingly.
- ContinuousDynamic collision detection and Interpolate reduce missed
  high-speed contacts and visual jitter. The thrower's colliders are ignored.
- Existing IDamageable.ApplyDamage, ground impact and one-hit destruction
  logic are retained; no changes to Counterspell/FRACTURE phase gating.

All new feel settings are serialized in StoneSentinelsController and can be
adjusted in the Inspector. No scene or imported prefab was overwritten.

## Verification

Tools/Test-StoneSentinelBoulderThrow.ps1 passed 31 source/ballistic assertions.
C# build: 0 errors; 34 existing warnings.

To avoid interrupting the player's running session, the real prefab was
tested in an independent, unsaved Unity local 3D physics scene:

- Thrown rock root scale 4.58, up from 3.82 (20% larger).
- Initial 60-unit test throw velocity: 97.4 world units/s (minimum loft).
- Rigidbody gravity enabled; angular velocity 17.0 radians/s.
- Self-collision with Stone Sentinel correctly ignored.
- At 0.20 seconds: 20.51 world units traveled, 165.7 degrees of tumbling.
- One physical contact by simulation step 24 (~0.48 seconds), near target.
- The user's SampleScene remained the active scene throughout the test.

Actual Stage 1 encounter-camera tuning and Android performance QA remain
pending; the isolated test verifies physics behavior but not final visuals.
