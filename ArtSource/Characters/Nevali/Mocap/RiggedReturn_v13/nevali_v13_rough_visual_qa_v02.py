"""Render real animated frames for visual inspection, leaving rig .blend intact."""
import bpy
import json
import math
from pathlib import Path
from mathutils import Vector

ROOT = Path(__file__).resolve().parent
SOURCE = ROOT / "Nevali_v13_MediaPipe_BlockPlay_Rough_v02.blend"
OUT = ROOT / "Nevali_v13_RoughMotion_VisualQA_v02"


def bounds_at(mesh, depsgraph):
    eob = mesh.evaluated_get(depsgraph)
    me = eob.to_mesh()
    try:
        coords = [eob.matrix_world @ v.co for v in me.vertices]
        if not all(all(math.isfinite(v[i]) for i in range(3)) for v in coords):
            return {"valid": False}
        return {
            "valid": True,
            "min": [round(min(v[i] for v in coords), 4) for i in range(3)],
            "max": [round(max(v[i] for v in coords), 4) for i in range(3)],
        }
    finally:
        eob.to_mesh_clear()


def main():
    if not SOURCE.exists():
        raise FileNotFoundError(SOURCE)
    if OUT.exists() and any(OUT.iterdir()):
        raise FileExistsError("Visual QA output directory has content; no overwrite")
    OUT.mkdir(exist_ok=True)
    bpy.ops.wm.open_mainfile(filepath=str(SOURCE))
    scene = bpy.context.scene
    meshes = [o for o in bpy.data.objects if o.type == "MESH"]
    if len(meshes) != 1:
        raise RuntimeError(f"Expected one skinned mesh, got {len(meshes)}")
    scene.render.engine = "BLENDER_EEVEE"
    scene.render.resolution_x = 640
    scene.render.resolution_y = 640
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = "PNG"
    scene.render.film_transparent = False
    if scene.world is None:
        scene.world = bpy.data.worlds.new("Nevali_VisualQA_World")
    scene.world.color = (0.20, 0.20, 0.20)

    bpy.ops.object.camera_add(location=(0, -3.5, 1.3))
    camera = bpy.context.object
    target = Vector((0, 0, 0.75))
    camera.rotation_euler = (target - camera.location).to_track_quat("-Z", "Y").to_euler()
    camera.data.type = "ORTHO"
    camera.data.ortho_scale = 2.65
    scene.camera = camera
    bpy.ops.object.light_add(type="AREA", location=(-1.6, -2.2, 3.0))
    key = bpy.context.object
    key.data.energy = 550
    key.data.shape = "DISK"
    key.data.size = 5
    bpy.ops.object.light_add(type="AREA", location=(1.8, 1.0, 2.5))
    fill = bpy.context.object
    fill.data.energy = 350
    fill.data.size = 4

    report = {"frames": {}, "note": "Rendered EEVEE previews from rough MediaPipe-driven animation; no Unity test"}
    for frame in (1, 72, 144, 240):
        scene.frame_set(frame)
        scene.render.filepath = str(OUT / f"Nevali_v13_Rough_F{frame:03d}.png")
        bbox = bounds_at(meshes[0], bpy.context.evaluated_depsgraph_get())
        report["frames"][str(frame)] = bbox
        print("POSE_BOUNDS " + str(frame) + " " + json.dumps(bbox))
        bpy.ops.render.render(write_still=True)
    (OUT / "VisualQA.json").write_text(json.dumps(report, indent=2), encoding="utf-8")
    print("VISUAL_QA_DONE=" + str(OUT))


if __name__ == "__main__":
    main()
