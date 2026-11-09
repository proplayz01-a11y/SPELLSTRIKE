# Nevali v12 — Mixamo auto-rig upload package

**Status:** Blender FBX export + independent FBX reimport QA passed.
**Not yet rigged by Mixamo.** Current FBX contains one unrigged,
single-object character mesh in an arms-extended T-pose.

## File for Mixamo

UPLOAD: Nevali_v12_MixamoUpload_UNRIGGED.fbx
WEBSITE: https://www.mixamo.com/

Do not upload the GLB and do not upload the pre-rig Blender project.
FBX contains embedded diffuse/normal images and 7 material slots.
Original source, hair, facial sculpt and costume are preserved.

## Auto-rig steps

1. Log in with Adobe ID, choose Upload Character, upload the FBX.
2. Visually verify the head, eyes, ribbons, dress and T-pose.
3. Place left/right wrist, elbow, knee and groin markers at anatomical joints, not skirt fabric. Choose Standard Skeleton if offered.
4. Run auto-rigger and inspect arm, leg, skirt and hair bending in preview.
5. Download FBX Binary, With Skin (T-pose/static if offered). Preserve the unrigged original upload.
6. Bring rigged FBX into Blender to verify armature hierarchy and bone weights before attaching motion.

IMPORTANT: The original v12 mesh has detached hair/clothing pieces and a
long skirt. Joining them into one mesh OBJECT does not fuse all vertices or
guarantee clean automated weights. Some parts may need manual weighting.

## Independent QA

See Nevali_v12_MixamoUpload_Validation.json.
41 source mesh objects became one single FBX mesh object without polygon
loss (118,962 polygons), seven material slots, 1.8913m physical height and
a ground-aligned origin. The binary FBX reimported into Blender; actual
embedded image bytes and reimported image nodes were verified.

Nevali_v12_MixamoUpload_PRE_RIG.blend is an editable backup, not an upload.

## After rigging

The eight-second child-actor Google Flow plate is already staged at:

C:\Users\David\Documents\Codex\MocapLab\mixamo-llm-mocap\plates\nevali_blockstack_flow_20261023\nevali_blockstack_source.mp4

The GVHMR extraction runner is ready but full estimation has been blocked
by insufficient free RAM and GPU VRAM. Do not close unsaved Blender/Unity
sessions automatically. After resource cleanup, estimate SMPL-X then
retarget to the correctly skinned Mixamo FBX. Child's cross-legged stance
and finger-to-block contact require manual correction during final QA.
