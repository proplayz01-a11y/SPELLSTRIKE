"""Isolated P01 camera + Young Van foreground composition correction."""
from pathlib import Path
import bpy
import json

HERE = Path(__file__).resolve().parent
SRC = HERE / "Panel01_RoughCut_v01" / "SPELLSTRIKE_P01_FamilyHome_NevaliV13_RoughCut_v01.blend"
OUT = HERE / "Panel01_RoughCut_v02"
DST = OUT / "SPELLSTRIKE_P01_FamilyHome_NevaliV13_RoughCut_v02.blend"
REPORT = OUT / "P01_FramingReport_v02.json"
if OUT.exists() and any(OUT.iterdir()):
    raise FileExistsError("v02 Panel01 already exists; refusing overwrite")
OUT.mkdir(exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=str(SRC))
scn = bpy.context.scene
camera = scn.camera
van = bpy.data.objects.get("P01_YOUNG_VAN_TEMPORARY_STANDIN")
nevali = bpy.data.objects.get("P01_NEVALI_V13_VIDEO_MOTION_ROUGH")
if not all((camera, van, nevali)):
    raise RuntimeError("Missing camera or actor")
van.location = (-1.80, -3.1, 0.0)
van.rotation_euler.z = 0.62
camera.data.lens = 32.0
scn.view_settings.view_transform = "AgX"
scn.view_settings.exposure = -0.50
scn.render.fps = 30
scn.frame_start, scn.frame_end = 1, 240
scn.frame_set(1)
scn["roughcut_notice"] = "V02: Young Van temp model visible foreground; Nevali v13 rough block motion; not Unity Timeline"
bpy.ops.wm.save_as_mainfile(filepath=str(DST), compress=True)
REPORT.write_text(json.dumps({
    "input": str(SRC), "output": str(DST),
    "van_foreground_root": list(van.location),
    "van_facing_z_rad": van.rotation_euler.z,
    "camera": camera.name, "lens_mm": camera.data.lens,
    "exposure": scn.view_settings.exposure,
    "frames": [1, 240], "fps": 30,
    "limitations": ["Temporary Young Van model", "Toy contact manually blocked", "Unity not tested"]
}, indent=2), encoding="utf-8")
print("P01_V02_SCENE_SAVED=" + str(DST))
