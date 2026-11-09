"""Lightweight CPU pose extraction for Nevali's existing Flow reference.

Uses installed Mixamo LLM Mocap MediaPipe Python + cached PoseLandmarker.
Creates an intermediate 3D world-landmark file, NOT GVHMR or SMPL-X data.
Never modifies video, Mixamo rig, earlier extraction, or Unity assets.
"""
from pathlib import Path
import json
import sys
import cv2
import numpy as np
import mediapipe as mp
from mediapipe.tasks import python as mp_python
from mediapipe.tasks.python import vision


def main():
    repo = Path.home() / "Documents/Codex/MocapLab/mixamo-llm-mocap"
    plate = repo / "plates/nevali_blockstack_flow_20261023"
    video = plate / "nevali_blockstack_source.mp4"
    model = repo / "tools/models/pose_landmarker_heavy.task"
    output = plate / "nevali_pose_world_mediapipe_v01.npz"
    report = plate / "nevali_pose_world_mediapipe_v01.json"
    if output.exists() or report.exists():
        raise FileExistsError("Never overwrite an existing v01 pose extraction")
    if not (video.is_file() and model.is_file()):
        raise FileNotFoundError("Missing installed model or staged video")
    cap = cv2.VideoCapture(str(video))
    fps = float(cap.get(cv2.CAP_PROP_FPS))
    count = int(cap.get(cv2.CAP_PROP_FRAME_COUNT))
    width = int(cap.get(cv2.CAP_PROP_FRAME_WIDTH))
    height = int(cap.get(cv2.CAP_PROP_FRAME_HEIGHT))
    if fps <= 0:
        raise ValueError("Video has no FPS")
    opts = vision.PoseLandmarkerOptions(
        base_options=mp_python.BaseOptions(model_asset_path=str(model)),
        running_mode=vision.RunningMode.VIDEO,
        num_poses=1,
        min_pose_detection_confidence=0.5,
        min_pose_presence_confidence=0.5,
        min_tracking_confidence=0.5,
    )
    world = []
    image = []
    with vision.PoseLandmarker.create_from_options(opts) as detector:
        frame_idx = 0
        while True:
            ok, bgr = cap.read()
            if not ok:
                break
            rgb = cv2.cvtColor(bgr, cv2.COLOR_BGR2RGB)
            mp_img = mp.Image(image_format=mp.ImageFormat.SRGB, data=rgb)
            result = detector.detect_for_video(mp_img, int(round(frame_idx * 1000 / fps)))
            w = np.full((33, 3), np.nan, dtype=np.float32)
            p = np.zeros((33, 4), dtype=np.float32)
            if result.pose_world_landmarks:
                for j, l in enumerate(result.pose_world_landmarks[0]):
                    w[j] = [l.x, l.y, l.z]
            if result.pose_landmarks:
                for j, l in enumerate(result.pose_landmarks[0]):
                    p[j] = [l.x * width, l.y * height, l.visibility or 0, l.presence or 0]
            world.append(w)
            image.append(p)
            frame_idx += 1
    cap.release()
    if len(world) != count:
        raise RuntimeError(f"Not all frames processed: {len(world)} != {count}")
    world = np.stack(world)
    image = np.stack(image)
    valid = np.isfinite(world).all(axis=(1, 2))
    essential = {name: int(i) for name, i in {
        "nose": 0, "left_shoulder": 11, "right_shoulder": 12,
        "left_elbow": 13, "right_elbow": 14, "left_wrist": 15, "right_wrist": 16,
        "left_hip": 23, "right_hip": 24, "left_knee": 25, "right_knee": 26,
        "left_ankle": 27, "right_ankle": 28
    }.items()}
    report_data = {
        "source": str(video),
        "method": "mediapipe_pose_world_landmarks_CPU",
        "is_gvhmr_smplx": False,
        "is_final_retarget": False,
        "fps": fps,
        "frames": len(world),
        "valid_world_frames": int(valid.sum()),
        "image_size": [width, height],
        "per_joint": {
            name: {
                "mean_visibility": round(float(image[:, idx, 2].mean()), 4),
                "image_motion_px": np.ptp(image[:, idx, :2], axis=0).round(2).tolist(),
                "world_motion_m": np.ptp(world[valid, idx, :3], axis=0).round(4).tolist()
            } for name, idx in essential.items()
        },
        "caveat": "Cross-legged legs/feet are occluded, world landmarks are an estimate; no reliable finger contact",
    }
    np.savez_compressed(output, world=world, image=image, fps=np.float32(fps))
    report.write_text(json.dumps(report_data, indent=2), encoding="utf-8")
    print("MEDIAPIPE_WORLD_OUTPUT=" + str(output))
    print("MEDIAPIPE_WORLD_REPORT=" + str(report))
    print("WORLD_VALID_FRAMES=" + str(int(valid.sum())) + "/" + str(len(world)))


if __name__ == "__main__":
    main()
