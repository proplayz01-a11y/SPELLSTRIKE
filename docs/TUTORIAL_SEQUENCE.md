# SPELLSTRIKE Locked Tutorial Sequence

**Status:** Approved and locked  
**Date:** 2026-07-25

This document is the canonical behavioral and presentation specification for
`TutorialScene`. Future implementation and UI changes must preserve this
sequence unless the team explicitly revisits the decision.

## Locked State Order

```text
IntroWelcome
-> IntroGameExplanation
-> MoveToBridge
-> MoveToShrine
-> TileIntroduction
-> TileSelectionExplanation
-> FormPlay
-> StrikePlay
-> CollectReplacementTiles
-> DefeatTrainingDummy
-> ClaimSpellbook
-> Complete
```

## Click-to-Advance Panel Rule

Introduction and lesson pages advance when the player clicks or taps the
tutorial panel.

This is a simple panel sequence:

```text
Panel appears
-> player clicks or taps anywhere on the active panel
-> next panel content appears
```

It does not use or imitate a literal animated book-page turn. A simple content
swap or short fade is sufficient.

- A dedicated fullscreen `ContinueButton` may receive clicks only while a page
  is waiting to advance.
- Decorative tutorial images, text, frames, arrows, and overlays must not block
  gameplay raycasts.
- The fullscreen continue receiver must be disabled immediately when gameplay
  interaction resumes.
- Each page should show a small `Click or tap to continue` indicator.

## State Specification

### 1. IntroWelcome

- Lock player movement and word-combat input.
- Keep the Tile Pool empty.
- Present:

```text
WELCOME TO SPELLSTRIKE
In this world, words become spells.
```

### 2. IntroGameExplanation

- Keep movement and word-combat input locked.
- Present:

```text
Collect letters, form valid English words,
and use those words in combat.
```

- Clicking or tapping this page closes the introduction and begins movement.

### 3. MoveToBridge

- Unlock player movement.
- Keep the Tile Pool empty and non-interactable.
- Show `TUTORIAL: MOVE`.
- Target the bridge checkpoint using the reusable animated World Arrow.
- The shrine checkpoint must not advance the tutorial during this state.

### 4. MoveToShrine

- Retarget the same World Arrow to the shrine checkpoint.
- Keep the Tile Pool empty and non-interactable.
- Advance only after the player reaches the shrine.

### 5. TileIntroduction

- Disable the World Arrow.
- Present a page containing:
  - a screenshot or approved image of the floating letter tile;
  - the heading `LETTER TILES`;
  - the body copy:

```text
Letter tiles are used to form the words
that power your spells.
```

- The final tile screenshot is a required content asset and may be replaced
  without changing tutorial logic.

### 6. TileSelectionExplanation

- This page does not require an image.
- Explain that selecting letter tiles places them in the Word Bar and that the
  selected letters must form the instructed word.
- After the page is advanced, populate and enable the Tile Pool.

### 7. FormPlay

- Populate exactly 16 shuffled Tile Pool letters containing `P`, `L`, `A`, and
  `Y`.
- Guide selection in the order `P -> L -> A -> Y`.
- Use animated UI pointers/highlights without revealing the letters in answer
  order inside the Tile Pool.
- Invalid gibberish remains uncastable through dictionary validation.
- Other valid submitted words do not execute an attack, do not consume tiles,
  and return their selected letters to the Tile Pool.

### 8. StrikePlay

- When `PLAY` is present in the Word Bar, highlight the STRIKE button.
- The player must press STRIKE to advance.
- `PLAY` executes through the normal attack pipeline.
- The tutorial must not create a separate replacement-tile system.

### 9. CollectReplacementTiles

- The existing `TileSpawner` produces four replacement world tiles for the
  four consumed letters in `PLAY`.
- One reusable World Arrow targets the nearest remaining replacement tile.
- After a tile is collected, the arrow retargets the nearest remaining tile.
- Only the replacement batch produced by the guided `PLAY` cast counts.
- Disable the arrow after all four tiles in that batch are collected.

### 10. DefeatTrainingDummy

- The dummy visual may remain visible before this state, but its combat target
  must be inactive or untargetable.
- After all four replacement tiles are collected, enable the dummy combat
  target and its 25 HP.
- Allow the player to form any valid English word and attack normally until the
  dummy is destroyed.

### 11. ClaimSpellbook

- Reveal the Spellbook at the shrine after confirmed dummy defeat.
- The Spellbook may scale in, float, rotate, glow, and use a destination marker.
- Entering the pickup trigger changes the tutorial panel from approach guidance
  to an interaction prompt; proximity alone does not claim the reward.
- The player presses `E` on PC to claim it. Android uses the same public claim
  action through a platform-appropriate `COLLECT` button.
- The final 3D model replaces the placeholder visual without changing the
  pickup root or logic.

### 12. Complete

- Record tutorial completion.
- Present the final completion message.
- Continue to the approved next scene when that transition is implemented.

## World Arrow Rule

Use one reusable arrow controller. Retarget it rather than instantiating an
unbounded series of arrow objects.

```text
Bridge checkpoint
-> Shrine checkpoint
-> Nearest remaining replacement tile
-> disabled
```

## Presentation Rule

The tutorial HUD must reproduce the navy, gold, translucent-panel presentation,
contextual arrows, glowing highlights, and layout language shown in the
adviser-presented tutorial mockups. Necessary content substitutions are:

- guided word: `PLAY`;
- final reward: Spellbook rather than fragment.

## Validation Requirement

The tutorial is not considered complete until the entire sequence passes:

- Unity Play Mode on PC;
- Android device testing;
- input-raycast validation with the tutorial HUD enabled;
- wrong-valid-word retry;
- normal four-tile replacement spawning and collection;
- dummy defeat and Spellbook claim.
