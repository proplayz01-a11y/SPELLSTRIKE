# SPELLSTRIKE — Right-hand fragment-feeding standard

## User correction

All enemies that participate in an *external fragment-feeding ritual* must
receive Nevali's soul energy at the **right hand**. Bramble Sprite is the
reference, but this is **not a Bramble-only customization**.

Hollow Knight remains excluded because Nevali's fragment is embedded in
his body. Other encounter activation/fragment visibility rules are unchanged.

## Code

- `Assets/Scripts/Story/SoulFragmentFeeding.cs` no longer uses the old
  `reachWithLeftHand` toggle. Every Humanoid feeding actor resolves
  `RightUpperArm`, `RightLowerArm`, and `RightHand`.
- Non-Humanoid enemies resolve an exact right-hand bone in their skeleton
  (e.g. `mixamorig:RightHand` on Stage 3 Stone Golem/Titan), rather than
  redirecting the energy to the chest. If a right-hand bone is genuinely
  absent, a right-side offset is used with an explicit warning.
- The existing visual-only energy curve and moving motes now end at that
  right-hand point without changing combat authority, enemy health, boss
  activation, soul fragments, or Magic Heal clip/loop timing.
- During regression QA, a Tide Wraith battle had initially left its
  feeding beam alive. The component now caches **all** local
  `battleStarted`/equivalent bool sources instead of binding only the first
  matching field, and interrupts the ritual as soon as any source starts
  combat. No enemy combat controller was edited.
- `Assets/Editor/SoulFragmentFeedingInstaller.cs` no longer writes the old
  left-hand field when creating or updating enemy bindings.

## Verification

- After script reload, Unity compiled without new errors.
- Stage 1 Play Mode: Bramble Sprite, Stone Sentinel, and Cursed Jester
  all fed into `mixamorig:RightHand`, with the actual last energy-line
  point matching the hand exactly (`0.0000` world units).
- Stage 2 Play Mode: Tide Wraith, Barnacle Husk, and Drowned Captain
  all fed into `mixamorig:RightHand` with `0.0000` endpoint error.
  Corsair Phantom's actor remained inactive according to its existing
  encounter gate; its rig **does** have a `mixamorig:RightHand` bone.
- Stage 2 combat follow-up: calling Tide Wraith's genuine `StartBattle()`
  set its `BattleStarted` state, the feeding component detected it,
  `IsFeeding` became false, and the runtime beam was destroyed on the
  next frame.
- Stage 3 Play Mode: `S3N3_DesertArcher` and `S3N2_StoneGolem` both had
  live energy lines terminating exactly at `mixamorig:RightHand`
  (`0.0000` world-unit endpoint discrepancy).
- Stage 3 Titan resolved `mixamorig:RightHand`, but did not spawn its
  channel because its original fragment/encounter progression was not in
  the feeding state; this behavior was deliberately preserved.
- Stage 3 first enemy also resolved `mixamorig:RightHand`, but had stopped
  feeding by the time of the line-endpoint check.
- Still pending: Corsair Phantom's full activation-to-battle playthrough,
  Titan's post-riddle fragment activation, individual close-up visual
  framing on all enemies, and Android hardware tests.

## Safety

- Backups in `Temp/RightHandFeedingStandardBackup_20261123/`.
- Existing dirty/source files, scenes, animation clips, and third-party
  local vendor packages were not reset, deleted, or published.
- The local `spellstrike_save.json` was hash-verified unchanged after QA.
- Unity was returned to Stage 1 `SampleScene.unity` in Edit Mode with
  no unsaved scene changes.
- No Git commit or push.
