"""Inspect actual animated right/left hand and hip in the separate v04 rough cut."""
from pathlib import Path
import json
import bpy

ROOT = Path(__file__).resolve().parent
bpy.ops.wm.open_mainfile(filepath=str(ROOT / "Nevali_v13_MediaPipe_BlockPlay_Rough_v04.blend"))
arm = next(ob for ob in bpy.data.objects if ob.type == "ARMATURE")
pb = arm.pose.bones
result = {}
for f in [1, 24, 48, 72, 96, 120, 144, 168, 192, 216, 240]:
    bpy.context.scene.frame_set(f)
    row = {}
    for bone_name in ["Hips", "RightHand", "RightHandMiddle1", "LeftHand", "LeftHandMiddle1", "Head"]:
        bone = pb.get("mixamorig:" + bone_name)
        if bone:
            p = arm.matrix_world @ bone.head
            row[bone_name] = [round(x, 3) for x in p]
            if bone_name == "RightHand":
                row["RightHand_tail"] = [round(x, 3) for x in (arm.matrix_world @ bone.tail)]
    result[f] = row
print("NEVALI_HAND_PROBE=" + json.dumps(result))
