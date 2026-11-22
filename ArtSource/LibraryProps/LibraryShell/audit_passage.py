import bpy
from mathutils import Vector
from pathlib import Path
source=Path(__file__).resolve().parent/"LibraryStudyArchive_OneBuilding_v01.blend"
bpy.ops.wm.open_mainfile(filepath=str(source))
for o in bpy.data.objects:
    if o.type!='MESH':continue
    pts=[o.matrix_world @ Vector(c) for c in o.bound_box]
    mi=[min(p[i] for p in pts) for i in range(3)]
    ma=[max(p[i] for p in pts) for i in range(3)]
    if (mi[0]<1.4 and ma[0]>-1.4 and mi[1]<1.8 and ma[1]>-1.0 and
        mi[2]<2.7 and ma[2]>.8):
       if o.name.startswith(('STUDY_MainRug','CENTRAL_CrimsonRunner','ARCHIVE_CrimsonRunner','STUDY_Floor','ARCHIVE_Floor','MESH_')):
           continue
       print("OBSTRUCTION_CHECK",o.name,"MIN",tuple(round(x,3) for x in mi),
            "MAX",tuple(round(x,3) for x in ma))
