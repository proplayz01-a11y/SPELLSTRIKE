"""Independent read-only reimport of the exported v03 animated FBX."""
import bpy
import json
import math
from pathlib import Path

ROOT = Path(__file__).resolve().parent
SOURCE = ROOT / "Nevali_v13_MediaPipe_BlockPlay_Rough_v03.fbx"
REPORT = ROOT / "Nevali_v13_RoughMotion_FBXRoundtripQA_v03.json"


def main():
    if REPORT.exists():
        raise FileExistsError("Refusing to overwrite existing roundtrip report")
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=str(SOURCE), use_anim=True)
    arms = [o for o in bpy.data.objects if o.type == "ARMATURE"]
    meshes = [o for o in bpy.data.objects if o.type == "MESH"]
    if len(arms) != 1 or len(meshes) != 1:
        raise RuntimeError(f"Unexpected armature/mesh count: {len(arms)}, {len(meshes)}")
    arm = arms[0]
    actions = []
    for action in bpy.data.actions:
        actions.append({
            "name": action.name, "frame_range": list(action.frame_range),
            "slots": len(action.slots) if hasattr(action, "slots") else None,
        })
    materials = []
    for mat in bpy.data.materials:
        ims = []
        if mat.use_nodes and mat.node_tree:
            for n in mat.node_tree.nodes:
                if n.type == "TEX_IMAGE" and n.image:
                    ims.append({"name": n.image.name, "has_data": bool(n.image.has_data),
                                "packed": bool(n.image.packed_file)})
        materials.append({"name": mat.name, "images": ims})
    weighted = sum(bool(v.groups) for v in meshes[0].data.vertices)
    result = {
        "source_fbx": str(SOURCE),
        "source_size": SOURCE.stat().st_size,
        "bones": len(arm.pose.bones),
        "vertices": len(meshes[0].data.vertices),
        "weighted_vertices": weighted,
        "triangles": sum(len(p.vertices)-2 for p in meshes[0].data.polygons),
        "armature_action_name": arm.animation_data.action.name if arm.animation_data and arm.animation_data.action else None,
        "actions": actions,
        "material_count": len(bpy.data.materials),
        "materials": materials,
        "expected": "240 frames at 30 fps from 192 source frames at 24 fps",
        "note": "Import/animation structure only; neither motion fidelity nor Unity Humanoid validated",
    }
    result["passed_structural"] = (
        result["bones"] == 57
        and result["weighted_vertices"] == result["vertices"] == 74083
        and result["triangles"] == 118962
        and bool(result["actions"])
        and any(a["frame_range"][1] - a["frame_range"][0] >= 238 for a in actions)
        and all(any(x["has_data"] for x in m["images"]) for m in materials)
    )
    REPORT.write_text(json.dumps(result, indent=2), encoding="utf-8")
    print("FBX_ROUNDTRIP_V03_PASS=" + str(result["passed_structural"]))
    print("FBX_ANIM_ACTIONS=" + json.dumps(actions))
    print("FBX_QA_REPORT=" + str(REPORT))
    if not result["passed_structural"]:
        raise RuntimeError("FBX structural/animation roundtrip QA failed")


if __name__ == "__main__":
    main()
