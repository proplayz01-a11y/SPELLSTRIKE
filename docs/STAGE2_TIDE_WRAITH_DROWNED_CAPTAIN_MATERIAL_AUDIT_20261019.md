# Stage 2 — Tide Wraith and Drowned Captain material audit

## Material restoration completed, 2026-10-19 (supersedes original diagnosis below)

The **original matching Tripo GLB files were recovered in local Downloads**:
`fantasy character 3d model.glb` and `fantasy pirate 3d model.glb`.
Each GLB contains the exact JPEG and matching Tripo material name referenced
by the rigged Stage 2 T-Pose FBX. Both original embedded JPEG byte ranges were
extracted, without generating replacement or approximate artwork.

| Character | Restored original texture | URP Lit material |
| --- | --- | --- |
| Tide Wraith | `Assets/Stage 2/TideWraith/Materials/fantasy+character+3d+model_basecolor.jpg` | `Assets/Stage 2/TideWraith/Materials/TideWraith_Body_URP.mat` |
| Drowned Captain | `Assets/Stage 2/Drowned Captain/Materials/fantasy+pirate+3d+model_basecolor.jpg` | `Assets/Stage 2/Drowned Captain/Materials/DrownedCaptain_Body_URP.mat` |

Both materials use the project's verified `Universal Render Pipeline/Lit`
shader, each with its *own* original Base Map, Opaque surface and neutral
Base Color. Their textures use sRGB color, mipmaps and an Android
1024-pixel max-size compressed import override.

**Importer remaps added:**

- `Assets/Stage 2/TideWraith/TideWraithUpdated/TIDE-WRAITH-FINAL@T-Pose.fbx.meta`
- `Assets/Stage 2/Drowned Captain/Animations/DROWNED-CAPTAIN@T-Pose.fbx.meta`

These remap only the visible character's existing imported body material
name to the matching new `.mat`. In addition, the two **exact production
Stage2Scene SkinnedMeshRenderer Material[0] slots** were explicitly bound
to their own new `.mat` references, so the current manually assembled
scene meshes cannot continue using their previous imported-material
subassets. The Captain's weapon material and its existing FBX remap
remain untouched. No colliders, animations or controllers were edited.

**Verification:** Unity Editor.log confirms reimport of both JPEGs, both
new materials and both T-Pose FBXs. Original GLB image checksums, URP shader
GUIDs, texture GUIDs and exact importer remap names passed static validation.
`Assets/Scenes/Stage2Scene/Stage2Scene.unity` changed **only** in:

- Tide Wraith `SkinnedMeshRenderer` fileID `4806417179843925815`,
  `m_Materials[0]` → `TideWraith_Body_URP.mat`
- Drowned Captain `SkinnedMeshRenderer` fileID
  `1343765722895505435`, `m_Materials[0]` →
  `DrownedCaptain_Body_URP.mat`

Saved Stage2 scene SHA before these two references:
`25d2db5b0ae6eaae1057572accbbc641f800dabc03fc6b1d3a6fe676eaed54c6`.
After:
`02e67b4e51ad7cb12c981cdf06a1cf87575210879508c329b7f7956f8140fb05`.
Backup scene and original importer metas, plus
`restore_manifest.json`, are in
`Library/Stage2CharacterMaterialsRestore_20261019/`.

**Pending:** visible Stage2Scene/Game View character appearance and
animation inspection, and physical Galaxy A05s render/performance testing.
Unity import success does not alone prove final skinned-mesh rendering.

**Date:** 2026-10-19  
**Original audit scope (historical, before implementation):** diagnostic / material recovery plan only. No production scene,
mesh, collider, shader, material, texture, prefab or Animator was modified.

## Finding

Both characters retain their original skinned models and authored Unity
encounter components. Their character FBX importers currently use
`materialImportMode: 2` with `externalObjects: {}`, meaning no external
`.mat` remap is configured for the imported character body materials.
The corresponding **original external texture filenames** are visible
inside the FBX source, but neither source JPG exists under the project's
`Assets` directory. These references are to a former local
`textures/packed` directory; the file's former path is not evidence that
the source is still present.

| Role | Production source model | Missing original image referenced inside FBX | Material path |
| --- | --- | --- | --- |
| Tide Wraith (Node 3) | `Assets/Stage 2/TideWraith/TideWraithUpdated/TIDE-WRAITH-FINAL@T-Pose.fbx` | `fantasy+character+3d+model_basecolor.jpg` | Model-local imported material subasset; no remap to standalone `.mat` |
| Drowned Captain (boss) | `Assets/Stage 2/Drowned Captain/Animations/DROWNED-CAPTAIN@T-Pose.fbx` (visible body's mesh/material); the prefab root uses `DROWNED CAPTAIN IDLE.fbx` | `fantasy+pirate+3d+model_basecolor.jpg` | Model-local imported body material; no remap to standalone body `.mat` |

Tide Wraith's existing skinned mesh scene renderer is
`tripo_node_efaa7f41-20cf-45f8-bec4-ccf276de6b37`, component
`4806417179843925815`, with **one material slot** sourced as fileID
`-2317486528147550585` and FBX GUID
`0ae9a87c16a3056479ba2f49242082f1`. This slot is not
`{fileID: 0}`: a material is referenced, but the external color map is
not restored.

Drowned Captain's **root** is a prefab instance from the Idle FBX (GUID
`a25f2e24bb25fd54c93d5a7a73d19e5c`), but the actual visible
**SkinnedMeshRenderer** is a scene object named
`tripo_node_05a7f5cc-20f5-4dfe-90ac-0e3b58fad0ca`, renderer
`1343765722895505435`, with one imported body-material slot
`{fileID:5800223854841290638,guid:15d19495344dec64a80bd90379d2c6d2,type:3}`.
That GUID belongs to **`DROWNED-CAPTAIN@T-Pose.fbx`**, not the Idle FBX.
Neither body importer has an external material remap. Avoid replacing
either source FBX or rebuilding the character; that risks Animator/AI wiring.

### Weapon exception: already mapped

The Captain's **separate weapon** already has
`Assets/Stage 2/Drowned Captain/WEAPON/tripo_mat_00d51b8d.mat`,
using URP Lit and Base Map
`Assets/Stage 2/Drowned Captain/WEAPON/DROWNED CAPTAIN MATERIAL.png`.
The weapon FBX `DROWNED-CAPTAIN-WEAPON.fbx.meta` has an explicit
`externalObjects` remap from the imported
`tripo_mat_00d51b8d` slot to this `.mat`. Preserve it; fixing
the Captain body should not alter weapon import settings.

## Safest restoration path in visible Unity Editor

1. **Locate the two original JPGs** from the original exported
   `textures/packed` package/source project. Do not use the Captain
   weapon atlas for the body's UVs or assume one character's basecolor
   works on the other.
2. Import them into **separate versioned directories**:
   `Assets/Stage 2/TideWraith/Materials/` and
   `Assets/Stage 2/Drowned Captain/Materials/`. Preserve texture
   import sRGB for basecolor. For A05s, consider max-size 512/1024,
   platform compression and mipmaps based on measured quality.
3. Create two external Unity Materials, e.g.
   `TideWraith_Body_URP.mat` and
   `DrownedCaptain_Body_URP.mat`, set shader to
   **Universal Render Pipeline/Lit**, and assign their corresponding
   JPG to **Base Map**. Start Opaque, smoothness low/moderate, no
   unnecessary costly transparency. Add deliberate cyan/sea-green
   emission to Tide Wraith *after* seeing the restored texture.
4. On each **visible mesh's source FBX** (Tide's
   `TIDE-WRAITH-FINAL@T-Pose.fbx` and Captain's
   `DROWNED-CAPTAIN@T-Pose.fbx`, **not** the Captain Idle root FBX
   or arbitrary animation clips), use the Inspector
   **Materials** import tab / **Remapped Materials** section to assign
   the new external `.mat` to the imported body material slot.
   Apply and wait for Unity import. Alternatively, override the
   production renderer's material element in an isolated scene
   preview first, but prefer source importer remap after approval
   because it survives prefab reinstance and avoids divergent clones.
5. Verify in the saved production Stage2Scene that body and weapon
   are independently textured; skinned mesh follows all existing
   animations; boss and normal enemy health, colliders, triggers,
   attack events, VFX and NavMesh agents remain intact.
6. PC Game View under existing URP assets followed by Galaxy A05s
   physical test. Observe pink shader/error, white/grey models,
   inverted faces, mesh UV stretch, overbright emission and frame time.

## If original JPG sources are unrecoverable

An Opaque URP/Lit **stylized fallback material** can be created to
make the models visibly colored, but it will **not restore the intended
costume/skin details** because a single flat color cannot replace the
original UV atlas. Request art-source export or retexture in Blender
before claiming final model quality. Do not generate unrelated
textures without checking that they match the model's UV layout.

## Important constraints / acceptance state

- The Stage2Scene file was already modified by the user before this
  audit. Do not reset Git or overwrite it to solve a material issue.
- The user owns the current Stage 2 collider layout; **do not touch
  MeshColliders**, animations, health scripts or unrelated material
  profiles while importing and remapping.
- Weapon material exists and is already remapped. Do **not** overwrite
  it with a body texture.
- All listed steps are a **diagnosed implementation plan**, not a claim
  that the original source images were found, Unity materials were
  created or Stage 2 Game View has passed.
