# Stage 4 v003 standalone NavMesh QA in Unity Editor
No Play Mode. Geometry sources restricted to the 19 isolated imported collision meshes, not SampleScene.
Temporary NavMeshData is added for path checks, then removed. No global NavMesh settings altered.
- Agent: radius=5.355, height=21.42, climb=3.213, slope=45, voxel=1, tileSize=128
- Physics collision sources collected: 19
- Bake bounds min=(-2313.43, -45.10, -1261.78) max=(101.87, 90.52, 110.12)
- Standalone in-memory bake: SUCCEEDED
- Start nav sample: FOUND, position=(6.00, 2.29, 12.00)
- HollowKnight nav sample: FOUND, position=(-276.00, 6.40, -132.00)
- DrownedCaptain nav sample: FOUND, position=(-624.00, 12.40, -306.00)
- Titan nav sample: FOUND, position=(-1280.00, 18.40, -620.00)
- VanderPoel nav sample: FOUND, position=(-2020.00, 30.40, -980.00)
- Start -> HollowKnight: PASS status=PathComplete corners=3
- Start <- HollowKnight: PASS status=PathComplete corners=3
- HollowKnight -> DrownedCaptain: PASS status=PathComplete corners=3
- HollowKnight <- DrownedCaptain: PASS status=PathComplete corners=3
- DrownedCaptain -> Titan: PASS status=PathComplete corners=2
- DrownedCaptain <- Titan: PASS status=PathComplete corners=2
- Titan -> VanderPoel: PASS status=PathComplete corners=2
- Titan <- VanderPoel: PASS status=PathComplete corners=2
- Adjacent directional paths complete: 8/8; do not claim Play Mode traversal from this fixture.
- Independent test NavMesh asset: Assets/Stage 4/ConvergenceCitadel_v003/Navigation/ConvergenceCitadel_Player535_UnityQA_NavMesh.asset
- Titan IK/deformed contacts, player walking, camera, attacks and Galaxy A05s: NOT TESTED
