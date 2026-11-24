# SPELLSTRIKE — Hollow Knight: Relentless Knight (Motion Design Lock)

Date: 2026-11-24. User-selected **C — Relentless Knight**.
**Status: FINALIZED MOTION SPEC, NOT IMPLEMENTED ANIMATION SWAPS.**
Keep existing Unity boss code, charge anti-teleport fix, scene, damage
events, third-party packs and gameplay save unchanged during production.

## Personality and look

**A calculating executioner with sudden bursts of aggression.**
Heavy steps, lowered sword, patient stillness, planted stance, dramatic
but readable wind-ups. Fast strikes come in short bursts, followed by
visibly weighty recovery. NO teleporting, foot skating, ballerina
spins, uncontrolled root motion, or exaggerated clownish taunts.
The sword remains attached to his existing right-hand weapon grip.

## Locked motion pack and current Animator mapping

The actual Unity Base Layer currently has exactly EIGHT states.
Each target retargeted mocap clip must preserve the state name and
event callbacks below. Target durations are seconds at normal speed.

| Unity state | New clip name / main motion | Length | Required existing animation events |
|---|---|---:|---|
| Idle | HK_MOCAP_HeavyGuardIdle — chest breathing, subtle helmet movement, sword low | 5.0 loop | none |
| Walk | HK_MOCAP_ArmoredWalk — planted, deliberate heavy footfalls | 1.367 loop | none |
| Run | HK_MOCAP_RelentlessRun — powerful lowered-stance burst | 0.700 loop | none |
| CloseAttack | HK_MOCAP_ThreeHitExecution — horizontal, reverse diagonal, overhead strike | 3.533 | sword ON/OFF at 0.83/0.93, 1.73/1.80, 2.63/2.70; OnSwordSlashFinished 3.50 |
| ChargeAttack | HK_MOCAP_RushCleave — crouch, dash, wide cleave, planted brake | 1.933 | sword ON 0.93, OFF 1.30; OnChargeAttackFinished 1.90 |
| CloseAttack2 | HK_MOCAP_RushThrust — crouch, driving thrust, brake | 2.100 | sword ON 0.90, OFF 1.03; OnChargeAttack2Finished 2.07 |
| BeingHit | HK_MOCAP_ArmoredStagger — recoil, support step, guard recovery | 1.733 | OnBeingHitFinished 1.70 |
| Dead | HK_MOCAP_FinalKneel — resists, kneels, drops sword, collapses | 3.567 | DropSword 3.33; OnDeathFinished 3.567 |

Additional CINEMATIC-ONLY mocap:
**HK_MOCAP_ColdMockingTalk** (4–5 s; restrained head tilt and
dismissive sword gesture). Assign to the existing optional
`hollowKnightTalkingMocap` slot in
`Stage1HollowKnightConfrontation.cs`. Van's talking mocap is a
SEPARATE character clip / final-polish task.

## Time-coded choreography

### Idle, 5.0 s (loop)
0–1 planted pose, blade held low; 1–2.8 controlled chest breathing;
2.8–3.8 slight helmet turn toward Van; 3.8–5 returns to the exact
original pose. First/last key values MATCH for all animated bones;
feet and root remain still.

### Walk, 1.367 s (loop)
0–0.35 planted right foot, left step; 0.35–0.70 weight settles;
0.70–1.03 planted left foot, right step; 1.03–1.367 back to same
contact phase. No ice-skating or sword clipping.

### Run, 0.700 s (loop)
0–0.17 planted drive; 0.17–0.35 first powerful stride; 0.35–0.53
second heavy stride; 0.53–0.70 clean return to gait contact.
Tune actual gait against scene movement caps (walk 24, run 42
world units/s). Do not mask bad scaling with arbitrary foot speed.

### Three-Hit Execution, 3.533 s (one-shot)
0–0.65 draw sword back; 0.65–0.96 horizontal slash, **hit 0.83–0.93**;
0.96–1.55 replant and reverse wind-up; 1.55–1.86 diagonal slash,
**hit 1.73–1.80**; 1.86–2.43 two-handed overhead wind-up;
2.43–2.76 execution strike, **hit 2.63–2.70**;
2.76–3.533 heavy recovery. Three *distinct* contacts; no lingering
active hitboxes between them.

### Rush Cleave, 1.933 s (one-shot)
0–0.23 readable telegraph, crouch, NO travel; 0.23–0.39
acceleration; 0.39–1.11 forward burst and wide cleave (contact begins
0.93); 1.11–1.33 braking / cut ends by 1.30; 1.33–1.933 recover.
The CODE owns movement: 0.23 s wind-up, 1.10 s movement,
0.16 acceleration, 0.22 braking, 24-unit stopping margin.
Root motion OFF, direction locked on launch.

### Rush Thrust, 2.100 s (one-shot)
0–0.23 low guard; 0.23–0.39 explosive launch; 0.39–0.90 drive
forward; **0.90–1.03 thrust contact**; 1.03–1.33 blade retraction
and braking; 1.33–2.10 grounded recovery. Visually DISTINCT from
the wide cleave; same controller-driven travel and dodge fairness.

### Armored Stagger, 1.733 s (one-shot)
0–0.26 recoil; 0.26–0.85 grounded support step; 0.85–1.45 regain
grip/guard; 1.45–1.733 settle. No physics ragdoll or teleport.

### Final Kneel, 3.567 s (one-shot)
0–0.60 final blow resistance; 0.60–1.50 falter;
1.50–2.70 one-knee collapse; 2.70–3.33 weaken/grip release;
**3.33 sword drops**; 3.33–3.567 final settling.
Do not double-spawn fragment rewards; preserve boss defeat callback.

### Cold Mocking Talk, 4–5 s (cinematic)
0–1 stillness and slight helmet tilt; 1–2.2 restrained pointing/
glove gesture; 2.2–3.6 dismissive gaze; 3.6–4.5 return to pose.
Conveys quiet cruelty during the post-LIBERATE confrontation.
No fighting or charge movement during dialogue.

## LLM Mixamo Mocap source-video standard

Each motion gets ONE clean mocap reference plate: locked full-body
camera, 1080p preferred (720p minimum), 24–30 fps, about 10 seconds,
same actor/studio across clips, unobstructed limbs, neutral clothes,
NO giant physical sword blocking tracking. Start AND finish with about
1 second of visible frontal T-pose. Perform only the requested move
between the bookends. Inspect actual frames: generated-video prompts
are not evidence the desired foot/hand performed the move.

Reusable source prompt:

> Same single adult performer, neutral tight clothing, plain evenly lit
> studio, visible floor grid, static camera showing every limb and foot.
> Hold a correct frontal T-pose for the first second. Perform exactly
> the specified heavily armored swordsman's choreography (mime the
> weapon without a prop), with deliberate grounded weight transfer,
> readable anticipation, complete foot visibility and no extra actions.
> Return to and hold a perfect frontal T-pose for the last second.
> One take, no cuts, camera moves, VFX, or hidden limbs.

For each clip create a beat checklist from the timing sheet above,
then process footage with GVHMR, numeric landmarks and the
per-motion `action_specs` workflow. Use source beats and foot-support
measurements rather than assigning limbs from a visual guess.
Crop/retime the retargeted animation to the Unity target length,
add the verified Unity Animation Events, and QA frame-by-frame.

## Known production blockers (NOT waived)

Current Hollow Knight source FBX was measured at ~0.381 m hip height
and **rejected** by the mocap rig setup validator. Obtain a correctly
scaled neutral rest-pose FBX / rig profile first. Previous local
environment: Blender 5.0 and RTX 4050 6 GB VRAM; full pipeline has
not passed on the machine. Test a tiny plate before batch capture.
Keep working assets local; do NOT redistribute unauthorized third-party
packages through the public GitHub repository.

## Production order and release gates

1. Rig prep/rest pose/scale, single-plate proof of retargeting.
2. Heavy Idle, Walk, Run — verify stationary loops and gait/stride.
3. Rush Cleave, then Rush Thrust — preserve charge movement, damage
   windows, root-motion OFF and no teleport/sliding at 30/60/120 FPS.
4. Three-Hit Execution — exactly three separately enabled sword windows.
5. BeingHit and Dead — correct recovery, sword drops once, defeat
   callback once.
6. Cold Mocking Talk — hook into existing cinematic; restore combat
   animator after dialogue. Then Van's own talking mocap separately.
7. Full Stage 1 PC/Android combat QA with restored save data.

**Approval:** The visual personality, choreographic beats, clip inventory,
production priority, and original animation-event timings are LOCKED.
**Not implemented yet:** new mocap video plates, imported FBX clips,
retargeted combat controller, mocap talking, or hardware QA.
