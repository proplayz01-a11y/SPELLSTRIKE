# Word Master Royal UI | SPELLSTRIKE

Production scene: Assets/Scenes/WordMasterScene.unity (original scene GUID retained).
Overlay canvas: WordMasterRoyalCanvas, sorting order 220, 2400 x 1080 reference resolution.
Runtime view: Assets/Scripts/Minigames/UI/WordMasterRoyalUI.cs.
Gameplay adapter: Assets/Scripts/Minigames/Modes/MinigamesSceneController.WordMasterRoyalBridge.cs.

## Asset design

- Resources/WordMasterRoyal holds 49 UI sprite PNGs and matching Unity import metadata.
- Slot, Normal, Gold, Silver, DarkGray, Red, and L_A through L_Z share the same 1024 x 1024 canvas and center.
- Three layers form a guess cell: Holder, TileBase, LetterOverlay. One sprite set is reused across turns and A-Z buttons.
- Approved art sources were copied from Downloads, universal fit kit, and existing in-project Hall of Chronicles resources. Original source files remain unchanged.
- Gold is correct slot, silver is valid letter in wrong slot, dark gray is missing, red is absent on keyboard.
- Turns and Prize plaques use active-gold/inactive-gray visuals and pulsing overlays. Submit is disabled until a five-letter dictionary-valid guess exists.
- Tutorial hint arrow targets the initially locked tile. After submission the ornate panel displays clues.
- Score accumulates turn prizes (10,000 / 5,000 / 2,500 / 1,500 / 500), Words counts solved rounds.

## Behavior / ownership

The production scene retains the existing MinigamesCanvas gameplay controller and DictionaryManager prefab. WordMasterRoyalUI only renders and forwards interactions to WordMasterSession via the Royal bridge. It does not replace dictionary or clue rules.

Do not run a blanket minigame UI rebuild on WordMasterScene: the Royal overlay is already serialized. This integration does not change the Main Menu, Spell It Out, Word Excavation, or cinematics.

## Verified in Unity Editor Play Mode

- 26 A-Z keyboard buttons, five guess rows, active Turns and Prize indicators
- Locked hint and repeated-letter solve using TRUTH
- Full invalid dictionary word disables Submit
- Gold, silver, missing and red keyboard states
- Five wrong dictionary-valid attempts trigger Game Over
- Victory awards 10,000 points on turn one and increments Words
- New Round preserves cumulative Score; Menu > Resume works
- 2400 x 1080 Android-reference Game View screenshot checked with all assets

Physical Galaxy A05s profiling and APK build were not run. On-device layout and framerate need separate acceptance testing.
