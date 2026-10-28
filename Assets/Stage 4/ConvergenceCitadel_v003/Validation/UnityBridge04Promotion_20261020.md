# Stage4 Bridge04 collision-only promotion, 2026-10-20
- Verified RuntimeQA source: all four bridges, eight directions PASS via CharacterController.Move; previous 6/8 failure preserved.
- Visual GLB and source Blender geometry unchanged; only original Bridge04 deck collision is disabled and the new smooth collision proxy is enabled.
- Proxy built from original authored width 30.72 and piecewise y profile 15->15->27->27 Unity units.
- Backups: `Library/Stage4Bridge04_ValidatedPromotion_20261020`. No original SampleScene, Stage2, Stage3, prefabs, saves or Build Settings edited.
- PASS: `Assets/Scenes/Stage4Scene/Stage4Scene.unity`, original disabled, proxy enabled, 19 total enabled collider instances.
- PASS: `Assets/Scenes/Stage4Scene/Stage4Scene_ConvergenceCitadel_v003_Validation.unity`, original disabled, proxy enabled, 19 total enabled collider instances.
- STATUS: PASS, verified collision proxy in both isolated Stage4 scenes. Canonical player/Titan animation QA still pending.
