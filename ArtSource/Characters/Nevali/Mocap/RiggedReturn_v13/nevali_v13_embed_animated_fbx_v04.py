"""Fix missing texture payload in the v03 animated FBX export.

Animation/geometry unchanged; create separately versioned packed v04
Blend + FBX without touching the v13 source or previous motion outputs.
"""
import bpy
from pathlib import Path

ROOT = Path(__file__).resolve().parent
SRC_BLEND = ROOT / "Nevali_v13_MediaPipe_BlockPlay_Rough_v03.blend"
OUT_BLEND = ROOT / "Nevali_v13_MediaPipe_BlockPlay_Rough_v04.blend"
OUT_FBX = ROOT / "Nevali_v13_MediaPipe_BlockPlay_Rough_v04.fbx"


def main():
    if not SRC_BLEND.exists():
        raise FileNotFoundError(SRC_BLEND)
    if OUT_BLEND.exists() or OUT_FBX.exists():
        raise FileExistsError("v04 animation asset already exists")
    bpy.ops.wm.open_mainfile(filepath=str(SRC_BLEND))
    mats = list(bpy.data.materials)
    image_nodes = [
        n for mat in mats if mat.node_tree
        for n in mat.node_tree.nodes if n.type == "TEX_IMAGE" and n.image
    ]
    print("PREPACK_IMAGES=" + repr([
        (n.image.name, n.image.has_data, bool(n.image.packed_file), n.image.filepath)
        for n in image_nodes
    ]))
    if len(mats) != 4 or len(image_nodes) < 4:
        raise RuntimeError("Original v03 Blend missing one of four material image nodes")
    bpy.ops.file.pack_all()
    if any(not n.image.packed_file for n in image_nodes):
        raise RuntimeError("Could not pack all image textures")
    bpy.ops.wm.save_as_mainfile(filepath=str(OUT_BLEND), compress=True)
    bpy.ops.object.select_all(action="DESELECT")
    rigs = [ob for ob in bpy.data.objects if ob.type == "ARMATURE"]
    meshes = [ob for ob in bpy.data.objects if ob.type == "MESH"]
    if len(rigs) != 1 or len(meshes) != 1:
        raise RuntimeError("Expected one rig and skinned mesh")
    rigs[0].select_set(True)
    meshes[0].select_set(True)
    bpy.context.view_layer.objects.active = rigs[0]
    bpy.ops.export_scene.fbx(
        filepath=str(OUT_FBX), use_selection=True,
        object_types={"ARMATURE", "MESH"},
        path_mode="COPY", embed_textures=True,
        add_leaf_bones=False, bake_anim=True,
        bake_anim_use_all_bones=True,
        bake_anim_use_nla_strips=False,
        bake_anim_use_all_actions=False,
        bake_anim_force_startend_keying=True, bake_anim_step=1.0
    )
    print("V04_PACKED_BLEND=" + str(OUT_BLEND))
    print("V04_EMBEDDED_ANIM_FBX=" + str(OUT_FBX))
    print("TEXTURE_IMAGES=" + ",".join(n.image.name for n in image_nodes))


if __name__ == "__main__":
    main()
