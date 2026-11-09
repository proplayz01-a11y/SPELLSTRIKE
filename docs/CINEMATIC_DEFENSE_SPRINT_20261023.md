# SPELLSTRIKE — cinematic defense sprint (October 23–28, 2026)

**Direction:** Ship a complete, watchable, narration-led rough cut before spending more time on cosmetic polish. Preserve all 42 story beats but combine adjacent beats into shared camera setups. Stage 5 remains a parallel must-pass deliverable. Do not claim the sequence is animated/implemented when only blocked or documented.

## Current confirmed inputs

- Panel 01 / Home, Panels 02–03 / Great War, Panel 04 / ruined home: real Blender-exported, Unity-imported environment scenes at `Assets/Scenes/Cinematics/Prologue_P01_FamilyHome_Defense_v01.unity`, `Prologue_P02_P03_GreatWar_Defense_v01.unity` and `Prologue_P04_RuinedHome_Defense_v01.unity`.
- Two temporary Young Van / Nevali skinned 18-bone character assets and example clips exist under `Assets/Cinematics/Storyboard3D/Characters/DefensePrototypes/`. They are **temporary stand-ins**, not the final Rodin Nevali or approved Young Van models.
- Panel 01 place-and-save Editor action is implemented in `Assets/Editor/Cinematics/PrologueP01CharacterBlockingBuilder.cs` with a one-time backup guard. **Not executed in this sprint session:** Unity menu item unavailable because Editor reported GUI-handle failure and stayed in compilation; existing clean scene was not altered.
- StudyArchive and shot plan: `Assets/Scenes/Cinematics/StudyArchive_CinematicSet_v01.unity`; `docs/STORYBOARD_CINEMATIC_CAMERA_DIRECTION_LOCK_20261023.md`. Panels 15–18 already have a separate preliminary Unity Timeline; it is not complete.
- Do not touch gameplay assets or the original StoryScene, player prefabs, or Build Settings until a new full playback/handoff is proven.

## First testable vertical slice: Panel 01, 8–12 seconds

**Narrative beat:** Van Aert quietly watches his daughter Nevali play with wooden blocks at home, viewed through an interior doorway.

**Composition:** A01 doorway layered medium-wide, 35-mm-equivalent framing. Young Van on **left foreground**, Nevali **mid-right**, blocks in the lower-right area. Gentle ~0.4m dolly/push that does not cross or obscure Van's silhouette. Warm interior illumination, legible child expression and hands. Do not add the wife here.

**Blocking:** Place a non-speaking Young Van at the door; use subtle breathing/head movement only. Nevali shifts her right hand to a block, stacks it, releases, and returns to idle. One toy-block contact/release event must be visible. Use placeholder toy motion if custom Mixamo LLM Mocap is not ready. Her final sculpt can be swapped into the temporary actor socket later.

**Rough timing:** 0–2s establish the room and both actors; 2–6s Nevali reaches, lifts or places a block; 6–9s Van watches and subtly reacts; 9–11s hold on child, then crossfade/cut into Great War. Lock exact seconds to recorded VO/audio when available.

**Provisional VO for timing only:** “Before everything changed, Van Aert would watch his daughter play. For a moment, the world was peaceful.” Rewrite/record against the approved story canon before shipping. No final narration audio is claimed.

**Acceptance gate:** Unity Play Mode shows two actors at intended marks, at least one readable block interaction, one working shot camera, no black frame at exit, and an 8–12s recorded Game View clip. A real camera screenshot is necessary. Do not accept merely because two prefabs are in the hierarchy.

## Follow-on gate order

1. **Finish Panel 01:** place stand-ins safely, verify bone/animation compatibility, calibrate camera and toy contact, and record a rough Game View clip.
2. **Panels 02–04:** 2–3 establishing/reaction shots using the existing Unity environments, Young Van placeholder, separate casting tiles and controlled transition. Keep loss implied, not graphic. These can be narration-driven rather than choreography-heavy.
3. **StudyArchive:** Connect already-blocked Panels 15–18 with book inserts and reused shots; render one contiguous playable sequence. Use existing old Van/Vander and current study-library layout.
4. **Forest wake-up and handoff:** Use existing TutorialForest and established camera setups F01–F05; prove seamless handoff into actual gameplay controls.
5. **Critical-path completion:** Stage 5 end-to-end gameplay and Android/PC validation, plus prologue skip/replay and a defense-rehearsal build, outrank optional face/hair polish.

## Production rules

- Sol Ultra can handle Nevali face remodeling separately in versioned Blender source. Do not overwrite the current standalone face repair or silently swap a potentially unrigged sculpt into the Unity scene.
- Reuse the pre-existing animation prototypes and cameras instead of commissioning more assets for every storyboard beat.
- Separate **implemented**, **rough-cut**, **planned**, and **blocked** statuses in reports.
- Save and test after each small shot; have a viewable 16:9 sequence at every milestone. Do not block all cinematics on one character's facial detail.
- Work in deliberate sessions with breaks and at least a meaningful sleep window. All-night fatigue is not a production dependency.

## Current Unity blocker

At last check, the target P01 scene was loaded and clean, and both placeholder GLBs existed. The guarded Place Young Van and Nevali menu returned unavailable; the Builder type was not yet loaded and Unity reported it was compiling, while Editor logs repeatedly showed Win32 CreateWindowEx GUI-handle exhaustion. **No actor placement was claimed or forced.** The next safe attempt is after the current Editor settles/restarts cleanly under user control, checking its saved scene state and duplicate-placement backup guard first.
