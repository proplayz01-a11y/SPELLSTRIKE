# Desert archer — Mixamo files

Upload **S3N2_DesertArcher_Mixamo.fbx** directly to Mixamo. It contains the unrigged character, empty hollow quiver, three materials, and embedded base-color image. No camera, lights, hidden original, rig, or animation is exported.

The ZIP is a delivery bundle: extract it and upload the FBX. The package has not been uploaded to or auto-rigged by Mixamo.

- `Textures/DesertArcher_BaseColor.jpg`: original 4096 x 4096 source atlas, extracted without resizing or repainting.
- `DesertArcher_Materials.blend`: Blender material library. File > Append > this file > Material to reuse the three materials. Its image is packed as well.
- `Material_Settings.json`: exact source material values for recreating assignments if needed.
- Body and quiver exterior share the textured character material. Quiver interior and rim use plain dark/worn leather colors; they do not have separate image textures.
- No separate normal, roughness, or metallic image maps exist in this source; none have been fabricated.

Blender material datablocks are not Unity `.mat` files. FBX carries exportable material information; advanced renderer settings may require reassignment after rigging/import. Preserve this texture and material library as the source for that step.

The editable character source remains separate and unchanged. Do not assume rigging/deformation is validated until the uploaded character has been checked in Mixamo and in the target game.

Adobe's supported formats and embedded-media instructions: https://helpx.adobe.com/creative-cloud/help/mixamo-rigging-animation.html
