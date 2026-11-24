# Echo Rune Core — Stage 1 gift (2026-11-23)

**Design approved:** Option B, the Cracked Rune Core. A small indigo stone
sphere with physical fractures, cyan rune engravings, and a glowing inner
essence. The Spirit Remnant gives **a fragment of his own essence** to Van
Aert for scripted remote communication later in the story. It is **not**
one of Nevali's missing fragments, and not an AI/helper chat menu.

## Implementation

- `Assets/Scenes/SampleScene.unity`: existing **Spirit Remnant** has the
  `EchoRuneCorePresentation` component, referencing the authored model.
  The approved character, 16-line conversation and existing camera director
  are preserved.
- `Assets/Characters/SpiritRemnant/EchoRuneCore/EchoRuneCore.glb`:
  original Blender-authored mesh, imported by glTFast. Five renderers,
  seven material slots after joining rune glyphs for mobile efficiency.
- `ArtSource/Characters/SpiritRemnant/EchoRuneCore.blend`:
  editable Blender 5 source; no downloaded or vendor geometry.
- `Tools/Blender/build_echo_rune_core.py`:
  deterministic rebuild/export script, generating both files above.
- `Assets/Scripts/Story/SpiritRemnantEncounter.cs`:
  append nine core-gift lines after the original sixteen. Old save files
  with `stage1SpiritRemnantMet=true` but no core get a dedicated gift-only
  conversation; after ownership they receive the approved repeat conversation.
- `Assets/Scripts/Story/EchoRuneCorePresentation.cs`:
  materialize, hover, float toward Van, absorb and show acquired toast.
  At the gift's held beat, the model is truly parented to Van's animated
  humanoid `LeftHand` bone with `SetParent(hand, true)` and a smooth
  palm-placement blend. The default palm offset is `(0, 0.075, 0.005)`
  in hand-local units, based on the available Mixamo index-finger knuckle.
  Hand parenting is scale-compensated so the model stays `0.85` world
  units rather than ballooning with Van's 11x character scale. Both
  hand and offset remain configurable in the Unity Inspector.
  Cancellation destroys the cinematic prop and does NOT unlock the core.
- `Assets/Scripts/Story/SpiritRemnantCinematic.cs`:
  `ShowGiftBeat` adds reveal/transfer shots while using the existing
  Cinemachine camera and gameplay lock/restore implementation. The
  transfer framing favors the outside of Van's receiving hand, and its
  aim follows the animated core's position without extra per-frame
  physics occlusion sweeps.
- `Assets/Scripts/GameDatabaseManager.cs`:
  `stage1EchoRuneCoreAcquired` saved to existing JSON and SQLite player
  key-value storage. `HasEchoRuneCore()` and
  `MarkEchoRuneCoreAcquired()` expose the event to future story scripting.

## QA performed

- Blender 5.0 export succeeded, with source and model saved.
- Unity 6.3 imported the GLB via glTFast. New model/script links were
  assigned on the *actual* Spirit Remnant NPC and verified after
  scene save/reopen (`isDirty=false`).
- Play Mode: older save with Spirit already met but core not acquired
  opened nine-line gift-only dialogue. Core spawned and the
  `GiftReveal` Cinemachine shot took priority.
- Play Mode: cancelling mid-gift restored the original camera, removed
  the temporary orb, and did not change the unlock flag.
- Isolated test: new-save flow played all 25 lines via the actual
  `SpiritRemnantEncounter.Advance()` method; both first-meeting and core
  flags became true. The two flags were verified inside a **temporary JSON**
  file by redirecting the save path during the test, not touching the
  user's normal save file.
- Isolated test: an old save with the Spirit already met but no core
  played all nine gift lines, then wrote the core ownership flag into
  a second temporary JSON file. The normal save path and database object
  were restored after each isolated test.
- Existing local Unity Play Mode logs report `Mono.Data.Sqlite` native
  initialization failure, so only **JSON round-trip** is demonstrated here;
  SQLite key read/write was wired but needs environment-specific validation.
  Additional existing warnings include Unity Remote, duplicate Panel04
  menu registrations, and HUD bootstrap; no new Echo Rune C# compiler
  error was observed.
- Physical Android device, full player walkthrough, and future Cursed
  Jester communication trigger have **not** been verified/implemented.
- Hand-attachment Play Mode verification: at gift line 6 the core's
  `transform.parent` matched Van's actual `mixamorig:LeftHand`.
  The initial held test demonstrated 0.000004 units of follow error,
  retained 1.35 world units despite 11x hand scale, and reduced to
  half its size mid-absorption. Following Game View review, the held
  scale was intentionally reduced to 0.85 world units and the offset
  aligned with Van's index-knuckle direction. The final Play Mode
  test confirmed it was parented to the hand, at 0.85 world scale,
  with a palm-relative position rather than a floating fixed point.

## Next narrative event

After Cursed Jester is defeated, when Van reaches the Royal Castle
viewpoint, **Van initiates** the magical contact through the core.
The Spirit warns him of the Hollow Knight. This is designed but remains
**unimplemented** and must use story-specific trigger checks; no
freeform assistant UI or unsolicited Spirit call.

## Licensing

No third-party Hovl, Vefects, or HomebrewIK Demo files were incorporated
into the core asset. These vendor folders must remain local and must
never be committed/pushed to the public repository without permission.
