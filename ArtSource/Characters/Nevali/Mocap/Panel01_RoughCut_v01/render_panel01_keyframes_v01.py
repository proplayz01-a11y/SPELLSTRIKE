"""Render four proof-of-frame PNGs from the isolated Family Home rough cut."""
import bpy
import json
from pathlib import Path

HERE = Path(__file__).resolve().parent
SOURCE = HERE / "SPELLSTRIKE_P01_FamilyHome_NevaliV13_RoughCut_v01.blend"
OUT = HERE / "FrameQA_v01"
REPORT = HERE / "P01_FrameQA_v01.json"
if REPORT.exists() or (OUT.exists() and any(OUT.iterdir())):
    raise FileExistsError("QA render already exists")
OUT.mkdir(exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=str(SOURCE))
scene = bpy.context.scene
scene.render.engine = "BLENDER_EEVEE"
scene.render.resolution_x = 960
scene.render.resolution_y = 540
scene.render.resolution_percentage = 100
scene.render.image_settings.file_format = "PNG"
scene.render.film_transparent = False
if not scene.camera:
    raise RuntimeError("No camera")
body = bpy.data.objects.get("P01_NEVALI_V13_SKIN")
rig = bpy.data.objects.get("P01_NEVALI_V13_VIDEO_MOTION_ROUGH")
van = bpy.data.objects.get("P01_YOUNG_VAN_TEMPORARY_STANDIN")
moving = bpy.data.objects.get("P01_TOY_BLOCK_HAND_BLOCKED")
if not all((body, rig, van, moving)):
    raise RuntimeError("P01 actor, skin, or toy not present")
report = {"scene": scene.name, "frames": {}, "camera": scene.camera.name}
for frame in (1, 96, 144, 240):
    scene.frame_set(frame)
    toyloc = [round(v, 4) for v in moving.location]
    scene.render.filepath = str(OUT / f"Home_NevaliV13_F{frame:03d}.png")
    bpy.ops.render.render(write_still=True)
    report["frames"][frame] = {"toy_location": toyloc,
                                "render": scene.render.filepath}
REPORT.write_text(json.dumps(report, indent=2), encoding="utf-8")
print("P01_FRAME_QA=" + str(REPORT))
