# SPELLSTRIKE Minigames Phase 3: Gameplay QA + UI Redesign

## Goal
Validate all three independent session models in Unity Play Mode and on the
Android target, then upgrade the minigame screens to the approved fantasy
visual language of the main menu. Do not modify the locked Main Menu V11.

## Gate A: Behavior and navigation QA (before redesign)

1. Open Mode Select from Main Menu; verify all three destinations and return.
2. **Word Master:** initial hint, duplicate-letter clues, correct-position
   locking, invalid/short guesses, all five turns, win and loss, reset,
   keyboard colors, result overlay and leaving during an active round.
3. **Spell It Out:** curated dictionary readability, shuffled tile ordering,
   5-stage session with distinct length progression, correct/wrong submissions,
   30-second timeout, pause freeze, final summary and repeated rounds.
4. **Word Excavation:** 8/9-letter source, repeated-letter inventory, minimum
   length, invalid/duplicate words, points and capped time bonuses, 60-second
   timeout, pause freeze, completion, result and restart.
5. **Cross-cutting:** pause and resume, options placeholder behavior, mouse
   input, back/quit confirmation, return to mode select, no missing scripts,
   no unintentional scene changes, no console errors.
6. Fix confirmed gameplay bugs with new regression tests before visual work.

## Gate B: UI design direction and review

- Match the approved Main Menu's warm fantasy storybook aesthetic: parchment,
  carved stone, subtle gold outlines, readable ivory text, cyan magic only
  where it serves interactions.
- Build **distinct mode identities** while keeping shared headers, back/pause
  controls, primary buttons, result panels and hover/press responses.
- Keep actual text and input legible; do not convert gameplay tiles into
  expensive 3D effects without performance evidence.
- Word Master: make 5 guess rows, starter hint, 26-letter keyboard, and clues
  immediately understandable. Show clear post-win and post-loss flows.
- Spell It Out: prioritize definition readability, timer, shuffled input,
  answer progress and results summary.
- Word Excavation: prioritize source letters, composed word, 60-second timer,
  found-word tiers, reward progress and readable feedback.
- QA assets in mockups/contact sheets first; user reviews before production
  prefab/scene edits. No more prototype scene version proliferation.

## Gate C: Implementation and release QA

- Keep scenes and existing scene GUIDs unchanged. Prefer prefab-based reusable
  UI elements and predictable object names; remove runtime UI reconstruction
  only after verifying all scene references and safe migration.
- Target 2400x1080 landscape (Galaxy A05s) and PC. Test small touch targets,
  safe areas, text wrapping, hover alternatives and input latency.
- Verify animations and overdraw are inexpensive on Android hardware, then
  record actual FPS and screenshots instead of assuming optimization.
- Capture each mode's start, pause, success, failure, results, and exit states.
- Commit separately: gameplay bug fixes, approved UI assets, and scene wiring.
- Completion: all QA gates pass, user approves screens, no new Unity errors,
  Android device check passes and the final branch commit is recorded.
