# Stage4 v003 real Play Mode capsule collision traversal (NOT canonical player)
Dedicated Stage4 RuntimeQA scene only, original VanAert/Titan prefabs inactive. No GameDatabaseManager, objectives, unlocks or PlayerPrefs writes by this test.
Real CharacterController.Move + imported MeshCollider contact while traversing all four bridge decks forwards and backwards. This is not player input/animation or Titan combat.
- Capsule: height 21.42, radius 5.355, step 3.213, slope 45; speed=65.0 units/s
- Bridge 4 forward: FAIL, path=PathComplete, corners=2, actualDeckContacts=579, unsupportedFrames=0, groundedFrames=692/700, maxSingleFrameDrop=0.700, elapsedSimulation=7.29s
  - start=(-1551.93, 18.40, -752.29); endpoint=(-1864.61, 30.40, -904.41); FINAL=(-1710.33, 15.45, -829.35); remainingXZ=171.569; sideCollisionFrames=555/700; flagsLast=7; navCorners=(-1551.93, 18.40, -752.29); (-1864.61, 30.40, -904.41)
  - CapsuleCast obstacle candidates (nearest 15m): COL_Bridge_04_Deck_PHYSICS@6.25
- Bridge 4 reverse: FAIL, path=PathComplete, corners=2, actualDeckContacts=617, unsupportedFrames=0, groundedFrames=664/700, maxSingleFrameDrop=0.300, elapsedSimulation=5.07s
  - start=(-1864.61, 30.40, -904.41); endpoint=(-1551.93, 18.40, -752.29); FINAL=(-1731.44, 19.12, -839.61); remainingXZ=199.620; sideCollisionFrames=377/700; flagsLast=7; navCorners=(-1864.61, 30.40, -904.41); (-1551.93, 18.40, -752.29)
  - CapsuleCast obstacle candidates (nearest 15m): 
- Bridge4 diagnostic pass: 0/2 directional bridge runs pass. Original complete 6/8 run is retained unchanged. This cannot validate original VanAert controls, boss combat, camera or physical A05s performance.
