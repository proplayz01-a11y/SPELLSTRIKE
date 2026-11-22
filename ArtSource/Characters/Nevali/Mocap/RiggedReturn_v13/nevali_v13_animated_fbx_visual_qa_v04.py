"""Real rendered-frame QA of independently reimported animated v04 FBX."""
import bpy
import json
from pathlib import Path
from mathutils import Vector

ROOT = Path(__file__).resolve().parent
SOURCE = ROOT / "Nevali_v13_MediaPipe_BlockPlay_Rough_v04.fbx"
OUT = ROOT / "Nevali_v13_AnimatedFBX_VisualQA_v04"
REPORT = ROOT / "Nevali_v13_AnimatedFBX_VisualQA_v04.json"
if REPORT.exists() or (OUT.exists() and any(OUT.iterdir())):
    raise FileExistsError("Refusing to overwrite existing v04 visual QA")
OUT.mkdir(exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=str(SOURCE), use_anim=True)
armatures = [o for o in bpy.data.objects if o.type == "ARMATURE"]
meshes = [o for o in bpy.data.objects if o.type == "MESH"]
if len(armatures) != 1 or len(meshes) != 1:
    raise RuntimeError("Expected one animated Mixamo armature and mesh")
scene = bpy.context.scene
scene.render.engine = "BLENDER_EEVEE"
scene.render.resolution_x = 640
scene.render.resolution_y = 640
scene.render.resolution_percentage = 100
scene.render.image_settings.file_format = "PNG"
scene.render.film_transparent = False
if scene.world is None:
    scene.world = bpy.data.worlds.new("FBX_Visual_QA_World")
scene.world.color = (0.2, 0.2, 0.2)
bpy.ops.object.camera_add(location=(0, -3.5, 1.3))
cam = bpy.context.object
cam.rotation_euler = (Vector((0, 0, 0.75)) - cam.location).to_track_quat("-Z", "Y").to_euler()
cam.data.type = "ORTHO"
cam.data.ortho_scale = 2.65
scene.camera = cam
bpy.ops.object.light_add(type="AREA", location=(-1.6, -2.2, 3.0))
bpy.context.object.data.energy = 550
bpy.context.object.data.size = 5
bpy.ops.object.light_add(type="AREA", location=(1.8, 1.0, 2.5))
bpy.context.object.data.energy = 350
bpy.context.object.data.size = 4
qa = {"source": str(SOURCE), "frame_snapshots": {}, "action": None}
arm = armatures[0]
if arm.animation_data and arm.animation_data.action:
    qa["action"] = {"name": arm.animation_data.action.name,
                    "range": list(arm.animation_data.action.frame_range)}
for f in (2, 97, 145, 241):
    scene.frame_set(f)
    scene.render.filepath = str(OUT / f"FBX_Animated_F{f:03d}.png")
    bpy.ops.render.render(write_still=True)
    qa["frame_snapshots"][f] = scene.render.filepath
REPORT.write_text(json.dumps(qa, indent=2), encoding="utf-8")
print("NEVALI_V04_ANIMATED_FBX_VISUAL_QA=" + str(REPORT))
