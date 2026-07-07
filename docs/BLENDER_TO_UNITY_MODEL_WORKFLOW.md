# Blender to Unity Model Workflow

Use this for rigged enemies with separate clothing, robes, armor, cloaks, or coats.

## Main Rule

Do not use `Ctrl + J` as the main fix for clothing.

Joining meshes does not make clothing follow bones. Clothing needs:
- correct armature parenting
- vertex groups
- transferred weights

## Clean Workflow

1. Start with a clean Blender scene.
2. Import rigged body first.
3. Keep only one real armature.
4. Rename the armature clearly, such as `Corsair_Rig`.
5. Set rig to Rest Position.
6. Import clothing mesh.
7. Fit clothing manually.
8. Apply Rotation & Scale to clothing.
9. Parent clothing to armature using `With Empty Groups`.
10. Transfer weights from body to clothing.
11. Test in Pose Mode.
12. Delete hidden body parts under clothing.
13. Export final selected objects as FBX.

## Weight Transfer Direction

Correct:

```text
Body → Clothing
```

Do not transfer in reverse.

## Common Error

If Blender shows:

```text
Bone Heat Weighting: failed to find solution for one or more bones
```

Use:

```text
With Empty Groups → Transfer Weights
```

not automatic weights.

## Unity Import Rules

For final model FBX:
- Rig tab
- Animation Type: Humanoid
- Avatar Definition: Create From This Model

For animation FBXs:
- Animation Type: Humanoid
- Avatar Definition: Copy From Other Avatar
- Source: final model avatar

For idle/move:
- Loop Time ON
- Loop Pose ON

For attack/hit/death:
- Loop Time OFF

Root motion:
- OFF unless specifically needed.
