# SPELLSTRIKE Word Master | Final gameplay and UI QA

Date: 2026-11-01
Engine: Unity 6.3 LTS (6000.3.10f1), URP
Production scene: Assets/Scenes/WordMasterScene.unity
Build slot: 8. Main Menu V11 remains build slot 0, GUID feb90d4e4b6367541b727d6eeb81e0ac.
Primary behavior reference: Videos/Screen Recordings/Screen Recording 2026-11-01 124437.mp4 (2:08, 30 FPS). Main steps: first-letter hint, five guesses, color clues, carry-forward correct positions, five-turn loss.

## Verified in Unity Editor Play Mode

| Case | Result |
|---|---|
| First-letter hint and directional arrow | Passed |
| Correct gold, present silver, missing gray | Passed |
| Correct positions frozen as ivory on the next turn | Passed |
| Full RAYED example: REAPS, RATES, RACER, RAVEN, RAKER | Passed, Game Over after attempt 5 |
| Word Master supplemental dictionary recognizes RAKER | Passed |
| Eliminated keyboard letters turn red | Passed |
| All 26 A-Z key hitbox centers, EventSystem raycast | 26/26 |
| Invalid TZQXJ word, disabled Submit, dictionary feedback | Passed |
| Clear all open slots and replace with TRUTH | Passed |
| Duplicate-letter guess SHEEP typed with both Es | Passed |
| Five turn prizes: 10000, 5000, 2500, 1000, 500 | 5/5, total 19000 |
| Score and Words persist across New Round | Passed |
| Game Over reveals target word and replay/mode/menu options | Passed |
| Victory awards points and shows sprite stars | Passed |
| Contextual tile hover gives explanatory clue | Passed |
| Dismiss initial hint and show it again on a new round | Passed |
| Paused via Menu and resumed through Resume | Passed |
| Prize 4 label retained as 1,000 in saved Edit Mode scene | Passed |
| Result > Mode Select > Word Master navigation | Passed |
| Result > Main Menu navigation | Passed, canonical GUID and build slot unchanged |
| Screen-space UI at 2400 x 1080, active Android build target | Passed in Editor |
| Original Main Menu V11 scene unaffected | Passed by Git scope / GUID |

## Visual QA captures (local project Temp folder)

- Temp/WordMasterFinalDefenseQA/WM_FullGameplay_Runtime_Initial.png
- Temp/WordMasterFinalDefenseQA/WM_RAYED_Turn3_LockedClues_QA.png
- Temp/WordMasterFinalDefenseQA/WM_Final_BookwormFiveTurnsGameOver.png
- Temp/WordMasterFinalDefenseQA/WM_VictoryStars_ActualSprite_QA.png

## Scope and remaining device checks

Editor Play Mode tests were run on the Android build target at 2400 x 1080. No APK was built or installed, and physical Samsung Galaxy A05s performance, hardware touch accuracy and framerate have NOT been validated. An existing unrelated Unity Remote requirements check error was observed; no Word Master C# compilation errors appeared. A physical device acceptance pass is still recommended before claiming Android performance validation.

The implementation reuses approved Royal Word Master sprite art from Downloads/WORD MASTER; a small procedural five-point star sprite replaces a missing-font glyph on the victory popup. Existing scene edits outside the Word Master layout were kept out of the focused source control changes.


## Bookworm contextual hint follow-up, 2026-11-01

Reference: user screenshots of Bookworm's turn, prize and invalid-word hint cards.

- Reused the existing approved Hint sprite as the gold frame; added a non-interactive dark inset for readable ivory text, with compact sizing and directional pointers.
- Turns and Prize hints show the current turn and the authoritative prize value, then reveal detailed explanations on hover. Tapping either plaque or its visible hint toggles the details for Android users who cannot hover.
- A complete dictionary-invalid guess shows a compact red Submit-disabled explanation; hovering or tapping reveals how to remove placed tiles and try another word.
- No dictionary, turn, prize, scoring, or level navigation logic was changed by the contextual hints. Popups are runtime UI-only and deliberately do not write unrelated scene hierarchy changes.
- Confirmed Turn 3 of 5 and the 2,500-point prize popup after the RAYED / REAPS / RATES sequence.
- Confirmed TZQXJ is invalid, Submit disabled, expanded explanation readable, invalid popup hidden after removing tiles, then TRUTH accepted and victory awarded.
- Unity compilation: zero new script errors. The 2400x1080 Android-target Game View was inspected, but Android device touch/performance has not been tested.

Visual captures on the project PC:
- Temp/WordMasterContextHintQA/WM_Turn3_DarkHints.png
- Temp/WordMasterContextHintQA/WM_InvalidWord_CompactHint.png
- Temp/WordMasterContextHintQA/WM_InvalidWord_ExpandedFinal.png
