# SPELLSTRIKE — Imported Magic Heal Feeding Pose

## Final idle-loop trim — user-approved frames 28–42

On 2026-11-23, the user requested looping **only frames 28 through 42**
of the supplied `Y Bot@Magic Heal.fbx`, instead of the full animation.
Updated the Unity `ModelImporter.clipAnimations[0]` on
`Assets/Animations/SoulFeeding/YBot_MagicHeal.fbx`:

- `firstFrame = 28`, `lastFrame = 42`
- `loopTime = true`, `loopPose = true`
- Root rotation, height and horizontal translation locked as before
- 30 FPS, resulting loop length **0.4667 seconds**
- Clip remains `SoulFeeding_MagicHeal_Loop`, and its Humanoid avatar,
  derived Animator Controllers and upper-body layer references remain valid

**Verified in actual Stage 1 Unity Play Mode:** Bramble Sprite, Stone
Sentinel and Cursed Jester each played the exact short-loop clip at full
layer weight. The controller reported `normalizedTime=19.81`, proving
repeated playback. Stone Sentinel's existing `StartBattle()` correctly
switched `SoulFeeding` off and reset the layer from 1.00 to 0.00.

The original FBX file bytes were not modified; only Unity's local FBX
importer settings were changed. Importer metadata backup:
`Temp/MagicHealTrim_28to42_20261123/YBot_MagicHeal.fbx.meta.before_trim`.
The local save was unchanged by this Play Mode test (matching SHA-256
before/after). No assets were staged or pushed to the public repository.

Implemented: 2026-11-23

## User-supplied animation

- Original uploaded filename: `Y Bot@Magic Heal.fbx`
- File also found in `C:\Users\David\Downloads\Y Bot@Magic Heal.fbx`
- Uploaded copy and Windows Downloads copy have identical SHA-256:
  `3ccaed27b9db0989fac825f60a204c0cd1cf101ddae077276459c092aec96e6c`
- Source imported locally as:
  `Assets/Animations/SoulFeeding/YBot_MagicHeal.fbx`
- Unity FBX import: **Humanoid**, valid Human Avatar, a single
  2.6667-second playable animation renamed `SoulFeeding_MagicHeal_Loop`,
  `loopTime=true`, `loopPose=true`, root translation/rotation locked.

## Integration

**Story correction:** Hollow Knight does **not** use the Magic Heal feeding
pose or external fragment-to-hand energy stream. Nevali's fragment is
**embedded inside the Hollow Knight**, not beside him for a separate draining
ritual. The Hollow Knight's `SoulFragmentFeeding` component has been removed
from the Stage 1 scene, and his original
`Assets/Characters/HollowKnight/HK_Animator_Controller.controller` has been
restored. His existing boss-fragment pickup and combat progression have not
been deleted or changed. Both installers explicitly exclude this boss.

`Assets/Editor/SoulFeedingMagicHealInstaller.cs` installs an independent
upper-body masked override layer onto a **copy** of each enemy's existing
Animator Controller. Source combat controllers are unchanged. The new layer
`Soul Feeding - Magic Heal` is appended to the end of the original layer
stack, with default weight zero and looping `Magic Heal (loop)` state.
Mask includes body, head, both arms and fingers, excluding root and legs
so normal locomotion and original animation events remain on base layers.

`Assets/Scripts/Story/SoulFragmentFeeding.cs` now fades the imported
layer to full weight during an uninterrupted precombat feeding ritual.
When the existing combat controller starts battle, it immediately sets
the feeding layer weight to zero and destroys the visual soul-transfer
energy effect. If an imported layer is unavailable, the original procedural
arm extension remains available as a fallback. No combat, objective,
fragment pickup or enemy health systems were edited.

### Wired humanoids (9 total, Hollow Knight excluded)

| Stage | Actor |
| --- | --- |
| 1 | Bramble Sprite |
| 1 | Stone Sentinel |
| 1 | Cursed Jester |
| 2 | Barnacle Husk |
| 2 | Tide Wraith |
| 2 | Corsair Phantom |
| 2 | Drowned Captain |
| 3 | Sunbound Warden |
| 3 | Desert Archer |

The nine used derived controllers are in:
`Assets/Animations/SoulFeeding/Controllers/`.

**Not changed:** Stone Golem (nonhumanoid), Titan Earthbreaker
(nonhumanoid and separately staged statue/riddle awakening). The Corsair
Phantom remains inactive until its existing spawn sequence; the animation
layer is installed for when the actor becomes active. Stage 4 is not part
of this milestone.

## Verification

- Unity 6.3 imported the FBX as an `Avatar.isValid && Avatar.isHuman`
  asset and `AnimationClip.humanMotion` was true.
- Initial Edit Mode installation had ten humanoid enemies. Following the
  embedded-fragment correction, there are **nine intended** humanoid feeders.
  Hollow Knight's original controller was restored in the saved Stage 1 scene.
  The nine remaining configured humanoids have their derived controllers,
  imported looping clip on their appended layer, and
  `useImportedMagicHeal=true`.
- Original Stage 1 Play Mode test (before Hollow Knight exclusion): four
  humanoids ran the authored animation at
  layer weight 1.00; `normalizedTime` exceeded 3.0, confirming looping.
  Calling Stone Sentinel's existing `StartBattle()` stopped the ritual,
  set animation layer weight from 1.00 to 0.00, and left combat started.
- Stage 2 Play Mode: Barnacle Husk, Tide Wraith, and Drowned Captain ran
  the imported animation at full layer weight. Corsair Phantom remains
  inactive until the existing encounter starts. Tide Wraith's actual
  `StartBattle()` stopped feeding and dropped layer weight to zero.
- Stage 3 Play Mode: Desert Archer and Sunbound Warden looped the imported
  animation at full layer weight. Sunbound Warden's `StartBattle()` stopped
  feeding and dropped the layer to zero. Titan's fragment remained hidden
  by the existing riddle gate.
- No animation import or C# compiler errors were reported.
  The unrelated existing `Unity Remote requirements check failed` and
  Titan V017 preflight message were observed during Stage 3 Play Mode.

## Limitations / next QA

- This is an authentic imported FBX animation, but the visual appearance on
  every enemy model and Android device performance has not been manually
  approved in full gameplay on device.
- Final humanoid pose offsets may need art-direction adjustments on unusual
  rigs, particularly unusually scaled character models.
- The energy-transfer VFX and actual animation now run together; no
  resource/health drain is introduced.

## Backups, source and licensing

- Scene backups: `Temp/MagicHealAnimationBackup_20261123/`.
- All source-derived animation and controller copies are local and
  currently **uncommitted**.
- The user's prior instruction prohibits unauthorized redistribution of
  third-party packages in the public repository. No Git push was made, and
  the original Hovl, Vefects and HomebrewIK packages remain local.
- A future commit must review the Mixamo animation's applicable license
  and account for scene dependencies; do not stage vendor folders blindly.
