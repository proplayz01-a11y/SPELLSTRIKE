# SPELLSTRIKE Gothic Gateway: Study to Rear Archive

## Source of truth

The storyboard's central stone arch in approved/finalized Panels 16 and 34.
Also use docs/LIBRARY_ENVIRONMENT_BIBLE.md and the canonical top-down layout.
The gateway itself is one open passage from front study to rear archive,
not a second standalone room. Grey masonry voussoirs, dark-blue inset rib,
two blue/gold columns and small aged-gold accents.

## Outputs

- Editable Blender assembly: ArtSource/LibraryProps/GothicGateway/StudyArchive_GothicGateway_v01.blend
- Standalone detailed GLB: Assets/Cinematics/Storyboard3D/Environment/Architecture/GothicGateway/StudyArchive_GothicGateway_v01.glb
- Standalone medium-distance GLB: StudyArchive_GothicGateway_v01_LOD1.glb
- Standalone far/mobile GLB: StudyArchive_GothicGateway_v01_LOD2.glb
- Front / side / back / LOD2 proof renders: Library/StoryboardGatewayPreview_20261022/
- Deterministic build script: build_library_gateway_v01.py
- Round-trip validation: validate_library_gateway_v01.py

Created in Blender 5.0.1 with zero Rodin credits.

## Dimensions and ownership

- At floor center (0,0,0), facing Blender -Y. Blender +Z up; GLB handles Unity
  coordinate conversion. 1 Blender unit = 1 meter.
- 3.56 m nominal clear opening width at the arch spring.
- Inner arch apex height 4.1 m. Outer stone band is taller. These are
  draft dimensions until tested with Vander's actual cinematic actor.
- Individual stone blocks, gold trims, and paired blue columns remain editable
  in the source .blend; the GLB joins material-identical pieces to 8 meshes.
- Four named placement-hint Empties: two GuardianStatue and two WallTorch.
  They are location hints, not substitute meshes. Adjust to real statues'
  pedestal widths and the surrounding wall once the one-building set is built.
- No red carpet, statues, torches, surrounding wall sections or archive doors
  baked into this asset.
- The storyboard files, existing Unity scenes, prefabs and game scripts
  were not edited.

## Unity to-do

1. Import GLB with the project's standard glTF pipeline, convert materials
   to URP compatible settings and inspect both study-facing and archive-facing
   sides.
2. Position at the single central transition of the top-down map.
3. Check Vander and Van can walk through: no automatic collision is
   included. Use simple pillar/arch colliders that leave the hole open.
4. Create a Unity prefab and an actual LODGroup with the three GLBs; names
   alone do not create LODGroup behavior.
5. Instantiate the independent two Guardian Statues and two higher/outward
   Wall Torches relative to hint transforms. No flames are fused into arch.
6. Compare real 3D shots with 05/16/34 and adjust placement or size if needed.

No Unity Editor or Android rendering was performed in this standalone build.
LOD savings are geometric measurements, not a claim about measured FPS.
