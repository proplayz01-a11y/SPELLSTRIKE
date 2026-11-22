"""Independent import/deformation QA for the Mixamo-skinned Nevali v13.

Run with Blender 5.0 --background --factory-startup --python this_script.py.
Never edits the approved v13 source or returned FBX. Outputs a new .blend
and JSON in the same dedicated RiggedReturn_v13 folder.
"""
import bpy
import json
import math
import os
import traceback
from mathutils import Vector

ROOT = os.path.dirname(os.path.abspath(__file__))
SOURCE = os.path.join(ROOT, "Nevali_v13_Mixamo_TPose_WithSkin.fbx")
OUTPUT_BLEND = os.path.join(ROOT, "Nevali_v13_RigDeformation_QA_v01.blend")
OUTPUT_JSON = os.path.join(ROOT, "Nevali_v13_RigDeformation_QA_v01.json")


def evaluated_bounds(meshes):
    depsgraph = bpy.context.evaluated_depsgraph_get()
    bounds = []
    nonfinite = 0
    for ob in meshes:
        ob_eval = ob.evaluated_get(depsgraph)
        eval_mesh = ob_eval.to_mesh()
        matrix = ob_eval.matrix_world
        try:
            for v in eval_mesh.vertices:
                pos = matrix @ v.co
                if not all(math.isfinite(x) for x in pos):
                    nonfinite += 1
                else:
                    bounds.append(pos[:])
        finally:
            ob_eval.to_mesh_clear()
    if not bounds:
        return {"valid": False, "nonfinite": nonfinite}
    return {
        "valid": nonfinite == 0,
        "nonfinite": nonfinite,
        "min": [min(v[i] for v in bounds) for i in range(3)],
        "max": [max(v[i] for v in bounds) for i in range(3)],
    }


def main():
    if not os.path.isfile(SOURCE):
        raise FileNotFoundError(SOURCE)
    if os.path.exists(OUTPUT_BLEND) or os.path.exists(OUTPUT_JSON):
        raise FileExistsError("Refusing to overwrite existing v01 QA artifacts")
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=SOURCE, use_anim=True)
    meshes = [o for o in bpy.data.objects if o.type == "MESH"]
    armatures = [o for o in bpy.data.objects if o.type == "ARMATURE"]
    if len(armatures) != 1:
        raise RuntimeError(f"Expected one armature, found {len(armatures)}")
    rig = armatures[0]
    bpy.context.view_layer.objects.active = rig
    rig.select_set(True)
    pose_bones = list(rig.pose.bones)
    bone_names = [b.name for b in pose_bones]
    vertex_groups = {}
    weighted_total = 0
    for obj in meshes:
        num_weighted = sum(bool(v.groups) for v in obj.data.vertices)
        weighted_total += num_weighted
        vertex_groups[obj.name] = {
            "vertices": len(obj.data.vertices),
            "weighted_vertices": num_weighted,
            "group_count": len(obj.vertex_groups),
            "armature_modifier_count": sum(mod.type == "ARMATURE" for mod in obj.modifiers),
            "armature_parent": obj.parent.name if obj.parent else None,
            "materials": [slot.material.name if slot.material else None for slot in obj.material_slots],
        }
    mats = []
    for mat in bpy.data.materials:
        images = []
        if mat.use_nodes and mat.node_tree:
            images = [
                {"name": n.image.name, "filepath": n.image.filepath, "size": list(n.image.size),
                 "packed": bool(n.image.packed_file), "has_data": bool(n.image.has_data)}
                for n in mat.node_tree.nodes
                if n.type == "TEX_IMAGE" and n.image
            ]
        mats.append({"material": mat.name, "images": images})

    bpy.context.scene.frame_start = 1
    bpy.context.scene.frame_end = 60
    bpy.context.scene.render.fps = 30
    base = evaluated_bounds(meshes)
    animation_tests = {}
    test_specs = [
        ("LeftArm", (0.4, 0, 0)),
        ("RightForeArm", (0, 0, 0.65)),
        ("LeftUpLeg", (0.55, 0, 0)),
        ("RightLeg", (-0.6, 0, 0)),
        ("Head", (0, 0.4, 0)),
    ]
    frames = [10, 20, 30, 40, 50]
    for pb in pose_bones:
        pb.rotation_mode = "XYZ"
        pb.rotation_euler = (0, 0, 0)
        pb.keyframe_insert(data_path="rotation_euler", frame=1, group=pb.name)
    for frame, (suffix, rot) in zip(frames, test_specs):
        bpy.context.scene.frame_set(frame)
        candidates = [b for b in pose_bones if b.name.rsplit(":", 1)[-1].endswith(suffix)]
        if len(candidates) != 1:
            animation_tests[suffix] = {"result": "MISSING_OR_AMBIGUOUS_BONE", "matches": [b.name for b in candidates]}
            continue
        pb = candidates[0]
        pb.rotation_euler = rot
        bpy.context.view_layer.update()
        posed = evaluated_bounds(meshes)
        animation_tests[suffix] = {
            "result": "FINITE" if posed["valid"] else "NONFINITE",
            "bone": pb.name,
            "rotation_rad": list(rot),
            "bounds": posed,
        }
        pb.keyframe_insert(data_path="rotation_euler", frame=frame, group=pb.name)
        pb.rotation_euler = (0, 0, 0)
        pb.keyframe_insert(data_path="rotation_euler", frame=min(frame + 4, 59), group=pb.name)
        bpy.context.view_layer.update()
    bpy.context.scene.frame_set(60)
    for pb in pose_bones:
        pb.rotation_euler = (0, 0, 0)
        pb.keyframe_insert(data_path="rotation_euler", frame=60, group=pb.name)
    bpy.context.scene.frame_set(1)
    result = {
        "source_fbx": SOURCE,
        "source_bytes": os.path.getsize(SOURCE),
        "armature_count": len(armatures),
        "armature_name": rig.name,
        "bone_count": len(pose_bones),
        "bone_names": bone_names,
        "mesh_count": len(meshes),
        "mesh_details": vertex_groups,
        "weighted_vertices": weighted_total,
        "triangle_count": sum(sum(len(poly.vertices)-2 for poly in obj.data.polygons) for obj in meshes),
        "material_count": len(bpy.data.materials),
        "materials": mats,
        "base_bounds": base,
        "pose_tests": animation_tests,
        "test_notice": "Synthetic one-joint stress tests, NOT Google Flow retargeting or animation quality acceptance",
        "animation_range": [1, 60],
    }
    bpy.ops.wm.save_as_mainfile(filepath=OUTPUT_BLEND, compress=True)
    with open(OUTPUT_JSON, "w", encoding="utf-8") as f:
        json.dump(result, f, indent=2)
    print("NEVALI_V13_RIG_QA_SAVED=" + OUTPUT_JSON)
    print("ARMATURE_BONES=" + str(len(pose_bones)) + " MESHES=" + str(len(meshes)) +
          " WEIGHTED_VERTICES=" + str(weighted_total))
    print("POSE_TESTS=" + json.dumps({k: v["result"] for k, v in animation_tests.items()}))


if __name__ == "__main__":
    try:
        main()
    except Exception:
        traceback.print_exc()
        raise
