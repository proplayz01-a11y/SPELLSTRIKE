# Titan V017 solo arena: animation + procedural integration QA

**Date:** 2026-10-14  
**Scope:** Isolated `Titan_V017_SoloTestArena` Play Mode only. No production boss combat, damage, Stage3 scene edits, animation re-keying, or changes to Head/Chest tuning.  
**Decision:** Freeze the existing Head/Chest procedural attention as-is pending any future request. Evaluate existing authored animation clips and their QA-only Animation Rigging interactions instead. **Do not override earlier user visual FAIL assessments with structural tests.**

## Verified scene conditions

- Unity isolated Play loaded **exactly one scene**, the solo arena. Original Van movement and cloned Stage3 Main Camera operate, Titan boss AI remains disabled, Animator + both RigBuilder layers are active, and the scene-only gaze logger is installed.
- The experimental arena Animator Controller remains `Titan_TestArena/TitanV017_WriteDefaultsON_StompProof.controller`, which references the **original eight animation clips**. Only its V017 state uses the experimental `Write Defaults = ON` correction to preserve procedural foot constraint weights. Do **not** silently promote this test controller to production.
- SHA comparison reconfirmed `Assets/Scenes/Stage3Scene/Stage3Scene.unity` matches `Temp/TitanSoloPlaySafety/Stage3Scene_PreTitanSoloPlay_20261013.unity` and approved `Titan_ColossalStompFourRight_V017_CrushReview.anim` matches hash `8F19397D906A0E8437130AA43188A6B14E026255DAF013A695F4E45593B5713B`.

## State-by-state validation

Validated eight original clips by playing the states in the isolated Animator, sampling eight time points across each one, measuring original skinned-rig limb/hip/spine motion and returning to AwakenedHold. Also exercised the actual live QA selector and checked procedural aim suppression in frozen mid-attack/death preview poses. These checks establish **clip availability, changing evaluated poses and procedural interaction**, not cinematic quality or acceptable physical foot support throughout the entire motion.

| Original Animator state | Runtime / procedural result | Acceptance |
| --- | --- | --- |
| `DormantStatueV004` | State resolves; one-frame static statue pose; head/chest aim 0 / 0 | Functional PASS; visual QA still user-owned |
| `AwakeningV004` | Source 9.933s one-shot loads and body/hand poses change across time; aim 0 / 0; authored return transition to Hold exists | Functional PASS; visual smoothness not independently certified |
| `AwakenedHoldV004` | Static pose sampled consistently; idle gaze 0.780 neck / 0.220 chest when Van is valid | Functional PASS; gaze naturalness **FAIL by user** |
| `ColossalHeavyWalk` | Source 3.933s looping clip changes left/right foot world positions, hand poses and spine rotation; with `Walking=true`, live Animator remained in Walk across ~4.9 loops; gaze weights 0.580 neck / 0.130 chest | Functional PASS after scene-only selector fix; visual foot-slide QA open |
| `HammerfistV003` | Source 8.333s one-shot has changing arm/hip poses; while attack selected, neck/chest aim weights reached 0 / 0. Authored return to Hold observed in Play | Functional PASS; visual QA open |
| `EarthbreakerSlamV003` | Source 6.967s one-shot has substantial changing arm/hip poses; at frozen 45% attack pose, neck/chest aim weights 0 / 0 while both RigBuilder layers stayed enabled; Game View mid-pose screenshot reviewed | Functional PASS; visual QA open |
| `TitanDeathFromRightPreview` | Source 3.300s one-shot has changing limbs/hip; at frozen 53% state neck/chest weights 0 / 0 | Functional PASS as **preview**, not production death integration; visual QA open |
| `ColossalStompRightFootV017` | Source 8.767s original clip samples changing poses; four procedurally constrained contacts **not repeatable** | **FAIL** strict repeatability gate |

**QA selector bug fixed (arena-only):** `TitanV017ArenaAnimationMenuQA.PlayExistingState` previously called `Animator.Play("ColossalHeavyWalk")` without setting the actual `Walking` bool, so the authored `Walk -> Hold` transition (`Walking=false`) immediately returned to idle. It now sets `Walking=true` only for selected Heavy Walk, and false for the other states. Confirmed the menu-driven walk stayed active across multiple loops, with proper procedural walking attention weights. Production Animator Controller and all animation clips remain untouched. Note that this QA menu still jumps directly to requested states; it is **not proof of naturally blended production transitions**.

## Three consecutive real procedural V017 replay results (unchanged safety limits)

| Run | Real contacts verified | Outcome |
| --- | ---: | --- |
| 1 | 2/4 | **ABORT**: engaged right IK reach `30.309m > 28.618m` at clip `3.2112s`, weight `0.085` |
| 2 | 4/4 | **PASS**: completed original 8.767s clip; all four real deformed-mesh right-foot contacts and left support checks passed, no damage/CC |
| 3 | 1/4 | **ABORT**: engaged right IK reach `29.083m > 28.618m` at clip `1.8841s`, weight `0.106` |

**Aggregate:** **1 of 3 complete replays passed.** This reproduces the prior observation that a one-off 4/4 PASS is not sufficient. Do not lift or bypass the strict `94%` anatomical reach cap and do not equate original animation playback with accepted procedural stomp behavior. The left support was objectively grounded during the successful 4/4 replay's sampled contact windows; full visual biomechanics and between-contact stability remain unapproved.

## Retained failures and next optional work

1. **Head/Chest Aim is frozen as-is.** The scene-only logger proves bone rotations actually happen, but chest contribution remains small and the user's original visual FAIL stands. No additional aim tuning made during this QA.
2. **V017 procedural IK is nondeterministic at the strict recovery reach limit.** Accept only after multiple consecutive full-length 4/4 replays, including different render frame rates, with no 94% violations, genuine deformed-foot/ground checks and no visible foot skating. No further corrective IK tuning authorized in this validation pass.
3. **Original clips all resolve and produce evaluated poses**, but production state transitions, face/neck aesthetic quality, full-clip left support balance and attack camera readability require subjective gameplay inspection; sampling and Console logs alone are insufficient.
4. **Natural walk-to-stance and collider separation remain not integrated.** Full gameplay validation must not enable Titan AI until physical clearance and stance feasibility are addressed.
5. The latest Unity Console still contains pre-existing unrelated project C# warnings and V017 safety abort logs, and a prior Android Unity Remote requirements error. These are not equivalent to zero-warning, zero-glitch production acceptance.

**Last known Unity state:** Isolated solo arena remains in Play, Animator restored to `AwakenedHoldV004`, speed `1`, `Walking=false`, original Van manually controllable, boss AI disabled, V017 preview stopped, scene-only aim logs active. No authored clip or Stage3 production scene altered.
