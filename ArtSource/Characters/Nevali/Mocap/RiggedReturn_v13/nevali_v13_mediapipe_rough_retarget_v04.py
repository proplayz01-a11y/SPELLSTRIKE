"""Versioned 8-second MediaPipe -> Nevali Mixamo FK rough cut v03.

This is a *low-cost defense blocking alternative*, NOT SMPL-X/GVHMR
retargeting. Right-arm movement comes from the actual Google Flow video;
seated legs are approximate manual animation because feet are occluded.
Keep all 192 source frames, resampled to 240 frames at 30 FPS. Never
overwrite the approved v13 Blender/GLB/FBX or the Mixamo T-pose return.
"""
from pathlib import Path
import json
import math
import bpy
import numpy as np
from mathutils import Vector

HERE = Path(__file__).resolve().parent
FBX = HERE / "Nevali_v13_Mixamo_TPose_WithSkin.fbx"
PLATE = Path.home() / "Documents/Codex/MocapLab/mixamo-llm-mocap/plates/nevali_blockstack_flow_20261023"
LANDMARKS = PLATE / "nevali_pose_world_mediapipe_v01.npz"
OUT_BLEND = HERE / "Nevali_v13_MediaPipe_BlockPlay_Rough_v03.blend"
OUT_FBX = HERE / "Nevali_v13_MediaPipe_BlockPlay_Rough_v03.fbx"
OUT_QA = HERE / "Nevali_v13_MediaPipe_BlockPlay_Rough_v03.json"


def target_world(vec):
    # MediaPipe camera facing actor: X screen-right; Y image-down, Z away.
    # Imported Nevali faces -Y in Blender. Ground/up becomes +Z.
    return Vector((float(vec[0]), float(vec[2]), -float(vec[1])))


def set_aim(rig, pb, direction, strength=1.0):
    """Solve a rotation in bone-local space after parent matrices updated."""
    if direction.length < 1e-6:
        return
    want_world = direction.normalized()
    arm_inv = rig.matrix_world.to_3x3().inverted()
    want = (arm_inv @ want_world).normalized()
    rest_q = pb.bone.matrix_local.to_quaternion()
    rest_dir = rest_q @ Vector((0, 1, 0))
    full_q = rest_dir.rotation_difference(want)
    if strength != 1.0:
        full_q = full_q.slerp(type(full_q)(), 1.0-strength)
    desired = full_q @ rest_q
    if pb.parent:
        parent_now = pb.parent.matrix.to_quaternion()
        parent_rest = pb.parent.bone.matrix_local.to_quaternion()
        inherited = parent_now @ parent_rest.inverted() @ rest_q
    else:
        inherited = rest_q
    local = inherited.inverted() @ desired
    local.normalize()
    pb.rotation_quaternion = local
    bpy.context.view_layer.update()


def pose_joints(world):
    midpoint = lambda a, b: 0.5 * (world[a] + world[b])
    return {
        "hips": midpoint(23, 24),
        "shoulders": midpoint(11, 12),
        "nose": world[0],
        "l_sh": world[11],
        "l_el": world[13],
        "l_wr": world[15],
        "r_sh": world[12],
        "r_el": world[14],
        "r_wr": world[16],
    }


def pose_rough_frame(arm, joints):
    bones = arm.pose.bones
    # Retain Mixamo hips as root and let the body lower toward the floor.
    # The leg shapes are artist-authored because source toes were invisible.
    aims = [
        ("Spine", "hips", "shoulders", 0.65),
        ("LeftUpLeg", None, None, 1.0),
        ("RightUpLeg", None, None, 1.0),
        ("LeftLeg", None, None, 1.0),
        ("RightLeg", None, None, 1.0),
        ("LeftArm", "l_sh", "l_el", 1.0),
        ("LeftForeArm", "l_el", "l_wr", 1.0),
        ("RightArm", "r_sh", "r_el", 1.0),
        ("RightForeArm", "r_el", "r_wr", 1.0),
        ("Head", "shoulders", "nose", 0.28),
    ]
    artificial = {
        # Keep knees relaxed under the hem rather than forcing both thighs
        # fully horizontal: v01 spread the skin-weighted dress excessively.
        "LeftUpLeg": Vector((0.22, -0.45, -0.35)),
        "RightUpLeg": Vector((-0.22, -0.45, -0.35)),
        # V02's downward shins pushed boots ~0.25m beneath the floor.
        "LeftLeg": Vector((-0.36, 0.10, -0.15)),
        "RightLeg": Vector((0.36, 0.10, -0.15)),
    }
    for name, a, b, strength in aims:
        pb = bones.get("mixamorig:" + name)
        if not pb:
            raise RuntimeError("Missing expected Mixamo bone " + name)
        direction = artificial[name] if name in artificial else target_world(joints[b]-joints[a])
        set_aim(arm, pb, direction, strength=strength)


def main():
    required = (FBX, LANDMARKS)
    if not all(x.exists() for x in required):
        raise FileNotFoundError("Missing rigged FBX or verified MediaPipe world clip")
    if any(x.exists() for x in (OUT_BLEND, OUT_FBX, OUT_QA)):
        raise FileExistsError("Rough v01 output exists; refusing overwrite")
    source = np.load(LANDMARKS)
    original = source["world"]
    confidence = source["image"]
    src_fps = float(source["fps"])
    if original.shape != (192, 33, 3) or src_fps != 24:
        raise RuntimeError(f"Source differs from verified 8s, 24fps plate: {original.shape} {src_fps}")
    if not np.isfinite(original).all():
        raise RuntimeError("Invalid 3D source frames")
    if min(float(confidence[:, i, 2].mean()) for i in (11, 12, 13, 14, 15, 16)) < 0.65:
        raise RuntimeError("Upper body is not sufficiently visible")

    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=str(FBX), use_anim=False)
    arms = [o for o in bpy.data.objects if o.type == "ARMATURE"]
    meshes = [o for o in bpy.data.objects if o.type == "MESH"]
    if len(arms) != 1 or len(meshes) != 1:
        raise RuntimeError("Expected one Mixamo skinned mesh and armature")
    arm = arms[0]
    arm.name = "Nevali_v13_MixamoArmature"
    bpy.context.view_layer.objects.active = arm
    for ob in bpy.context.selected_objects:
        ob.select_set(False)
    arm.select_set(True)
    for pb in arm.pose.bones:
        pb.rotation_mode = "QUATERNION"
        pb.rotation_quaternion.identity()
    scene = bpy.context.scene
    scene.render.fps = 30
    scene.frame_start = 1
    scene.frame_end = 240
    # Static seated root: the source girl is cross-legged on the ground.
    # Folded-knee sitting: pelvis near 0.55 m with feet close to floor.
    arm.location.z -= 0.42

    # Smooth just 3 source frames, zero-padded by nearest frame.
    padded = np.pad(original, ((1, 1), (0, 0), (0, 0)), mode="edge")
    filtered = (padded[:-2] + 2.0*padded[1:-1] + padded[2:]) / 4.0
    frame_qa = {}
    controls = ("Spine", "LeftUpLeg", "RightUpLeg", "LeftLeg", "RightLeg",
                "LeftArm", "LeftForeArm", "RightArm", "RightForeArm", "Head")
    for frame in range(1, 241):
        # Original frame indices 0..191 occupy t=0..7.958s.
        # 240 target keys occupy t=0..7.967s. No clip trimming.
        p = (frame - 1) * src_fps / 30.0
        a = min(191, int(math.floor(p)))
        b = min(191, a + 1)
        fraction = p - math.floor(p)
        lm = filtered[a] * (1.0-fraction) + filtered[b] * fraction
        scene.frame_set(frame)
        pose_rough_frame(arm, pose_joints(lm))
        arm.keyframe_insert(data_path="location", frame=frame, group="SeatedRoot")
        for name in controls:
            pb = arm.pose.bones["mixamorig:" + name]
            pb.keyframe_insert(data_path="rotation_quaternion", frame=frame, group=pb.name)
        if frame in (1, 60, 120, 180, 240):
            wrist = target_world(lm[16] - lm[12])
            frame_qa[str(frame)] = {
                "right_wrist_from_shoulder_m": list(wrist),
                "nose_image_x_px": float(confidence[min(191, a), 0, 0]),
            }
    scene.frame_set(1)
    scene["nevali_motion_qa"] = "UNPOLISHED_MEDIAPIPE_WORLD_ROUGH_V03"
    scene["source_clip"] = str(LANDMARKS)
    scene["source_original_frames"] = 192
    scene["quality_note"] = "Foot and leg motion is manually blocked; correct seated folds/dress, right-hand prop contact in Blender"
    bpy.ops.wm.save_as_mainfile(filepath=str(OUT_BLEND), compress=True)

    bpy.ops.object.select_all(action="DESELECT")
    arm.select_set(True)
    for mesh in meshes:
        mesh.select_set(True)
    bpy.context.view_layer.objects.active = arm
    bpy.ops.export_scene.fbx(
        filepath=str(OUT_FBX), use_selection=True, object_types={"ARMATURE", "MESH"},
        add_leaf_bones=False, bake_anim=True, bake_anim_use_all_bones=True,
        bake_anim_use_nla_strips=False, bake_anim_use_all_actions=False,
        bake_anim_force_startend_keying=True, bake_anim_step=1.0)
    report = {
        "status": "ROUGH_V03_FOOT_FLOOR_CORRECTION_NEEDS_VISUAL_QA",
        "is_gvhmr_smplx": False,
        "source": str(LANDMARKS),
        "rig_source": str(FBX),
        "original_video_frames": 192, "original_video_fps": 24,
        "animation_frames": 240, "target_fps": 30,
        "duration_seconds": 8.0,
        "armature_bones": len(arm.pose.bones),
        "mesh_vertices": sum(len(o.data.vertices) for o in meshes),
        "output_blend": str(OUT_BLEND), "output_fbx": str(OUT_FBX),
        "right_arm_source_visibility": round(float(confidence[:, 16, 2].mean()), 3),
        "foot_visibility": round(float(confidence[:, [27,28,29,30,31,32], 2].mean()), 3),
        "qa_samples": frame_qa,
        "limitations": [
            "MediaPipe world landmarks are estimated from partially occluded seated footage",
            "Hand/finger grip and wooden-block contact not yet animated",
            "Seated skirt, shoe position, root height and knee folding require visual correction",
            "Does not constitute validated SMPL-X/GVHMR motion extraction",
            "Animation export and real Unity playback still need independent verification",
        ]
    }
    OUT_QA.write_text(json.dumps(report, indent=2), encoding="utf-8")
    print("NEVALI_ROUGH_RETARGET_BLEND=" + str(OUT_BLEND))
    print("NEVALI_ROUGH_RETARGET_FBX=" + str(OUT_FBX))
    print("NEVALI_ROUGH_RETARGET_QA=" + str(OUT_QA))
    print("COMPLETE_DURATION=240 frames @30 fps, source 192 @24")


if __name__ == "__main__":
    main()
