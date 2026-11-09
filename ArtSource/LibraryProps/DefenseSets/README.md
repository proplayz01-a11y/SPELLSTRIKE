# SPELLSTRIKE: Prologue Defense 3D Environment Prototypes

Date: 2026-10-23. These are low-poly **actual Blender 3D assets**, not final art.
They are designed for rapid defense-room cinematic blocking. No Rodin
credits, no game scenes and no existing Blender library source overwritten.

## Completed editable sets

All sources are in ArtSource/LibraryProps/DefenseSets.
All GLB exports are in Assets/Cinematics/Storyboard3D/Environment/DefenseSets.

| Panel | Source Blender project | Unity-ready GLB |
| --- | --- | --- |
| 01, peaceful Van/Nevali home | Prologue_Home_01_Peaceful_v01.blend | Prologue_Home_01_Peaceful_v01.glb |
| 02–03, Great War | Prologue_GreatWar_02_03_v01.blend | Prologue_GreatWar_02_03_v01.glb |
| 04, destroyed same home | Prologue_Home_04_Ruined_v01.blend | Prologue_Home_04_Ruined_v01.glb |

The home has the entry doorway, oak floor, amber glass, rugs, toy blocks,
chairs, tables and shelves. The ruined version preserves the same footprint
and color-block continuity but replaces walls with broken segments and adds
burnt beams, rubble, toys and Nevali's blue ribbon. The battlefield features
burned building silhouettes, crumbled masonry, violet Broken Language
proxies, a *placeholder* faceless threat and separate A–G word tile meshes.

The editable Blender scenes include named ANCHOR objects for intended Van,
Nevali and grief performers. **The characters themselves are not modeled
or animated in these environment outputs.** A true young, dark-haired Van
and Nevali rig are separate prerequisites, as is mocap for the toy-playing
action and grief reaction.

## Repeatable Blender operations

Blender 5.0.1 headless, project-root relative:

- Build: python script ArtSource/LibraryProps/DefenseSets/build_prologue_defense_sets_v01.py
- Reimport QA: python script ArtSource/LibraryProps/DefenseSets/validate_prologue_defense_sets_v01.py
- Evidence: Library/StoryboardDefenseSets_20261023 contains three image
  previews and two machine-readable JSON QA reports.

Rebuilding intentionally overwrites the generated Blender and GLB
prototype files. Protect any manually edited variants before rerunning.

## Unity import

When Unity's Editor GUI is healthy, first save the current open scene.
Then run **SPELLSTRIKE > Cinematics > Defense Prototype >
1 Import 3 Blender Environments** in Edit Mode.

The guarded visible Editor command is implemented in
Assets/Editor/Cinematics/PrologueDefenseEnvironmentSceneBuilder.cs. It will
create three NEW scene files without modifying any existing scene:

1. Assets/Scenes/Cinematics/Prologue_P01_FamilyHome_Defense_v01.unity
2. Assets/Scenes/Cinematics/Prologue_P02_P03_GreatWar_Defense_v01.unity
3. Assets/Scenes/Cinematics/Prologue_P04_RuinedHome_Defense_v01.unity

Each scene imports the Blender GLB with glTFast, gets a reference camera,
temporary light and basic blocking-floor collider, and remains outside Build
Settings. The previous clean Editor scene is restored afterward.

**Unity integration COMPLETE (2026-10-23):** The guarded importer was successfully
executed through the connected Unity Editor bridge and all three scene files
listed above were created and saved. The initial GUI window-handle problem was
bypassed without restarting the Editor. Each scene was loaded in Unity and a
real Game View PNG captured in Library/StoryboardDefenseSets_20261023:
Unity_P01_Home_GameView.png, Unity_P02P03_War_GameView.png and
Unity_P04_RuinedHome_GameView.png. Read-only Unity inspection PASS: 121, 148
and 120 meshes; 1,516, 2,787 and 1,888 triangles; zero missing scripts and
zero null material slots. The original StudyArchive scene was restored clean.

**Still prototype:** The actor rigs, narration and animation are not included
in these scenery scenes. Ruined-home Unity preview lighting is dark; an
optional guarded fill-light helper was authored but has NOT yet been applied.

## Cinematic camera and actor rules

Use the locked panel A01–A04 camera descriptions in
docs/STORYBOARD_CINEMATIC_CAMERA_DIRECTION_LOCK_20261023.md.
Narration carries exposition but real characters must eventually animate.
For custom Nevali/toy and book contact performances, use the user's installed
Mixamo LLM Mocap with a suitable locked-camera reference plate, then retarget
and film with independently animated Unity cameras.

Keep the old StoryScene image prologue working as fallback. Do not alter
the Stage 5 or tutorial scene just to demonstrate these standalone sets.
