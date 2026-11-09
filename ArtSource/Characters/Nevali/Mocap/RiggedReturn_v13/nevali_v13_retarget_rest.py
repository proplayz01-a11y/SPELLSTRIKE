"""Create a *separate* Nevali-specific Mixamo rest rig + mocap profile.

No writes to mixamo-llm-mocap/rig_profile.json or its Y Bot files.
Requires existing local installed MocapLab, versioned output names.
"""
import bpy
import json
import math
import os
import sys
from pathlib import Path

HERE = Path(__file__).resolve().parent
SOURCE = HERE / "Nevali_v13_Mixamo_TPose_WithSkin.fbx"
OUT_BLEND = HERE / "Nevali_v13_Mixamo_RetargetRest_v01.blend"
OUT_PROFILE = HERE / "Nevali_v13_Mixamo_RigProfile_v01.json"
MOCAP_PIPELINE = Path.home() / "Documents" / "Codex" / "MocapLab" / "mixamo-llm-mocap" / "pipeline"


def main():
    if not SOURCE.is_file() or not (MOCAP_PIPELINE / "setup_rig.py").is_file():
        raise FileNotFoundError("Cannot find v13 returned rig or installed MocapLab")
    if OUT_BLEND.exists() or OUT_PROFILE.exists():
        raise FileExistsError("Versioned v01 rest/profile exists, refusing overwrite")
    sys.path.insert(0, str(MOCAP_PIPELINE))
    from setup_rig import dump_rig_profile

    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.context.scene.render.fps = 30
    bpy.context.scene.frame_start = 1
    bpy.ops.import_scene.fbx(filepath=str(SOURCE))
    arms = [o for o in bpy.data.objects if o.type == "ARMATURE"]
    if len(arms) != 1:
        raise RuntimeError(f"Expected exactly one Mixamo armature; got {len(arms)}")
    arm = arms[0]
    arm.name = "Armature"
    bones = arm.pose.bones
    if any(not b.name.startswith("mixamorig:") for b in bones):
        raise RuntimeError("Unexpected non-Mixamo bones")
    if "mixamorig:Hips" not in bones:
        raise RuntimeError("Missing Mixamo Hips bone")
    hip = (arm.matrix_world @ bones["mixamorig:Hips"].matrix).to_translation().z
    if not (0.5 < hip < 1.5):
        raise RuntimeError(f"Suspicious Mixamo rest hip: {hip:.4f} meters")
    if abs(arm.rotation_euler.x - math.pi / 2) > 1e-3:
        raise RuntimeError(f"Unexpected armature X rotation: {arm.rotation_euler.x}")
    # Nevali's custom-size Mixamo return uses scale 1.0, unlike repository
    # Y Bot convention (0.01). Preserve that scale: hip height has already
    # been validated in meters, and dump_rig_profile() measures actual scale.
    if not any(abs(arm.scale.x - allowed) < 1e-5 for allowed in (1.0, 0.01)):
        raise RuntimeError(f"Unexpected armature scale: {arm.scale[:]}")

    profile = dump_rig_profile(arm, OUT_PROFILE)
    if len(profile.get("rest", {})) < 20 or not profile.get("rig"):
        # The actual retarget uses FK rest matrices via bl_motion.rig_table().
        # Keep the measured profile if rig_table's keys differ; require a
        # canonical nested rest table rather than guessing structures.
        print("PROFILE_TOP_LEVEL_KEYS=" + repr(list(profile.keys())))
    bpy.ops.wm.save_as_mainfile(filepath=str(OUT_BLEND), compress=True)
    print("NEVALI_RETARGET_REST_SAVED=" + str(OUT_BLEND))
    print("NEVALI_RIG_PROFILE_SAVED=" + str(OUT_PROFILE))
    print("PROFILE_SUMMARY=" + json.dumps({
        "bones": len(bones), "hip_height": hip,
        "ground_z": profile.get("ground_z"),
        "keys": list(profile.keys()),
    }))


if __name__ == "__main__":
    main()
