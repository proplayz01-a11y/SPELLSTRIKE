# SPELLSTRIKE 3D Storyboard Cinematic Workbench

## Isolated scene

Open `Assets/Scenes/Cinematics/Storyboard3D_CinematicWorkbench.unity`.

This scene is a **separate 3D cinematic production workspace**, not the
existing comic page-based `StoryScene` and not a playable game stage.
It is **not** in Build Settings and is not connected to progression.

## Current stage of production

- `LOCATION_01_LIBRARY_HALL_BLOCKOUT`: simple Unity cube geometry indicating
  an enclosed library hall, shelves, columns, desk and a conversation table.
  Everything named `Blockout` is a placeholder, not final Rodin art.
- `LOCATION_02_LIBRARY_CORRIDOR_STAGING`: inactive empty slot for the corridor
  from the existing storyboard. It can be activated after layout/blockout.
- `LOCATION_03_BATTLEFIELD_STAGING`: inactive empty slot for the battlefield
  from the storyboard. Verify its composition from the original panels first.
- `_RODIN_..._IMPORTS`: place separate imported environmental modules here,
  then hide/release the corresponding blockout geometry when approved.
- `_LOD_...`: organizational roots only. Actual Unity LODGroup components
  must be created once LOD0/1/2 versions of real environment meshes exist.
- `_CHARACTER_STAGING_NO_GAMEPLAY_PERSISTENCE`: empty pose markers for Van Aert,
  Vander Poel and Nevali. Use non-gameplay character models/Animator instances
  for Timeline, **not** the DontDestroyOnLoad gameplay player prefab.
- `Main_CinematicCamera_ANIMATE_IN_TIMELINE`: the active main camera, ready for
  camera animation/shot binding. Initial framing points into the library hall.
- `Timeline_Director_ASSIGN_PLAYABLE_ASSET_LATER`: an actual PlayableDirector
  component with no PlayableAsset yet. **Timeline clips, tracks, actor bindings
  and voice are not authored** at this stage.
- `_SHOT_BLOCKING_MATCH_STORYBOARD`: temporary shot markers only. Their order,
  positions and number are to be approved against the storyboard panels.
- `_VOICE_LINE_AUDIO_CLIPS_DROP_HERE`: voice assets belong in a separate
  directory; Timeline Audio Tracks can link to them later.

## Next workflow

1. Inventory the original storyboard panels, their camera compositions,
   story beats, unique locations and actor appearances.
2. Block out each required location and approve rough camera shot framing.
   Reuse the same 3D room for multiple angles rather than generating duplicates.
3. Generate **environment** modules in Rodin; clean, UV, retopologize or
   decimate in Blender; import them as independently replaceable meshes.
4. Create real LODGroups when LOD meshes are available; control camera
   visibility, shadow casters, texture sizes and lighting. LOD placeholders
   here do not improve performance by themselves.
5. Add a Timeline PlayableAsset to the existing Director; author camera,
   animation and voice/audio tracks. Use Mixamo/mocap clips on separate
   cinematic actors and synchronize them to dialogue.
6. Verify scene and timeline first on PC, then the physical Galaxy A05s.

The four simple URP blockout materials in `Materials` are editable visual
references only. No finished cutscene, final animation, dialogue, Rodin meshes,
occlusion bake or mobile frame-rate claims exist yet.
