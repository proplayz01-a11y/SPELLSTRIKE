# Stage 3 Node 3 Stone Golem — 2026-10-05

## Scope

This layer contains Heavy Walk, a stopped/recovery stance, and Jump as Ground
Slam. It introduces no other attack, stagger, death animation, or progression
callback. Production assets live under
`Assets/Stage 3/Enemies/STAGE 3 ENEMY NO. 3/`; the old Stage 1 Stone Sentinel
controller and Animator remain reference assets.

The protected source
`ArtSource/Characters/StoneGolem/VolumetricStone_v001/StoneGolem_ActualStone_v002.blend`
was not edited. SHA256 before implementation:
`EB0A7FB2316170809BC6FA5C7DC463789631BB4CC2317CB8E661D93924D1E1BC`.
VALORANT was absent during the audit and authoring checks. Existing dirty
Blender sessions were left open.

## Download audit and clip mapping

The upload FBX is the clean unrigged geometry. The downloaded Mixamo animation
files contain the new rigged Golem. All inspected downloads share 33 Mixamo bones,
28 weighted vertex groups, identical rest matrices, no unweighted vertices,
79,878 triangles and the original nine material slots.

| Download suffix | Production clip/state | Length | Evidence/use |
| --- | --- | ---: | --- |
| `@Mutant Walking.fbx` | `HeavyWalk.anim` / Heavy Walk | 2.166667 s | In-place heavy walk; looping enabled. |
| `@Mutant Idle.fbx` | `Stop_StationaryLoop.anim` / Stop | 5.3 s | Exact matching endpoints; available stationary/recovery loop. |
| `@Mutant Jumping.fbx` | `GroundSlam_Jump.anim` / Ground Slam | 3.8 s | In-place jump with vertical skinned-bone motion. |
| `@Step Backward.fbx` | Not imported into production | 0.833333 s | Travels about 0.729 m backward; unsuitable as a braking Stop. |

No separately named Stop/braking clip was found. The available Mutant Idle is
used transparently as a stationary loop; this does not claim a one-shot braking
animation. Punch and Head Hit downloads are outside this layer and were not added.

The source axes, mesh and skeleton agree across these files. A valid Generic
Avatar is created from `Model/StoneGolem_Rig.fbx`, and animation sources copy that
Avatar. This preserves the exact existing Mixamo skeleton without unnecessary
Humanoid retargeting. Root rotation, XZ and height are baked into the clips;
`Animator.applyRootMotion` is false. The Jump's skinned motion supplies its airtime,
so the grounded NavMesh root requires no scripted leap or landing translation.

## Authored timing

Frame-by-frame evaluated mesh contact establishes the first fully planted frame
as source frame 69, or Unity zero-based frame 68. The preceding frame remains
about 0.028 m above the source floor; contact is about -0.004 m. Apex clearance is
about 0.634 m at source scale.

| Clip event | Time | Receiver |
| --- | ---: | --- |
| `OnStopSettled` | 0.333333 s | Ready the stopped/recovery stance. |
| `OnGroundSlamImpact` | 2.266667 s | Apply at most one AoE hit through existing `PlayerHealth.TakeDamage`. |
| `OnGroundSlamFinished` | 3.766667 s | Enter Stop recovery. |

`StoneGolemAnimationRelay` lives on the same ModelRig object as the Animator.
The controller owns explicit crossfades among the three Animator states. There
are no automatic exit-time transitions. A missing finish event only recovers
safely and reports an error; it never synthesizes damage.

## Scene and combat integration

`Stage3Scene` retains the existing Node 3 trigger, `nodeIndex=3`, progression
manager, arena footprint and baked `NavMesh-STAGE3MAP` data. The trigger now points
to `S3N3_StoneGolem` and its `StoneGolemController.StartBattle` receiver. The prefab
is under `_Enemies_CHARACTERS` with root scale one and an independently scaled
ModelRig. Spawn is on a connected path near Node 3. The pre-existing live scene
root offset was preserved.

Stage 3's runtime NavMesh bootstrap may enable and park an agent before combat.
That does not start the battle: the Golem's state remains Inactive, combat collider
disabled and tag Untagged until the actual Node 3 encounter sends StartBattle.
The controller clears any path and parks the agent each inactive frame. Ground
alignment uses evaluated animated contact, rather than lower rest-pose renderer
bounds: ModelRig local Y is 0.0064684 at the production scale.

Combat approaches through `EnemyNavMovement`, enters Stop inside attack range,
faces the player before committing, then plays Ground Slam without following the
player during the jump. Approach range and landing AoE use the existing player
collider surface. This matters because the live player's root scale is 10 and its
CharacterController is approximately 10 m wide: centre-based range caused a
physical push/chase that never reached the attack threshold. The player's rig and
colliders were not changed. Stop recovery precedes the next walk or attack.

Initial Inspector values: damage 15, radius 3 m, cooldown 4 s after impact,
attack range 2.7 m, minimum recovery 0.75 s, walk speed 1.196925 m/s and acceleration
time 0.75 s. The visual scale is 1.376251; the speed derives from the audited
planted-foot source speed of 0.8697 m/s. WalkRate follows actual agent velocity.
Selected Gizmos display impact radius and attack range. Shared `EnemyHealth` is
200; reaching zero uses its existing destruction fallback, without completing
Node 3. Node 3 `completeNodeOnEnemyDefeat` is false and there is no Golem defeat
callback. Node 4 remains locked.

## Validation status

The final controlled Unity 6000.3.10f1 Stage3Scene Editor Play Mode run passed
both attack cycles through the real Player collider / Node 3 OnTriggerStay path.
Only preceding node flags were unlocked for the runtime test; Play Mode exit
restored the authored progression state. The temporary validation script and its
meta were removed after the pass. Fresh compilation after cleanup passed
(Unity Tundra build success, 5.28 seconds, no new C# errors). The final live
Edit Mode audit reports a clean saved scene, the correct Node 3 receiver/root,
unchanged existing NavMesh registration and Node 3/4 progression still locked.
The production prefab and scene both save ModelRig local Y as 0.006468377.

| Check | Measured final result |
| --- | --- |
| Before battle / locked Node 3 | Inactive, zero velocity, no path, combat collider disabled; no attack or movement. |
| Actual trigger after preceding nodes unlock | StartBattle reached; Heavy Walk travelled more than 1 m. |
| Speed and pose | Agent reached 1.196925 m/s; WalkRate follows velocity; rendered walking, jump and landing captures reviewed. |
| Before the landing event | No HP change or impact increment throughout observed windup/airtime. |
| In-range landing | Exactly one impact and one 15 HP hit through PlayerHealth. |
| Animated grounding | Stationary mesh bottom 0.0000 m; airborne 0.7884 m at the captured sample; landing 0.0006 m relative to agent ground. |
| Jump movement | Grounded agent root did not move/teleport; committed facing did not change. |
| Finish / recovery | Authored finish event entered recovery, then returned to moving Heavy Walk. |
| Player leaves the radius mid-jump | One landing impact, zero additional damage, no homing/rotation. |
| Animator / C# agreement | 1,379 observed frames; zero persistent synchronization errors. Finish-event crossfade is evaluated on the next frame, then blends normally. |
| NavMesh / progression | Remained on NavMesh; maximum observed movement frame step 0.2878 m; Node 3 incomplete and Node 4 locked. |

Audit and temporary validation evidence is under ignored `Temp/S3N3Audit/`.
The visible `SPELLSTRIKE/Stone Golem/Audit Node 3` menu can repeat the wiring and
NavMesh inspection. The setup menu refuses to replace an existing tuned instance.
`Align Authored Clip Grounding` preserves the root and updates only ModelRig's
animated floor offset in the scene and production prefab.

For a manual repeat, play Stage3Scene, finish Nodes 1 and 2, enter Node 3, and
watch Heavy Walk -> Stop -> Ground Slam -> landing -> Stop recovery. Observe
the controller counters/Animator and Player HP: impact/hit counters and damage
must change at landing, not takeoff. Move outside the selected impact Gizmo during
the jump to confirm the committed attack misses. Node 4 must remain locked.

Physical Android build/device validation has not been performed. Mixamo imports
report four discarded self-intersecting polygons; the protected source is not
modified to repair them. The new URP Lit materials preserve available diffuse
colors, without claiming a final procedural-material texture bake or mobile
geometry budget.
