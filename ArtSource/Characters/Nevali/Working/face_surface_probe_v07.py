import bpy
from mathutils import Vector
from pathlib import Path
root=Path.cwd()
bpy.ops.wm.open_mainfile(filepath=str(root/"ArtSource/Characters/Nevali/Working/Nevali_FaceHair_Repair_v06.blend"))
body=next(o for o in bpy.data.objects if o.name.startswith("NEVALI__RODIN_ORIGINAL_BODY"))
print("FACE_SURFACE_PROBE")
for z in [1.72,1.695,1.67,1.64,1.615,1.59,1.56,1.53,1.50,1.47]:
    vals=[]
    for x in [-.17,-.13,-.09,-.05,0,.05,.09,.13,.17]:
        hit,p,n,fi=body.ray_cast(Vector((x,-.75,z)),Vector((0,1,0)),distance=2)
        vals.append(round(p.y,3) if hit else None)
    print(round(z,3),vals)
