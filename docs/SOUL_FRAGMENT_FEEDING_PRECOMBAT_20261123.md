# SPELLSTRIKE — Enemies Feeding on Nevali's Fragments

Authoring date: 2026-11-23

## Story and animation specification

Reference: user's Mixamo **Magic Heal on Y Bot** screenshot. The intended
moment is *pre-battle*: each enemy reaches toward the fragment, quietly
absorbing the energy contained in that piece of Nevali's shattered soul.
**Exception:** Hollow Knight contains Nevali's fragment **embedded inside
his body**. He does not perform the external pre-battle feeding ritual.
The ritual continues **uninterrupted** until the player's existing battle
trigger actually begins combat; then the energy line, travelling motes,
and reaching pose stop so the enemy's approved combat animation takes over.

**Update later on 2026-11-23:** The user supplied
`Y Bot@Magic Heal.fbx`, and the authentic 2.67-second looping Humanoid clip
was initially imported and retargeted onto ten humanoid enemy Animator layers in
Stages 1–3. See `docs/MAGIC_HEAL_MIXAMO_IMPORT_20261123.md`. The procedural
arm extension below is now only the optional fallback when the clip/layer
is unavailable. After Hollow Knight's embedded-fragment exception, **nine**
humanoid enemies retain the imported loop.

## Spirit Remnant's explanation

The first encounter retains the original, approved sixteen Inspector-authored
dialogue beats and adds six **THE SOUL THEY ARE STEALING** lines, before
the nine-line Echo Rune Core gift. Van learns that enemies draw strength
from the light imprisoned in Nevali's fragments, and declares he will break
their hold. The approved original Spirit dialogue is not rewritten or removed.

First encounter now **31 lines** (16 original + 6 revelation + 9 gift).
The repeat conversation adds a reminder about the feeding danger.
Story completion flags are still awarded **only at the end** of the full
conversation. Canceling still awards no progression.

## Unity implementation

- Runtime component:
  `Assets/Scripts/Story/SoulFragmentFeeding.cs`
- Backed-up stage installer:
  `Assets/Editor/SoulFragmentFeedingInstaller.cs`
- Stage scenes changed:
  `Assets/Scenes/SampleScene.unity`,
  `Assets/Scenes/Stage2Scene/Stage2Scene.unity`,
  `Assets/Scenes/Stage3Scene/Stage3Scene.unity`

An `EnemyHealth` actor is linked to its closest unique fragment by
horizontal world distance. This relationship was inspected and matched
unambiguously for the three stages. Initially twelve actor–fragment pairs
were wired; **Hollow Knight has now been removed**, leaving **eleven**:
Stage 1 has three, Stage 2 four, and Stage 3 four. His embedded boss fragment
is reserved by the installer and is not assigned to another enemy.

All changes are visual: existing AI scripts, actual `StartBattle()`
methods, attack authority, `EnemyHealth`, `FragmentPickup`, collider
placement, NavMesh agents, vocabulary gates, boss unlock events and
save-state keys are unchanged. The new component reads the existing
controller's `battleStarted` bool via a one-time cached reflection
binding and immediately stops its presentation when combat actually starts.

### Animation

Humanoid enemies receive a procedural right-arm reaching pose toward their
assigned fragment. The arm moves subtly as though drawing energy, and the
enemy faces the shard while not in combat. Rotations are applied after
the base Animator each frame without changing Animator Controllers or
underlying clips. No procedural pose is applied after combat activates.

The Stone Golem and Titan do **not** receive a forced humanoid arm pose.
The Titan's existing dormant statue, fourth-riddle awakening and
hidden/unlocked fragment gating are preserved. Its soul energy visual
will only be visible while the actual fragment is active.

**Right-hand standard (2026-11-23):** Every configured fragment-feeding
enemy now receives energy into its **right hand**. Humanoid enemies use
`HumanBodyBones.RightHand`; Stone Golem and Titan use the existing
`mixamorig:RightHand` transform without changing their animation or
awakening logic. The obsolete scene-level `reachWithLeftHand` flag was
retired, and the installer no longer adds it.

Right-hand runtime QA: Stage 1 Bramble Sprite, Stone Sentinel, and Cursed
Jester (3); Stage 2 Barnacle Husk, Tide Wraith, Drowned Captain, and a
temporarily activated Corsair Phantom (4); Stage 3 Sunbound Warden,
Desert Archer, and Stone Golem (3). All ten active beams finished at
the right-hand bone (endpoint distance approximately zero). Titan also
resolved its right hand, but remained without a beam while the fragment
was hidden. Bramble and Sunbound Warden stopped feeding at actual battle
start. Hollow Knight remains excluded: his fragment is embedded.

The three scenes were saved, and the local game save SHA-256 was unchanged
after QA. Backups: `Temp/SoulFeedingRightHandStandard_20261123/`.
No third-party redistribution, commit, or Git push occurred.

### Magical energy effect

Each active ritual creates only runtime GameObjects, with no new
third-party meshes or textures: two colored, curved energy-stream
`LineRenderer` objects and **four travelling light motes**.
They visibly move **from the fragment toward the enemy** until battle
interruption, with shard-matched cyan/purple/violet colors. These
are not gameplay drains; no health or soul resource is altered.
VFX are destroyed when combat starts or the actor is disabled.
No Hovl/Vefects/HomebrewIK assets were added or redistributed by this work.

## QA performed

- Unity 6.3 C# compilation passed. A transient local-name compiler
  error introduced during the mote pass was corrected and recompiled.
- Initial Edit Mode: 4 feeding components in Stage 1, 4 in Stage 2,
  4 in Stage 3; revised Stage 1 scene has three, after Hollow Knight was
  excluded. Scenes saved cleanly.
- Stage 1 Play Mode before the correction: all four active enemy actors
  showed feeding; the
  components resolved their humanoid left-hand bone and created
  two runtime energy renderers. Stone Sentinel's real `StartBattle()`
  set its combat state and automatically interrupted feeding and cleared VFX.
- Stage 2 Play Mode: Barnacle Husk, Tide Wraith and Drowned Captain had
  live ritual visuals. The Corsair Phantom actor remains inactive until its
  original encounter activates; its component is installed but not visible
  before that activation. Tide Wraith's `StartBattle()` interrupted feeding.
- Stage 3 Play Mode: Sunbound Warden and Desert Archer channeled while
  idle, Stone Golem showed the energy connection without a humanoid pose.
  The Titan's fragment was not active, so its energy effect was correctly
  suppressed. The Warden's real `StartBattle()` interrupted the ritual.
- After the moving-mote revision, Stage 3's Desert Archer produced **two
  LineRenderers and four 3D motes**. The motes changed position between
  runtime snapshots at 54.7s and 65.1s, travelling along the channel.
- Isolated first-meeting dialogue test used a temporary in-memory
  `GameData`: 31 lines, revelation starts at line index 16, gift starts
  at index 22, and cancellation awarded no progress. The actual user
  saved-data object was restored, unchanged.

### Remaining verification

- The exact Mixamo Magic Heal FBX was subsequently supplied and integrated;
  see `docs/MAGIC_HEAL_MIXAMO_IMPORT_20261123.md`.
- Not every actor is visible before its original scripted activation
  (notably the Stage 2 Corsair Phantom). We deliberately did not override
  its authored active state or enemy targeting gates.
- The Titan's fourth-riddle statue and fragment hide/reveal need a
  separate story-specific ritual shot if desired.
- Stage 4's convergence encounters are **not** automatically assigned to
  fragments (only one legacy boss pickup is present for multiple enemies).
- Full manual player-path regression, every boss transition, Android device
  testing and performance profiling are pending.

## Backups / Git safety

Backup folder: `Temp/SoulFragmentFeedingBackup_20261123/`. Existing
changes from other SPELLSTRIKE sessions were preserved. No Git commit or
push was requested. Do not commit, push or redistribute the third-party
Hovl Studio, Vefects or HomebrewIK vendor asset folders to public GitHub.
