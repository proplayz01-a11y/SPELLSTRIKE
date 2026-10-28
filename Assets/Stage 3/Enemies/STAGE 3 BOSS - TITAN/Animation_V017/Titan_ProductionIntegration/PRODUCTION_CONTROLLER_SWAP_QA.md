# Titan V017 isolated production Animator controller clone

**Asset:** `Titan_V017_Stage3Boss_WDOn_ProductionClone.controller`

**Parent:** `../Titan_V017_Stage3Boss.controller`

**Proof controller:** `../Titan_TestArena/TitanV017_WriteDefaultsON_StompProof.controller`

## Exact serialized difference

The clone is an independent AnimatorController asset with its own Unity GUID and name.
It is byte-for-byte equivalent to the production source after exactly two
intentional replacements:

1. The controller's `m_Name` matches the new filename.
2. `ColossalStompRightFootV017` changes `m_WriteDefaultValues: 0` to `1`.

Compared with the isolated WriteDefaultsON proof controller, only `m_Name`
differs. All eight original state names/fileIDs, four parameters, eight
original animation GUIDs, ten transitions, transition conditions, timing,
base-layer defaults, state layout and remaining serialized fields are retained.
`TitanDeathFromRightPreview` intentionally retains its original Write
Defaults OFF setting; the six other non-stomp states remain ON.

## Preserved animation map

| Animator state | Existing animation asset (relative to `STAGE 3 BOSS - TITAN/`) |
| --- | --- |
| `DormantStatueV004` | `Animation_V004/Titan_Awakening_V004_DormantPose.anim` |
| `AwakeningV004` | `Animation_V004/Titan_Awakening_FlowGVHMR_V004_OneShot.anim` |
| `AwakenedHoldV004` | `Animation_V004/Titan_Awakening_V004_AwakenedPose.anim` |
| `ColossalHeavyWalk` | `Animation_V003/Titan_ColossalHeavyWalk_V001_Looping.anim` |
| `HammerfistV003` | `Animation_V003/Titan_Hammerfist_V003_OneShot.anim` |
| `EarthbreakerSlamV003` | `EarthbreakerSlam_V003/Titan_EarthbreakerSlam_V003_OneShot.anim` |
| `TitanDeathFromRightPreview` | `Animation_V015/Titan_DeathFromRight_RetargetPreview.anim` |
| `ColossalStompRightFootV017` | `Animation_V017/Titan_ColossalStompFourRight_V017_CrushReview.anim` |

All eight serialized `m_Motion` references resolve through their existing
`.anim.meta` files. This clone does not contain or duplicate animation curves.

## Deployment and acceptance

This is an **opt-in candidate controller**, not an automatically deployed
change to the Stage 3 production scene or prefab. The existing
`S3N4_TitanV017_ProductionRig.prefab` still references the original controller
GUID `93ff9461beb8a6c439479695706ed807`.

An integration owner must explicitly assign the new controller to a **staged**
production Titan Animator instance, keeping its Avatar, RigBuilder,
procedural IK, gameplay components and animation relay wiring intact. Validate
the eight states, Walking-bool loop and return, Awaken/Hammerfist/Earthbreaker
triggers, authored one-shot recovery, and V017 start/stop/restoration. The V017
state has no built-in exit transition and relies on its owning stomp
integration to return to the previous animation.

Write Defaults ON can restore default properties during V017 animation; the
isolated four-contact proof alone does not establish combined production
rig/gameplay correctness. Production acceptance additionally requires real
deformed-mesh sole-ground contact at all four impacts, planted left-foot
support, the unchanged strict 94% anatomical reach limit, natural head/chest
motion, and an uninterrupted gameplay encounter without duplicate damage.

## Verification scope

Static YAML validation completed without touching any source clips, controllers,
scenes or prefabs: the clone differs from production by only the two declared
fields and matches the proof controller except for its name; eight distinct
motion GUIDs exist; eight Animator state and ten transition fileIDs resolve;
the cloned controller and folder have unique Unity `.meta` GUIDs.

Unity import, Editor compilation, Play Mode, real contacts, procedural
interaction and Stage 3 combat testing must be carried out by the integration
owner before production adoption. No runtime or visual PASS is claimed here.
