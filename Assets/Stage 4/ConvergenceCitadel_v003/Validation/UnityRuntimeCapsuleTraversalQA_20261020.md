# Stage4 v003 real Play Mode capsule collision traversal (NOT canonical player)
Dedicated Stage4 RuntimeQA scene only, original VanAert/Titan prefabs inactive. No GameDatabaseManager, objectives, unlocks or PlayerPrefs writes by this test.
Real CharacterController.Move + imported MeshCollider contact while traversing all four bridge decks forwards and backwards. This is not player input/animation or Titan combat.
- Capsule: height 21.42, radius 5.355, step 3.213, slope 45; speed=65.0 units/s
- Bridge 1 forward: PASS, path=PathComplete, corners=2, actualDeckContacts=103, unsupportedFrames=0, groundedFrames=142/157, maxSingleFrameDrop=0.420, elapsedSimulation=2.27s
- Bridge 1 reverse: PASS, path=PathComplete, corners=2, actualDeckContacts=147, unsupportedFrames=0, groundedFrames=219/246, maxSingleFrameDrop=0.280, elapsedSimulation=2.27s
- Bridge 2 forward: PASS, path=PathComplete, corners=3, actualDeckContacts=122, unsupportedFrames=0, groundedFrames=227/239, maxSingleFrameDrop=0.700, elapsedSimulation=2.78s
- Bridge 2 reverse: PASS, path=PathComplete, corners=3, actualDeckContacts=124, unsupportedFrames=0, groundedFrames=218/240, maxSingleFrameDrop=0.337, elapsedSimulation=2.75s
- Bridge 3 forward: PASS, path=PathComplete, corners=2, actualDeckContacts=238, unsupportedFrames=0, groundedFrames=608/633, maxSingleFrameDrop=0.280, elapsedSimulation=4.83s
- Bridge 3 reverse: PASS, path=PathComplete, corners=2, actualDeckContacts=276, unsupportedFrames=0, groundedFrames=694/724, maxSingleFrameDrop=0.336, elapsedSimulation=4.83s
- Bridge 4 forward: FAIL, path=PathComplete, corners=2, actualDeckContacts=4690, unsupportedFrames=0, groundedFrames=4967/5000, maxSingleFrameDrop=0.280, elapsedSimulation=36.70s
- Bridge 4 reverse: FAIL, path=PathComplete, corners=2, actualDeckContacts=4917, unsupportedFrames=0, groundedFrames=4972/5000, maxSingleFrameDrop=0.609, elapsedSimulation=39.68s
- Final runtime capsule traversal: 6/8 directional bridge runs pass. This cannot validate original VanAert controls, boss combat, camera or physical A05s performance.
