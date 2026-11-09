import bpy
from mathutils import Vector
from pathlib import Path

p=Path(__file__).resolve().parent/"StudyChandelier_BrassRing_v01.blend"
bpy.ops.wm.open_mainfile(filepath=str(p))
for ob in bpy.data.objects:
    if ob.name.startswith("SPOKE") or ob.name.startswith("RING"):
        if ob.type=="MESH":
            pts=[ob.matrix_world @ v.co for v in ob.data.vertices]
            maxrad=max((pt.x*pt.x+pt.y*pt.y)**.5 for pt in pts)
            print("GEOM",ob.name,"location",tuple(round(v,3) for v in ob.location),
                  "dim",tuple(round(v,3) for v in ob.dimensions),
                  "max_world_radius",round(maxrad,3))
