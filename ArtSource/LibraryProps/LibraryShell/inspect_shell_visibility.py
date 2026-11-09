import bpy
from mathutils import Vector
from pathlib import Path
path=Path(__file__).resolve().parent/'LibraryStudyArchive_OneBuilding_v01.blend'
bpy.ops.wm.open_mainfile(filepath=str(path))
scene=bpy.context.scene
deps=bpy.context.evaluated_depsgraph_get()
for point in [(-.85,4,1.0),(0,6,1.4),(.55,4,1.7),(1.0,3,2.7),
              (-.8,8,1.5),(0,8,1.5)]:
    src=Vector((.35,-9.55,1.98))
    vec=Vector(point)-src
    ok,where,norm,idx,obj,mat=scene.ray_cast(deps,src,vec.normalized(),distance=vec.length)
    print('CAST',point,'HIT',obj.name if obj else None,'WHERE',tuple(round(v,3) for v in where))
for o in scene.objects:
    if o.type!='MESH' or 'WALL_' not in o.name:continue
    bb=[o.matrix_world@Vector(v) for v in o.bound_box]
    mn=Vector([min(v[i] for v in bb) for i in range(3)])
    mx=Vector([max(v[i] for v in bb) for i in range(3)])
    if mn.x<1.9 and mx.x>-.20 and mn.y<2 and mx.y>-.2 and mn.z<3:
        print('NEAR_GATE_WALL',o.name,'LO',tuple(round(x,2) for x in mn),'HI',tuple(round(x,2) for x in mx))
