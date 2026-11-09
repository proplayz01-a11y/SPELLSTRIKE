# Young Van Aert and Nevali: defense prototype characters

Date: 2026-10-23. Both characters are real editable Blender sources and
skinned glTF exports, but are not approved final sculpts or proven Mixamo
LLM Mocap retarget targets.

## Model files

- Young Van source: ArtSource/LibraryProps/DefenseCharacters/YoungVanAert_Prototype_v01.blend
- Young Van Unity GLB: Assets/Cinematics/Storyboard3D/Characters/DefensePrototypes/YoungVanAert_Prototype_v01.glb
- Nevali source: ArtSource/LibraryProps/DefenseCharacters/Nevali_Prototype_v01.blend
- Nevali Unity GLB: Assets/Cinematics/Storyboard3D/Characters/DefensePrototypes/Nevali_Prototype_v01.glb

Young Van has black hair, clean-shaven face, navy/gold wizard robe, and
boots. Nevali is smaller, with chestnut hair, two blue ribbons, a cream
dress, blue bodice and boots. Each GLB has one armature with 18 bones,
skinned low-poly body parts, and one exported blocking animation:
YoungVan_WatchingDaughter_Idle_v01 / Nevali_ReachingPlayingToyBlocks_v01.
These loops are basic posing, not contact-perfect hand/mocap motion.

Preview PNGs and independent Blender GLB re-import verification:
Library/StoryboardDefenseCharacters_20261023/

Regenerate only after backing up manual edits:
ArtSource/LibraryProps/DefenseCharacters/build_young_van_nevali_v01.py

Verify roundtrip:
ArtSource/LibraryProps/DefenseCharacters/validate_defense_characters_v01.py

## Unity placement

The visible Editor action
SPELLSTRIKE > Cinematics > Defense Prototype > 3 Place Young Van and Nevali
is prepared at Assets/Editor/Cinematics/PrologueP01CharacterBlockingBuilder.cs.
It targets the isolated Prologue_P01_FamilyHome_Defense_v01.unity scene only,
requires that scene open and saved, and creates a unique backup before
placing two actor instances. It refuses repeated operations.

**Not yet executed/verified:** Unity currently reports GUI object handle
exhaustion and the scene remains without these two characters. Do not
claim working Unity character animation until imported hierarchy, scale,
camera, motion and toy contact are visually checked in Game View.

Never replace production Older Van or his gameplay prefab with this
young-era placeholder. These assets are meant for early prologue panels,
not Panels 15-18.
