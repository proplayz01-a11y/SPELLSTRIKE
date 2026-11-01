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


## Click-anywhere hint dismissal QA (November 1, 2026)

- First-letter Hint visibly prompts: Click anywhere to continue. The entire 2400x1080 transparent click shield consumes the first click, dismisses the tutorial hint, and does not select the keyboard letter underneath.
- Confirmed the next click on the same A key **does** select the letter. This test used the Unity EventSystem `IPointerClickHandler` pathway, not only a direct Button callback.
- After submitting REAPS against RAYED, the Turn and Prize cards appear together. One click on their shared shield dismisses both immediately, and they stay dismissed for that turn.
- Clicking directly on the Prize Hint frame also dismisses it and the Turn Hint, preserving mouse-hover detail display until dismissal.
- Invalid-word warnings persist until dismissed, rather than timing out. Dismissal leaves the invalid word untouched and exposes the board for correction; changing to a new invalid guess may show another warning.
- During hints, desktop keyboard input is gated, and the transparent button consumes Android taps without accidentally submitting. Physical Android touchscreen QA remains pending.
- The approved fantasy hint artwork, player input, dictionary, and scoring remain unchanged. Hints are runtime-only overlays; no scene edits were included.

## Plaque-anchored directional arrow QA (November 1, 2026)

- Replaced fixed Turn and Prize arrow positions/rotations with actual RectTransform-based targets. Arrow tip pivot is aligned inside the outer-facing edge of the *active* plaque, with the sprite rotated and scaled to connect from the hint card.
- Reused approved Arrow.png and Hint popup art; the shaft renders behind the card, so no new artwork or scene changes were necessary.
- Moved only the Prize hint popup slightly to the right to leave enough space for its visible arrow. The popup size and board/layout were not widened.
- Live Play Mode Turn 2 after submitting REAPS against RAYED: both cards visible and both arrow tips landed exactly on Turn 2 and Prize 5,000 plaque edges; visual screenshot inspected.
- Programmatically checked all 5 possible turn indices and both active plaques (10 targets) against their actual screen-space world positions: 10/10 passed with 0.0000 Unity-unit tip-position error; each tip was inside its intended plaque rect.
- Turn 5 simulated active index visual screenshot inspected. This test changes the test-only Play Mode session turn, not the serialized scene or a live scored match.
- Click anywhere regression while the hint shield was active: the UI pointer click targeted the Hint frame, did not modify the guess, and revealed the keyboard hitbox after dismissal.
- Active Game View measured 2400 x 1080; physical Android screen/device QA and frame-rate measurements were not performed.

Local project QA screenshots:
- Temp/WordMasterArrowQA/Before_Turn2_ArrowAlignment.png
- Temp/WordMasterArrowQA/After_Turn2_PlaqueAnchored.png
- Temp/WordMasterArrowQA/After_Turn5_PlaqueAnchored.png


## Stage Select ribbon and animated Hall of Chronicles background (November 1, 2026)

- Imported the approved PNG from Downloads/WORD MASTER/Word Master Fantasy Banner.png as the WordMasterFantasyBanner 2172x724 RGBA UI sprite. The original Downloads source artwork was not modified. Import settings: Single Sprite, uncompressed, no mipmaps, alpha transparency.
- Reused Stage Select's original LibraryBackground hierarchy and its HallOfChroniclesAmbient component as an isolated StageSelectAnimatedBackdrop.prefab. Extracted without modifying or saving the original StageSelect scene. The copy carries 40 background Images, 39 animated glow/flame bindings, and animate=true. The Stage Select Book-only visibility guard reference was cleared.
- The existing Hall of Chronicles golden-dust runtime effect is retained (36 non-interactive floating sprites), as are the existing candle flicker and accent effects. No static imitation or new animation algorithm was introduced.
- WordMasterRoyalUI applies only the art swap at runtime. It disables the old static LibraryBackdrop Image only after instantiating the animated backdrop, which renders behind the dim layer, board, keyboard, hints, and menu buttons. Gameplay, turns, prizes, dictionary, scoring, and menus are unchanged.
- The new baked serif WORD MASTER lettering is used; the old TitleRibbon TMP label is disabled to prevent double printing. Final banner dimensions are 970 x 182 at (75,445) on the 2400x1080 canvas and were visually inspected for board instruction clearance.
- In Android-target Unity Game View, the cloned background instantiates with IsAnimating=true, 39 animation layer bindings, golden dust active, and the old static image hidden. Five independently moving candle flames changed alpha and Y scale between runtime samples, confirming actual animation.
- Click-anywhere Hint regression: after a single click, both Hint and shield dismissed with the in-progress word unchanged. All 26 of 26 keyboard hitboxes were still top EventSystem raycast targets, and 0 background Images captured mouse/touch raycasts.
- The Unity script compilation completed with zero new errors. Physical Android FPS and touchscreen QA remain pending.
- Both the Stage Select scene and the protected Main Menu V11 scene were untouched. The dirty WordMasterScene was preserved and not saved, overwritten, or staged by this integration. Assets and UI script initialize the look automatically in Play Mode and builds.

Final visual evidence saved on project PC:
- Temp/WordMasterStageSelectArtQA/After_WordMaster_Ribbon_AnimatedStageSelectBackground.png
- Temp/WordMasterStageSelectArtQA/Header_Size_970x182_Y445.png
- Temp/WordMasterStageSelectArtQA/Final_ApprovedWordMasterBanner_StageSelectAnimatedBackground.png


## Play Mode positions persisted to Edit Mode (November 1, 2026)

- Inspected 417 live Word Master UI RectTransforms and compared 306 matching objects against the serialized WordMasterScene; only two normal UI elements differed: TitleRibbon and Board/Instructions. Runtime popup/animated background objects were not used as Edit Mode layout sources.
- User-approved Play Mode TitleRibbon placement captured exactly: anchored position (75, 446.73), size (970,182), local scale (0.91, 1.59, 0.81), zero rotation. Board/Instructions anchored position changed from (0,228) to (0,205).
- Saved both RectTransforms into WordMasterScene in Edit Mode. The actual WordMasterFantasyBanner sprite is assigned to the scene Image; its duplicate WordMasterTitle TMP text is disabled in Edit Mode too.
- Updated both WordMasterRoyalUI runtime skin defaults and BuildLayout defaults to match; repeated Play Mode QA confirmed the same positions, scales, artwork, and Stage Select ambient background. After exiting Play Mode, Edit Mode inspection confirmed all values persisted and the scene was saved.
- The scene had thousands of unrelated pre-existing worktree changes. Git index staged ONLY the six explicitly approved scene changes via an audited, targeted patch; unrelated dirty scene work remains unstaged and preserved.
- Before modifying the scene, its bytes and source script and a TSV snapshot of all 417 Play Mode transforms were backed up under Downloads/WORD MASTER/EDITMODE_POSITION_PERSIST_20261101_1658.
- Screenshots: Temp/WordMasterRibbonPositionsQA/UserApproved_PlayMode_RibbonBeforePersistence.png and Temp/WordMasterRibbonPositionsQA/Persisted_Ribbon_PlayMode_Verification.png.
