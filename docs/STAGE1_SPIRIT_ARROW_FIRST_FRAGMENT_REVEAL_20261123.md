# SPELLSTRIKE — Stage 1 Guided First Fragment Discovery

Implemented locally: 2026-11-23. **Not committed or pushed.**

## Player-facing story sequence

1. Van meets the Spirit Remnant and completes the conversation, including
   the soul-fragment feeding explanation and Echo Rune Core gift.
2. The Spirit explains that the **first fragment is near the arena center**,
   and tells Van to follow the golden trail.
3. Stage 1 reuses the **exact animated TutorialDirectionalArrowTrail** script,
   sprite, gold color, scale, spacing, and travel speed. It points toward
   the Node 1 shrine/arena on the first leg.
4. On entering the Node 1 arena after the Spirit gift, a one-time
   **Cinemachine first-fragment reveal** takes priority over normal gameplay:
   it establishes the arena, shows Bramble Sprite draining energy from
   Nevali's blue shard, then reveals Van's reaction in captions.
5. Once the cinematic ends (or the player skips it), the existing
   **Node1Controller.TriggerEncounter()** begins the approved MITIGATE
   vocabulary teaching and barrier-gated battle sequence. Bramble's dormant
   feeding presentation runs until the vocabulary barrier is broken.

## Van's approved emotional reveal — added Nov 23

The first fragment is not merely a collectible. The user's approved emotional
direction is **shock -> heartbreak -> restrained fury**, while saving Van's
first explosive outburst for the later Hollow Knight confrontation.

The existing `Stage1FirstFragmentReveal` now plays these exact scripted
subtitle lines, synchronized with real Cinemachine camera beats:

| Beat | Camera direction | Subtitle (Van Aert) |
| --- | --- | --- |
| Establishing | Arena and Bramble feeding beside the shard | THE FIRST FRAGMENT |
| Recognition | Crystal close-up | “Nevali...? Is that... you?” |
| Heartbreak | Bramble draining magical energy from the fragment | “You're feeding on my daughter's soul...” |
| Controlled anger | Front-side close-up of Van's face | “Get your hands off her.” |
| Silence | Slow push in toward Van, no dialogue | [1.4 seconds of silence] |

- Typewriter subtitle reveal with the **VAN AERT** speaker name and
  minimum reading holds; each beat uses its own timing, editable in the
  component's Inspector.
- Smooth internal Cinemachine camera translations between individual shots.
- Van temporarily faces the fragment during the reveal. His original
  orientation, movement, attack locks, HUD and gameplay camera are restored.
- The close-up is filmed from the **opposite shoulder** so his sapphire
  staff does not obscure his eyes. This angle was compared directly in the
  actual Unity Game View and visibly improved face readability.
- A short final silent push-in before the original MITIGATE encounter.
- The existing PC Escape and mobile Skip controls still finish the sequence.
- These are **on-screen subtitles**. No voice-over or lip-sync has been added.

The Hollow Knight is **not** treated as an external feeding-fragment target;
the boss fragment is embedded inside him, and the arrow directs Van to
the authored castle approach when Nodes 1–3 are complete.

## What changed

- `Assets/Scripts/Story/Stage1FragmentGuidance.cs`
  - Uses the original `TutorialDirectionalArrowTrail` component.
  - Only visible after `HasMetSpiritRemnant()` and `HasEchoRuneCore()`.
  - Hidden during Spirit dialogue and the first-fragment cinematic.
  - Disappears within ~52 world units of its current destination so it
    does not clutter combat. Follows progression to Node 2, Node 3, and
    finally the Hollow Knight encounter.
- `Assets/Scripts/Story/Stage1FirstFragmentReveal.cs`
  - Separate temporary high-priority `CinemachineCamera`; the gameplay
    Main Camera and FreeLook rig are not replaced.
  - Three staged shots, cinematic captions, PC Escape and touch Skip,
    camera-brain blend restoration, HUD hide/restore, and player movement/
    attack locks with clean release.
  - Shares the existing Stage 1 scaled-world geometry; shots are
    deterministic regardless of approach direction.
  - Saves a one-time reveal milestone only after full completion or skip.
  - Includes editor-only preview mode that never writes the story flag or
    launches the vocabulary encounter.
- `Assets/Scripts/ArenaScripts/ShrineTrigger.cs`
  - First grants reveal ownership when the Spirit meeting/gift is complete.
  - Defers `Node1Controller.TriggerEncounter()` until the reveal is over.
  - `OnTriggerStay` allows a player already inside the large arena collider
    to progress after finishing the Spirit interaction.
- `Assets/Scripts/GameDatabaseManager.cs`
  - Adds `stage1FirstFragmentRevealed`, saved/restored through both the
    existing JSON and SQLite player-key methods.
- `Assets/Scripts/PlayerPersistence/PersistentThirdPersonCamera.cs`
  - The gameplay camera obstacle guard now also yields while the
    first-fragment cinematic is active, as it already does for the Spirit.
- `Assets/Scripts/Story/SpiritRemnantEncounter.cs`
  - Adds two dialogue lines about the arena/golden trail after the soul
    feeding revelation; retains the original 16 Inspector-authored lines.
  - The Echo Rune gift dialogue also points old-save players toward
    the arena; repeat dialogue reminds them about the trail.
- `Assets/Editor/Stage1FragmentStoryInstaller.cs`
  - Idempotent scene installer, copies the **actual serialized tutorial arrow
    settings**, and binds the existing Stage 1 NPC, arena, fragment,
    Bramble Sprite, next two fragments, progression manager, and castle.
- `Assets/Scenes/SampleScene.unity` includes the authored scene references.

## Scene locations checked

- `Node1_ShrineTrigger`: approximately (1205.88, -111.57, 517.10)
- First fragment `Icosphere`: approximately (1207.90, -114.99, 515.80)
- Bramble Sprite: near (1219.41, -125.47, 523.25)
- Stage 1 routes: `Node1_ShrineTrigger` ->
  `Icosphere.004` -> `Icosphere.003` ->
  `HollowKnight_TriggerEncounter`
- Arrow source: `Assets/Tutorial Assets/Navigation/DirectionalArrow.png`

## QA performed

- Unity 6.3 compiled with zero new C# errors.
- Stage 1 scene saved without dirty state. Serialized references, sprite,
  original tutorial arrow spacing/speed, and targets independently read back.
- Route resolution was checked for all four progression states. Temporary
  editor-side test values were reverted.
- Play Mode editor-only preview successfully activated the new Cinemachine
  camera and took priority over gameplay. After allowing Cinemachine's blend
  to finish, the Game View showed **Bramble Sprite alongside Nevali's blue
  fragment** in a readable arena shot, with the magical feeding energy
  crossing between them.
- The Play Mode camera guard bypass worked; the primary camera matched the
  temporary Cinemachine camera during the reveal.
- Van's third line was visibly verified in Game View, with readable dialogue
  and a clear, unobstructed facial close-up. The editor Play Mode tested the
  shot progression and transition into the existing Node 1 vocabulary UI.
- An Editor-only preview now marks itself consumed for the *current Play
  Mode session*, preventing the large arena's `OnTriggerStay` from replaying
  a real encounter after a no-save test.
- Preview Skip cleanup was directly verified: cinematic camera and UI
  destroyed, movement unlocked, player original facing restored, and saved
  reveal flag remained false.
- Local save was backed up at the start of this emotional-dialogue pass at
  `Temp/Stage1VanEmotionalRevealBackup_20261123/
  spellstrike_save.before_emotion_QA.json`. After preview tests the only
  serialized difference was the extra (false/default) reveal flag. The
  **exact pre-test JSON file was restored**, and SHA-256 equality verified:
  `DDFAF79F124196EC2C8F364B626BE7FDBB7847BCED3621E66B3005D27DF27086`.
  A copy of the changed post-preview version was retained for audit.
- The full player-driven Spirit -> arrows -> reveal -> vocabulary sequence
  and Android touch Skip have **not** received a complete on-device
  walkthrough. Do not claim shipping QA is finished.

## Save-data QA incident and recovery

One initial QA test set an in-memory `GameData` to a blank test profile.
When its normal completion callback fired it wrote that temporary profile
to the local JSON save. A separate QA snapshot was kept at
`Temp/Stage1FragmentDiscoveryBackup_20261123/spellstrike_save.qa_modified.json`.
The save was restored from an existing, **older 2026-11-23 2:32 PM** snapshot
at `Temp/SpiritRemnantFullReplayBackup_20261123/
spellstrike_save.before_full_replay.json`. That earlier snapshot holds
1,271 word-history entries, stage-unlock level 2 and tutorial progress.
It may not contain changes made after 2:32 PM; those could not be verified
from a more recent backup. A subsequent editor-only preview mode was added
so future camera QA never explicitly marks story progression.
Another normal Play Mode session also modified the local save; after testing,
the same older snapshot was restored, with a SHA-256 match verified.

## Backups / repository safety

Stage 1 source/scene backup:
`Temp/Stage1FragmentDiscoveryBackup_20261123/`.
All existing Stage 2–4 work, Spirit Remnant rig/dialogue/cinematic, Bramble
combat, vocabulary gates, and existing Hovl fragment VFX remain intact.
Do **not** commit or redistribute local unlicensed third-party Hovl,
Vefects, or HomebrewIK packages. No Git push for this feature.
