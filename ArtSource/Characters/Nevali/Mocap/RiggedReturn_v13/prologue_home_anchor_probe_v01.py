"""Read-only P01 Blender environment anchor/camera inspection."""
import bpy
import json
from pathlib import Path

root = Path(__file__).resolve().parents[5]
home = root / "ArtSource/LibraryProps/DefenseSets/Prologue_Home_01_Peaceful_v01.blend"
print("PROJECT_ROOT", str(root), "HOME_EXISTS", home.exists())
bpy.ops.wm.open_mainfile(filepath=str(home))
items = [
    {
        "name": obj.name, "type": obj.type, "location": [round(v, 3) for v in obj.location],
        "dimensions": [round(v, 3) for v in obj.dimensions],
        "rotation": [round(v, 3) for v in obj.rotation_euler],
    }
    for obj in bpy.data.objects
    if obj.type in {"CAMERA", "EMPTY"} or
    any(x in obj.name.lower() for x in ("toy", "block", "floor", "mat", "rug", "van", "nevali", "door"))
]
print("HOME_ANCHOR_PROBE=" + json.dumps(items))
