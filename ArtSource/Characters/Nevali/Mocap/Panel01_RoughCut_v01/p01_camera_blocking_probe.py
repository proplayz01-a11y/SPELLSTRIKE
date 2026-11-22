"""Read-only projection and Young Van model bounds inspection."""
import bpy
from pathlib import Path
from mathutils import Vector
from bpy_extras.object_utils import world_to_camera_view
root=Path(__file__).resolve().parent
bpy.ops.wm.open_mainfile(filepath=str(root/"SPELLSTRIKE_P01_FamilyHome_NevaliV13_RoughCut_v01.blend"))
s=bpy.context.scene
c=s.camera
print("CAMERA",c.name,"LOC",list(c.location),"ROT",list(c.rotation_euler),"LENS",c.data.lens)
for name in ("P01_YOUNG_VAN_TEMPORARY_STANDIN","P01_NEVALI_V13_VIDEO_MOTION_ROUGH","P01_TOY_BLOCK_HAND_BLOCKED","ANCHOR_VAN_YOUNG"):
    o=bpy.data.objects.get(name)
    if o:
        world=o.matrix_world.translation
        print("OBJECT",name,"WORLD",list(world),"DIMS",list(o.dimensions),"SCREEN",list(world_to_camera_view(s,c,world)))
for v in [(-2.4,-3.8,1.2),(-2.0,-3.0,1.2),(-1.8,-3.4,1.2),(-1.0,-3.1,1.2),(-2.4,-4.0,1.2),(-2.2,-4.4,1.2),(-2.5,-3.8,1.5)]:
    print("CANDIDATE",v,"SCREEN",list(world_to_camera_view(s,c,Vector(v))))
