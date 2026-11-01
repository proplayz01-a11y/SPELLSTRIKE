# Word Master Royal UI | SPELLSTRIKE

Production scene: Assets/Scenes/WordMasterScene.unity (original scene GUID retained).
Overlay canvas: WordMasterRoyalCanvas, sorting order 220, 2400 x 1080 reference resolution.
Runtime view: Assets/Scripts/Minigames/UI/WordMasterRoyalUI.cs.
Gameplay adapter: Assets/Scripts/Minigames/Modes/MinigamesSceneController.WordMasterRoyalBridge.cs.

## Asset design

- Resources/WordMasterRoyal holds the approved UI sprite set and a separately fitted compact keyboard-frame sprite.
- Slot, Normal, Gold, Silver, DarkGray, Red, and L_A through L_Z share the same 1024 x 1024 canvas and center.
- Three layers form a guess cell: Holder, TileBase, LetterOverlay. One sprite set is reused across turns and A-Z buttons.
- Bookworm-style alphabet keyboard has a perfectly centered 13 x 2 arrangement: A-M first row, N-Z second row. Centers use 99 UI units horizontally and 92 vertically; 126 x 126 sprite canvases show approximately 99 x 92 visible pixels, making adjacent tiles nearly touch.
- Letter overlays are rendered at 155 x 155 for bigger, legible Times New Roman Bold black 3D letters. Each tile has an invisible 96 x 90 hit target to prevent overlapping pointer regions, verified in Unity using actual EventSystem raycasts at all 26 centers (26/26 passed).
- KeyboardFrameCompact.png is a 1570 x 220 three-piece, aspect-controlled crop of the approved Ornate Empty Fantasy UI Panel in Downloads. It preserves both compass end caps and navy interior; no replacement artwork was generated. Runtime migration updates legacy serialized keyboard rectangles on scene load without overwriting the scene or unrelated dirty edits.
- Approved art sources were copied from Downloads, universal fit kit, and existing in-project Hall of Chronicles resources. Original source files remain unchanged.
- Gold is correct slot, silver is valid letter in wrong slot, dark gray is missing, red is absent on keyboard.
- Turns and Prize plaques use active-gold/inactive-gray visuals and pulsing overlays. Submit is disabled until a five-letter dictionary-valid guess exists.
- Tutorial hint arrow targets the initially locked tile. After submission the ornate panel displays clues.
- Score accumulates turn prizes (10,000 / 5,000 / 2,500 / 1,000 / 500), Words counts solved rounds. Crossed-out prizes show a red X after an unsuccessful guess.

## Behavior / ownership

The production scene retains the existing MinigamesCanvas gameplay controller and DictionaryManager prefab. WordMasterRoyalUI only renders and forwards interactions to WordMasterSession via the Royal bridge. It does not replace dictionary or clue rules.

Do not run a blanket minigame UI rebuild on WordMasterScene: the Royal overlay is already serialized. This integration does not change the Main Menu, Spell It Out, Word Excavation, or cinematics.

## Verified in Unity Editor Play Mode

- 26 A-Z keyboard buttons, five guess rows, active Turns and Prize indicators
- Compact keyboard Game View QA and raycast center validation: 26/26 buttons targeted correctly
- Locked hint and repeated-letter solve using TRUTH
- Full invalid dictionary word disables Submit
- Gold, silver, missing and red keyboard states
- Five wrong dictionary-valid attempts trigger Game Over
- Victory awards 10,000 points on turn one and increments Words
- New Round preserves cumulative Score; Menu > Resume works
- 2400 x 1080 Android-reference Game View screenshot checked with all assets

Physical Galaxy A05s profiling and APK build were not run. On-device layout and framerate need separate acceptance testing.


## Final defense gameplay integration (November 1, 2026)

- Starting-letter hint plus contextual on-hover/on-tap clues for previously submitted slots.
- Gold correct-position letters carry into the next turn as frozen ivory tiles. Silver indicates a letter elsewhere, dark-gray is absent. Only eliminated letters turn red on the A-Z keyboard.
- The Word Master-only 20 KB word supplement is exported from the project's existing word_definitions.json. It adds 2,905 dictionary entries without changing other minigames. This includes RAKER, used in the supplied Bookworm reference recording.
- Invalid words keep Submit disabled and show dictionary feedback. Players can remove and replace letters by clicking occupied current-row slots.
- Victory and Game Over screens reveal the mystery word, points, solved count, and options to replay, select another minigame, or return to the Main Menu. Victory uses three small pulsing sprite stars.
- See WORDMASTER_FINAL_DEFENSE_QA.md for tests, reference recording, screenshot paths, and device-validation limitations.
