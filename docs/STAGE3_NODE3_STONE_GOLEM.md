# Stage 3 Node 3 Stone Golem — 2026-10-05

## Layered Being Hit — 2026-10-06

This extends the existing production enemy. The Base Layer still contains only
Heavy Walk, Stop and Ground Slam. No additional attack, stagger or death system
was added.

### Animation audit

Both downloads match all 33 production Mixamo bone names/paths and rest matrices
exactly (maximum rest-matrix difference 0). Both armature object transforms are
stationary. The existing valid Generic production Avatar is reused.

| Source | Native length | Source movement / affected regions | Production use |
| --- | ---: | --- | --- |
| Step Backward | 25/30 = 0.833333 s | Hips travel 0.728972 m backward; vertical hip range 0.134579 m; pronounced leg/foot motion, smaller torso/arm motion. End pose matches the start after subtracting travel. | One-shot lower override layer; Hips and both leg chains only. |
| Head Hit | 66/30 = 2.2 s | No net horizontal travel. Hips also sway/rotate (up to 56 degrees), legs move, head recoils up to 76 degrees and arms contribute strongly. End pose returns to start. | One-shot upper override layer; Spine through head and both shoulder/arm/hand chains. Hips and legs excluded. |

The downloaded takes are imported with looping disabled. Matching Head Hit
endpoints do not make this reaction a permanent loop. Both production masks
exclude the ModelRig/root transform; no Humanoid retargeting or new Avatar is used.
The original BLEND and production rig FBX are unchanged.

### Ownership and movement

EnemyHealth's existing single reflection dispatch invokes
StoneGolemController.TriggerBeingHit(). There is no health polling and no duplicate
HealthChanged subscription. StoneGolemHitReaction owns the two layer plays,
authored retreat curve, fades and bounded finish watchdog; the combat controller
calls its Tick before normal AI.

A walking hit enters the existing Stop state while both layers play. Stop and
Recovery hits preserve their base state and existing recovery/cooldown clocks.
Ground Slam suppresses both hit layers and retreat, while EnemyHealth still
receives damage. It never cancels the committed jump.

The extracted lower clip flattens only the audited backward Hips position Z
curve (delta -0.728972). Hip bob/rotation and leg articulation remain. The same
authored translation profile drives NavMeshAgent.Move, with root motion disabled.
Initial retreat is 1.003248 world metres and is intentionally independent of the
Golem root scale. This prevents the saved 10.24 scene-scale override from turning
one authored step into an approximately 10 metre launch. Direction is captured
from the Golem's facing once per hit.
Each move is limited by the current NavMesh boundary and solid capsule sweeps.
There is no Warp, catch-up teleport, player homing or hit-time transform assignment.
At a boundary/obstacle the retreat can be shorter than a full authored stride.

Both layers use explicit Play at time zero for every hit, including repeated
hits. Finish acceptance ignores events from the old evaluation immediately after
a restart. Normal completion uses events; it does not inspect stale normalized
time to decide whether the new reaction has already ended.

| Relay event | Clip time | Result |
| --- | ---: | --- |
| OnLowerBodyHitFinished | 0.833233 s | Fade the lower layer to zero. |
| OnUpperBodyHitFinished | 2.166667 s | Fade the upper layer to zero. |

Layer defaults are zero, blending mode Override, and each layer has Empty plus
its authored reaction state with no auto-exit transitions. Both layers reset to
Empty after completion. Initial fade-in/out values are 0.06/0.12 s. Missing events
clear the reaction after the longest clip plus 0.4 s padding and fade duration
(about 2.72 s), then release normal combat. Disable/dead-player/dead-enemy/lost
NavMesh binding also clear the hit layers.

### Required spawn correction

The live scene contained a preserved 10.24 root-scale override and Y = 0.21.
Runtime inspection proved that the low spawn bound the agent to the lower
Y ~= 4.82 surface. Only the scene spawn height was corrected to the connected
Node 3 upper surface, Y = 7.2999196. X/Z (-673.0169, -534.2211), facing, size,
parent offset, trigger references and the existing bake were preserved.
The visible Align Node 3 Spawn Surface menu provides an undoable repeat.

The spawn remains near an upper-surface boundary, so early retreats can be
limited there. The full-stride test first lets the actual Heavy Walk enter open
arena space; no Golem teleport is used to prepare that test.

### Files and Inspector wiring

Production changes are StoneGolemHitReaction.cs, the small controller/relay
extension, StoneGolemHitSetupMenu.cs, two copied Mixamo sources, two one-shot .anim
clips, StoneGolemLowerHit.mask, StoneGolemUpperHit.mask, two Animator layers and the
existing prefab. The scene's required height correction is saved.
Animator, agent, collider, both clips, controller link and retreat curve are
assigned in the prefab; no Inspector assignment remains outstanding.
The three baseline animation files, including Ground Slam events, retain their
pre-task SHA256 values. The protected BLEND retains
EB0A7FB2316170809BC6FA5C7DC463789631BB4CC2317CB8E661D93924D1E1BC.

### Validation evidence and limits

Two controlled Unity 6000.3.10f1 Stage3Scene PC Editor runs passed. Each starts
through the real Node 3 trigger after unlocking only preceding nodes at runtime.
Play Mode exit restores the saved progression state.

| Check | Prefab-scale run | Saved 10.24-scale run |
| --- | --- | --- |
| Observed frames / persistent base-state mismatches | 1,909 / 0 | 2,267 / 0 |
| First retreat | 1.0033 m, full authored stride | Pre-fix evidence: 9.1497 m; requested 10.2733 m shortened at the NavMesh boundary |
| Maximum agent/root offset | 0.0000 m | 0.0000 m |
| Maximum horizontal Hips/root offset | 0.1750 m | 1.7924 m (same proportional offset at scene scale) |
| Maximum observed movement frame step | 0.3987 m | 1.8793 m at scene scale; continuous profile movement, no Warp |
| Exact pose ownership | Step leg rotations and Head Hit head/Spine2 rotations: 0 degree difference from their source samples | Same 0 degree difference |
| Reactions / landing impacts / player hits | 7 / 1 / 1 | 7 / 1 / 1 |

Both passes verify pre-battle gating, simultaneous layers, walking-hit recovery,
restarts after lower completion and near upper completion, Stop/Recovery hits,
zero weights after completion, missing-event watchdog release, NavMesh retention,
committed facing, no pre-landing damage, exactly one 15 HP landing hit, preserved
cooldown and Ground Slam finish-to-Recovery. Node 3 stays incomplete and Node 4
locked. Rendered layered poses were inspected. The temporary validator and meta
were removed after the passes.

After that validation exposed the giant-scale retreat, runtime retreat scaling was
corrected: `StoneGolemHitReaction` now treats the serialized 1.003248 value as
world-space metres instead of multiplying it by `transform.lossyScale`. The saved
scene scale, masks, clips, layer timing, NavMesh movement and Ground Slam behavior
remain unchanged. The 10.24-scale retreat number above is retained only as evidence
of the pre-fix behavior that motivated the correction.

The scale-one run uses a runtime-only size fixture and natural Stop/Ground Slam
range transitions. At the saved giant scene size, explicit existing Stop/Slam
entries and a temporarily non-moving player collider fixture isolate the hit
interaction and actual landing event. That run is not a sign-off of ordinary
giant-scale attack reach/collider balance. Those existing settings were preserved.
The fault-injection test uses a runtime-only clip/controller override; restoring
that test controller requires reapplying the current base state as fixture cleanup.
It is not part of production AI. Evidence is retained under ignored Temp/S3N3Audit
in unity_hit_validation_prefab_scale.txt and unity_hit_validation_scene_scale.txt.

Manual repeat: play Stage3Scene, finish Nodes 1/2, enter Node 3 and hit the Golem
with the existing damage path. Watch the two layers together, backward root and
collider movement, subsequent zero weights and normal combat resume. Repeat hits
during Stop/Recovery and during Ground Slam: slam hits must not cancel the attack
or produce an early/duplicate landing impact. A selected Golem still exposes the
existing impact/range Gizmos. Physical Android validation has not been performed.


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
